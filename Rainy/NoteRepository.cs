using System;
using System.Threading;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;

using ServiceStack.Common;
using ServiceStack.Text;

using Tomboy;
using Tomboy.Sync;
using System.Xml;
using System.Text;


namespace Rainy
{
	// TODO move OAuth stuff into here
	public class RainyFileSystemDataBackend : IDataBackend
	{
		protected string notesBasePath;

		public RainyFileSystemDataBackend (string data_path)
		{
			this.notesBasePath = Path.Combine (data_path, "notes");
		}
		public INoteRepository GetNoteRepository (string username)
		{
			return new DirectoryBasedNoteRepository (username, notesBasePath);
		}
		/// <summary>
		/// Note repository. There may only exists one repository of a username at any given time in memory. 
		/// When trying to create another one of the same username, the thread will block until the previous
		/// repository of that user was disposed. Best is to always use using (new NoteRepository (username)) {  ... }
		/// to make sure the repository is freed afterwards.
		/// </summary>
		public class DirectoryBasedNoteRepository : INoteRepository
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

				storage = new DiskStorage ();
				storage.SetPath (storagePath);
				Engine = new Engine (storage);

				// read in data from "manifest" file
				manifestPath = Path.Combine (storagePath, "manifest.xml");
				if (File.Exists (manifestPath)) {	
					string manifest_xml = File.ReadAllText (manifestPath);
					var textreader = new StringReader (manifest_xml);
					var xmlreader = new XmlTextReader (textreader);
					Manifest = SyncManifest.Read (xmlreader);
				} else {
					Manifest = new SyncManifest ();
					Manifest.ServerId = Guid.NewGuid ().ToString ();
				}	

			}
			public void Dispose ()
			{
				// write back the manifest
				using (var xmlwriter = new XmlTextWriter (this.manifestPath, Encoding.UTF8)) {
					SyncManifest.Write (xmlwriter, this.Manifest);
				}
				userLocks [Username].Release ();
			}
		}
	}
}