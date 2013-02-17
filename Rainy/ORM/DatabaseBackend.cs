using System;
using System.IO;
using System.Data;

using Rainy.OAuth;
using Rainy.Db;
using ServiceStack.OrmLite;
using Tomboy;
using Tomboy.Sync;
using Rainy.Interfaces;

namespace Rainy
{
	public class DatabaseBackend : IDataBackend
	{
		OAuthHandlerBase oauthHandler;

		public DatabaseBackend (string database_path, CredentialsVerifier auth = null, bool reset = false)
		{
			if (auth == null)
				oauthHandler = new OAuthDatabaseHandler (DbAuthenticator);
			DbConfig.CreateSchema (reset);
		}
		// verifies a given user/password combination
		protected bool DbAuthenticator (string username, string password)
		{
			DBUser user = null;
			using (var conn = DbConfig.GetConnection ()) {
				user = conn.FirstOrDefault<DBUser> (u => u.Username == username && u.Password == password);
			}
			if (user != null)
				return true;
			else
				return false;
		}

		#region IDataBackend implementation
		public INoteRepository GetNoteRepository (string username)
		{
			var rep = new DatabaseNoteRepository (username);
			return rep;
		}
		public OAuthHandlerBase OAuth {
			get {
				return oauthHandler;
			}
		}

		#endregion
	}

	// maybe move into DatabaseBackend as nested class
	public class DatabaseNoteRepository : Rainy.Interfaces.INoteRepository
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

			using (var trans = dbConnection.BeginTransaction ()) {
				dbConnection.Delete (dbUser);
				dbConnection.Insert (dbUser);
				trans.Commit ();
			}

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