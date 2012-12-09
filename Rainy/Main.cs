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

			// simply use user/password list from config for authentication
			OAuthAuthenticator config_authenticator = (username, password) => {
				// call the authenticater callback
				if (string.IsNullOrEmpty (username) || string.IsNullOrEmpty (password))
				return false;
				
				foreach (dynamic credentials in Config.Global.Users) {
					if (credentials.Username == username && credentials.Password == password)
						return true;
				}
				return false;
			};

			// TODO the oauth handler must be put into different data backends
			var oauth_handler = new OAuthHandler ("/tmp/rainy/oauth/", config_authenticator);
			oauth_handler.StartIntervallWriteThread ();

			var data_backend = new RainyFileSystemDataBackend (NotesPath);

			using (var listener = new RainyStandaloneServer (oauth_handler, data_backend)) {

				listener.Port = Config.Global.ListenPort;
				listener.Hostname = Config.Global.ListenAddress;

				listener.Start ();

				Console.WriteLine ("Press RETURN to stop Rainy");
				Console.ReadLine ();
			}
			oauth_handler.StopIntervallWriteThread ();
		}
	}
}