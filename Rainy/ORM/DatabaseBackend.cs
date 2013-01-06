using System;
using System.IO;

using ServiceStack.Text;

using Tomboy;
using Rainy.Db;
using ServiceStack.OrmLite;
using ServiceStack.Common;
using Tomboy.Sync;
using System.Data;

namespace Rainy
{
	public class DatabaseBackend : IDataBackend
	{
		private bool reset;

		public DatabaseBackend (string data_path, bool reset = false)
		{
			DbConfig.SetSqliteFile (Path.Combine (data_path, "rainy.db"));
			DbConfig.CreateSchema (reset);

		}
		#region IDataBackend implementation
		public INoteRepository GetNoteRepository (string username)
		{
			var rep = new DatabaseNoteRepository (username);
			return rep;
		}
		#endregion
	}

	// maybe move into DatabaseBackend as nested class
	public class DatabaseNoteRepository : INoteRepository
	{

		private readonly string username;
		private DbStorage storage;
		private string manifestPath;

		private Engine engine;
		private SyncManifest manifest;
		private IDbConnection dbConnection;
		private DBUser dbUser;

		public DatabaseNoteRepository (string username)
		{
			username = username;

			dbConnection = DbConfig.GetConnection ();
			storage = new DbStorage (username);
			engine = new Engine (storage);

			var db_user = dbConnection.Select <DBUser> ("Username = {0}", username);
			if (db_user.Count == 0) {
				dbUser = new DBUser () { Username = username };
			}
			else
				dbUser = db_user[0];

			if (dbUser.Manifest == null || string.IsNullOrEmpty (dbUser.Manifest.ServerId)) {
				// the user may not yet have synced
				dbUser.Manifest = new SyncManifest ();
				dbUser.Manifest.ServerId = Guid.NewGuid ().ToString ();
			}
		}

		#region IDisposable implementation
		public void Dispose ()
		{
			storage.Dispose ();

			// write back the user
			dbConnection.Update (dbUser);

			dbConnection.Dispose ();
		}
		#endregion
		#region INoteRepository implementation
		public Engine Engine {
			get {
				return engine;
			}
		}
		public SyncManifest Manifest {
			get {
				return dbUser.Manifest;
			}
		}
		#endregion
	}

}