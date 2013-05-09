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

		public DatabaseBackend (string database_path, CredentialsVerifier auth = null)
		{
			if (auth == null)
				oauthHandler = new OAuthDatabaseHandler (DbAuthenticator);
			else
				oauthHandler = new OAuthDatabaseHandler (auth);

			DbConfig.CreateSchema ();
		}
		// verifies a given user/password combination
		public static bool DbAuthenticator (string username, string password)
		{
			DBUser user = null;
			using (var conn = DbConfig.GetConnection ()) {
				user = conn.FirstOrDefault<DBUser> (u => u.Username == username && u.Password == password);
			}
			if (user == null)
				return false;

			if (user.IsActivated == false)
				return false;

			if (user.IsVerified == false)
				return false;

			return true;
		}

		#region IDataBackend implementation
		public INoteRepository GetNoteRepository (string username)
		{
			DBUser user = null;
			using (var db = DbConfig.GetConnection ()) {
				user = db.First<DBUser> (u => u.Username == username);
				// TODO why doesn't ormlite raise this error?
				if (user == null)
					throw new ArgumentException(username);
			}
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
		private DbStorage storage;
		private Engine engine;
		private IDbConnection dbConnection;
		private DBUser dbUser;

		public DatabaseNoteRepository (string username)
		{
			dbConnection = DbConfig.GetConnection ();
			dbUser = dbConnection.First<DBUser> (u => u.Username == username);
		
			storage = new DbStorage (dbUser);
			engine = new Engine (storage);

			if (dbUser.Manifest == null || string.IsNullOrEmpty (dbUser.Manifest.ServerId)) {
				// the user may not yet have synced
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