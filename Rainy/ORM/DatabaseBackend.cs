using System;
using System.IO;
using System.Data;

using Rainy.OAuth;
using Rainy.Db;
using ServiceStack.OrmLite;
using Tomboy;
using Tomboy.Sync;
using Rainy.Interfaces;
using ServiceStack.WebHost.Endpoints;

namespace Rainy
{
	public class DbAccessObject
	{
		protected IDbConnectionFactory connFactory;
		public DbAccessObject ()
		{
			this.connFactory = Container.Instance.Resolve<IDbConnectionFactory> ();
		}
	}

	public class DatabaseBackend : DbAccessObject, IDataBackend
	{
		OAuthHandlerBase oauthHandler;

		public DatabaseBackend (CredentialsVerifier auth = null) : base ()
		{
			if (auth == null)
				oauthHandler = new OAuthDatabaseHandler (DbAuthenticator);
			else
				oauthHandler = new OAuthDatabaseHandler (auth);

			CreateSchema ();
		}

		public static void CreateSchema (bool reset = false)
		{
			var factory = Rainy.Container.Instance.Resolve<IDbConnectionFactory> ();
			using (var db = factory.OpenDbConnection ()) {
				if (reset) {
					db.DropAndCreateTable <DBUser> ();
					db.DropAndCreateTable <DBNote> ();
					db.DropAndCreateTable <DBAccessToken> ();
				} else {
					db.CreateTableIfNotExists <DBUser> ();
					db.CreateTableIfNotExists <DBNote> ();
					db.CreateTableIfNotExists <DBAccessToken> ();
				}
			}
		}

		// verifies a given user/password combination
		public static bool DbAuthenticator (string username, string password)
		{
			DBUser user = null;
			var factory = Rainy.Container.Instance.Resolve<IDbConnectionFactory> ();
			using (var conn = factory.OpenDbConnection ()) {
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
			using (var db = connFactory.OpenDbConnection ()) {
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
	public class DatabaseNoteRepository : DbAccessObject, Rainy.Interfaces.INoteRepository
	{
		private DbStorage storage;
		private Engine engine;
		private DBUser dbUser;

		public DatabaseNoteRepository (string username)
		{
			using (var db = connFactory.OpenDbConnection ()) {
				dbUser = db.First<DBUser> (u => u.Username == username);
			}

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
			using (var db = connFactory.OpenDbConnection ()) {
				using (var trans = db.OpenTransaction ()) {
					db.Delete (dbUser);
					db.Insert (dbUser);
					trans.Commit ();
				}
			}
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