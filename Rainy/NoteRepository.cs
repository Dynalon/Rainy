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
	// TODO replace with tomboy library manifest
	// used internally to mimic the manifest.xml data storage
	[DataContract]
	[Obsolete]
	public class NoteManifest
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

	// TODO move OAuth stuff into here
	public class RainyFileSystemDataBackend : IDataBackend
	{
		protected string notesBasePath;

		public RainyFileSystemDataBackend (string notes_base_path)
		{
			this.notesBasePath = notes_base_path;
		}
		public INoteRepository GetNoteRepository (string username)
		{
			return new NoteRepository (username, notesBasePath);
		}
		/// <summary>
		/// Note repository. There may only exists one repository of a username at any given time in memory. 
		/// When trying to create another one of the same username, the thread will block until the previous
		/// repository of that user was disposed. Best is to always use using (new NoteRepository (username)) {  ... }
		/// to make sure the repository is freed afterwards.
		/// </summary>
		public class NoteRepository : INoteRepository
		{

			public string Username { get; protected set; }

			public Tomboy.Engine Engine { get; set; }

			//public Dictionary<string, int> NoteRevisions { get; set; }

			public NoteManifest Manifest { get; set; }

			protected string notesBasePath;
			protected IStorage storage;
			protected string storagePath;
			protected string manifestPath;

			// holds semaphores for each user to avoid multiple instances
			protected static Dictionary<string, Semaphore> userLocks = new Dictionary<string, Semaphore> ();

			public NoteRepository (string username, string notes_base_path)
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
				manifestPath = Path.Combine (storagePath, "manifest.json");
				if (File.Exists (manifestPath)) {	
					string manifest_json = File.ReadAllText (manifestPath);
					Manifest = manifest_json.FromJson <NoteManifest> ();
				} else {
					Manifest = new NoteManifest ();
				}	

			}
			public void Dispose ()
			{
				// write back the manifest
				var manifest = new NoteManifest ();
				manifest.PopulateWith (this.Manifest);
				string manifest_json = manifest.ToJson ();
				File.WriteAllText (this.manifestPath, manifest_json);

				userLocks [Username].Release ();
			}
		}
	}
}