using System;
using System.Threading;
using System.Collections.Generic;
using ServiceStack.WebHost.Endpoints;
using ServiceStack.Common.Web;
using System.IO;
using System.Runtime.Serialization;

using ServiceStack.Common;
using ServiceStack.Text;

using Tomboy;
using log4net;

using Rainy.OAuth;
using Rainy.WebService;
using JsonConfig;
using Mono.Options;

namespace Rainy
{
	public class AppHost : AppHostHttpListenerBase
	{
		public static OAuthHandler OAuth;
		public static string Passkey;	

		public AppHost () : base("Test", typeof(DTONote).Assembly)
		{
		}
		public override void Configure (Funq.Container container)
		{
			// not all tomboy clients send the correct content-type
			// so we force application/json
			SetConfig (new EndpointHostConfig {
				DefaultContentType = ContentType.Json 
			});
		}
	}




	/// <summary>
	/// Note repository. There may only exists one repository of a username at any given time in memory. 
	/// When trying to create another one of the same username, the thread will block until the previous
	/// repository of that user was disposed. Best is to always use using (new NoteRepository (username)) {  ... }
	/// to make sure the repository is freed afterwards.
	/// </summary>
	public class NoteRepository : IDisposable
	{
		// used internally to mimic the manifest.xml data storage
		[DataContract]
		class NoteManifest
		{
			[DataMember (Name = "note-revisions")]
			public Dictionary<string, int> NoteRevisions { get; set; }
			
			[DataMember (Name = "latest-sync-revision")]
			public long LatestSyncRevision { get; set; }
			
			[DataMember (Name = "current-sync-guid")]
			public string CurrentSyncGuid { get; set; }
			
			public NoteManifest ()
			{
				LatestSyncRevision = -1;
				NoteRevisions = new Dictionary<string, int> ();
				CurrentSyncGuid = Guid.NewGuid ().ToString ();
			}
		}

		protected IStorage Storage;
		protected string StoragePath;
		protected string ManifestPath;
		public string Username { get; protected set; }

		public Engine NoteEngine;

		public Dictionary<string, int> NoteRevisions { get; set; }
		public long LatestSyncRevision { get; set; }
		public string CurrentSyncGuid { get; set; }

		// holds semaphores for each user to avoid multiple instances
		protected static Dictionary<string, Semaphore> userLocks = new Dictionary<string, Semaphore> ();

		public NoteRepository (string username)
		{
			this.Username = username;

			lock (userLocks) {
				if (!userLocks.ContainsKey (Username))
					userLocks [Username] = new Semaphore (1, 1);
			}
			// if another instance for this user exists, wait until it is freed
			userLocks [username].WaitOne ();

			StoragePath = MainClass.NotesPath + "/" + Username;
			if (!Directory.Exists (StoragePath)) {
				Directory.CreateDirectory (StoragePath);
			}

			Storage = new DiskStorage ();
			Storage.SetPath (StoragePath);
			NoteEngine = new Engine (Storage);

			// read in data from "manifest" file
			ManifestPath = Path.Combine (StoragePath, "manifest.json");
			NoteManifest manifest;
			if (File.Exists (ManifestPath)) {	
				string manifest_json = File.ReadAllText (ManifestPath);
				manifest = manifest_json.FromJson <NoteManifest> ();
			} else {
				manifest = new NoteManifest ();
			}	
			((NoteRepository)this).PopulateWith (manifest);

		}
		public void Dispose ()
		{
			// write back the manifest
			var manifest = new NoteManifest ();
			manifest.PopulateWith (this);
			string manifest_json = manifest.ToJson ();
			File.WriteAllText (this.ManifestPath, manifest_json);

			userLocks [Username].Release ();
		}

	}

	public class MainClass
	{
		public static string NotesPath;
		public static string OAuthDataPath;

		// HACK a dictionary holding usernames and their repos
		// can be used for locking
		public static Dictionary<string, Semaphore> UserLocks;

		protected static void SetupLogging (int loglevel)
		{
			// console appender
			log4net.Appender.ConsoleAppender appender;
			appender = new log4net.Appender.ConsoleAppender ();
			appender.Layout = new log4net.Layout.PatternLayout
				//("%-4timestamp %-5level %logger %M %ndc - %message%newline");
				("%-4timestamp [%-5level] %logger->%M - %message%newline");

			switch (loglevel) {
			case 0: appender.Threshold = log4net.Core.Level.Error; break;
			case 1: appender.Threshold = log4net.Core.Level.Warn; break;
			case 2: appender.Threshold = log4net.Core.Level.Info; break;
			case 3: appender.Threshold = log4net.Core.Level.Debug; break;
			case 4: appender.Threshold = log4net.Core.Level.All; break;
			}

			log4net.Config.BasicConfigurator.Configure (appender);
			
			LogManager.GetLogger("Logsystem").Debug ("logsystem initialized");
			
			/* ColoredConsoleAppender is win32 only. A managed version was introduced to log4net svn
			and should be available when log4net >1.2.12 comes out.
		
			Below codes is not tested/working!	
				
			log4net.Appender.ColoredConsoleAppender appender;
			appender = new log4net.Appender.ColoredConsoleAppender ();
			appender.Layout = new log4net.Layout.PatternLayout ("%date [%thread] %-5level %logger [%property{NDC}] - %message%newline");
			log4net.Config.XmlConfigurator.Configure(new System.IO.FileInfo("/Users/td/log4net.config"));
			colors.BackColor = log4net.Appender.ColoredConsoleAppender.Colors.HighIntensity;
			colors.ForeColor = log4net.Appender.ColoredConsoleAppender.Colors.Blue;
			colors.Level = log4net.Core.Level.Debug;
			appender.AddMapping(colors);	
			*/	
		}
		public static void Main (string[] args)
		{
			// parse command line arguments
			string config_file = "settings.conf";
			int loglevel = 0;
			bool show_help = false;

			var p = new OptionSet () {
				{ "c|config=", "use config file",
					(string file) => config_file = file },
				{ "v", "increase log level, where -vvvv is highest",
					v => { if (v != null) ++loglevel; } },
				{ "h|help",  "show this message and exit", 
					v => show_help = v != null },
			};
			p.Parse (args);

			if (show_help) {
				p.WriteOptionDescriptions (Console.Out);
				return;
			}

			if (!File.Exists (config_file)) {
				Console.WriteLine ("Could not find a configuration file (try the -c flag)!");
				return;
			}

			// set the configuration from the specified file
			Config.Global = Config.ApplyJsonFromPath (config_file);

			string data_path = Config.Global.DataPath;
			if (string.IsNullOrEmpty (data_path)) {
				data_path = Directory.GetCurrentDirectory ();
			}
			NotesPath = Path.Combine (data_path, "notes");
			OAuthDataPath = Path.Combine (data_path, "oauth");

			string listen_hostname = Config.Global.ListenAddress;
			int listen_port = Config.Global.ListenPort;

			var logger = LogManager.GetLogger ("Main");
			SetupLogging (loglevel);

			// start the WebServices
			var appHost = new AppHost ();

			logger.Debug ("starting oauth data store write thread"); 
			AppHost.OAuth = new OAuthHandler (OAuthDataPath);
			AppHost.OAuth.StartIntervallWriteThread ();

			appHost.Init ();

			string listen_url = "http://" + listen_hostname + ":" + listen_port + "/";
			logger.DebugFormat ("starting http listener at: {0}", listen_url);
			appHost.Start (listen_url);

			Console.WriteLine ("Press RETURN to stop Rainy");
			Console.ReadLine ();

			logger.DebugFormat ("stopping oauth data store write thread");
			AppHost.OAuth.StopIntervallWriteThread ();
		}
	}
}