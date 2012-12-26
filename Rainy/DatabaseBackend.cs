using System;
using System.IO;

using ServiceStack.Text;

using Tomboy;
using Rainy.Db;
using ServiceStack.OrmLite;
using ServiceStack.Common;

namespace Rainy
{
	// TODO replace with tomboy library manifest
	// used internally to mimic the manifest.xml data storage

	public class DatabaseBackend : IDataBackend
	{
		private bool reset;

		private string storagePath;

		public DatabaseBackend (string path, bool reset = false)
		{
			this.storagePath = path;
			if (reset) {
				var dbFactory = new OrmLiteConnectionFactory ("rainy.db", SqliteDialect.Provider);
				using (var c = dbFactory.OpenDbConnection ()) {
					c.DropAndCreateTable <DBNote> ();
					c.DropAndCreateTable <DBUser> ();
				}
			}

		}
		#region IDataBackend implementation
		public INoteRepository GetNoteRepository (string username)
		{
			var rep = new DatabaseNoteRepository (username, storagePath);
			return rep;
		}
		#endregion
	}

	// maybe move into DatabaseBackend as nested class
	public class DatabaseNoteRepository : INoteRepository
	{

		private readonly string Username;
		private DbStorage storage;
		private string manifestPath;

		private Engine engine;
		private NoteManifest manifest;

		public DatabaseNoteRepository (string username, string base_path)
		{
			Username = username;
			// TODO move everything JSON/filebased into database backend
			var storagePath = base_path + "/" + Username;
			if (!Directory.Exists (storagePath)) {
				Directory.CreateDirectory (storagePath);
			}
			
			storage = new DbStorage (username, new OrmLiteConnectionFactory ("rainy.db", SqliteDialect.Provider));
			storage.SetPath (storagePath);
			engine = new Engine (storage);
			
			// read in data from "manifest" file
			this.manifestPath = Path.Combine (storagePath, "manifest.json");
			if (File.Exists (manifestPath)) {	
				string manifest_json = File.ReadAllText (manifestPath);
				manifest = manifest_json.FromJson <NoteManifest> ();
			} else {
				manifest = new NoteManifest ();
			}
		}
		#region IDisposable implementation
		public void Dispose ()
		{
			storage.Dispose ();

			// write back the manifest
			var manifest = new NoteManifest ();
			manifest.PopulateWith (this.Manifest);
			string manifest_json = manifest.ToJson ();
			File.WriteAllText (this.manifestPath, manifest_json);
				
		}
		#endregion
		#region INoteRepository implementation
		public Engine Engine {
			get {
				return engine;
			}
		}
		public NoteManifest Manifest {
			get {
				return manifest;
			}
		}
		#endregion
	}

}