using System;
using System.Threading;
using System.Collections.Generic;
using System.IO;

using System.Xml;
using System.Text;
using Rainy.OAuth;
using Rainy.Db;
using Rainy.Interfaces;
using ServiceStack.OrmLite;
using JsonConfig;
using Rainy.WebService;
using Tomboy.Sync;
using Tomboy;


namespace Rainy
{
	public class ConfigFileAuthenticator : IAuthenticator
	{
		private dynamic userList;
		public ConfigFileAuthenticator (dynamic userlist)
		{
			userList = userlist;
		}
		public bool VerifyCredentials (string username, string password)
		{
			// call the authenticater callback
			if (string.IsNullOrEmpty (username) || string.IsNullOrEmpty (password))
				return false;

			foreach (dynamic credentials in userList) {
				if (credentials.Username == username && credentials.Password == password)
					return true;
			}
			return false;
		}
	}
	public class ConfigFileAdminAuthenticator : IAdminAuthenticator
	{
		public bool VerifyAdminPassword (string password)
		{
			if (string.IsNullOrEmpty (password))
				return false;

			return password == Config.Global.AdminPassword;
		}
	}

	// TODO move OAuth stuff into here
	public class FileSystemBackend : Rainy.Interfaces.IDataBackend
	{
		string notesBasePath;
		OAuthHandler oauthHandler;

		public FileSystemBackend (string data_path, IDbConnectionFactory factory, IAuthenticator auth, OAuthHandler handler, bool reset = false)
		{
			oauthHandler = handler;

			// TODO move this into the oauth stuff
			//DbConfig.CreateSchema ();

			this.notesBasePath = Path.Combine (data_path, "notes");
			if (!Directory.Exists (notesBasePath)) {
				Directory.CreateDirectory (notesBasePath);
			}
		}
		public INoteRepository GetNoteRepository (IUser user)
		{
			return new DirectoryBasedNoteRepository (user.Username, notesBasePath);
		}
		public OAuthHandler OAuth {
			get { return oauthHandler; }
		}


		/// <summary>
		/// Note repository. There may only exists one repository of a username at any given time in memory. 
		/// When trying to create another one of the same username, the thread will block until the previous
		/// repository of that user was disposed. Best is to always use using (new NoteRepository (username)) {  ... }
		/// to make sure the repository is freed afterwards.
		/// </summary>
		public class DirectoryBasedNoteRepository : Rainy.Interfaces.INoteRepository
		{

			public string Username { get; protected set; }

			public Tomboy.Engine Engine { get; set; }

			//public Dictionary<string, int> NoteRevisions { get; set; }

			public SyncManifest Manifest { get; set; }

			protected string notesBasePath;
			protected IStorage storage;
			protected string storagePath;
			protected string manifestPath;

			// holds semaphores for each user to avoid multiple instances
			protected static Dictionary<string, Semaphore> userLocks = new Dictionary<string, Semaphore> ();

			public DirectoryBasedNoteRepository (string username, string notes_base_path)
			{
				this.Username = username;
				this.notesBasePath = notes_base_path;

				lock (userLocks) {
					if (!userLocks.ContainsKey (Username))
						userLocks [Username] = new Semaphore (1, 10);
				}
				// if another instance for this user exists, wait until it is freed
				userLocks [username].WaitOne ();

				storagePath = this.notesBasePath + "/" + Username;
				if (!Directory.Exists (storagePath)) {
					Directory.CreateDirectory (storagePath);
				}

				var disk_storage = new DiskStorage ();
				disk_storage.SetPath (storagePath);
				Engine = new Engine (storage);

				// read in data from "manifest" file
				manifestPath = Path.Combine (storagePath, "manifest.xml");
				if (File.Exists (manifestPath)) {	
					string manifest_xml = File.ReadAllText (manifestPath);
					Manifest = SyncManifest.Read(manifest_xml);
				} else {
					Manifest = new SyncManifest ();
					Manifest.ServerId = Guid.NewGuid ().ToString ();
				}	

			}
			public void Dispose ()
			{
				// write back the manifest
				using (var output_stream = new FileStream (this.manifestPath, FileMode.OpenOrCreate)) {
					SyncManifest.Write (this.Manifest, output_stream);
				}
				userLocks [Username].Release ();
			}
		}
	}
}