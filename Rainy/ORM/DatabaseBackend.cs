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
		public DbAccessObject (IDbConnectionFactory factory)
		{
			connFactory = factory;
		}
	}

	public class DbAuthenticator : IAuthenticator
	{
		private IDbConnectionFactory connFactory;

		public DbAuthenticator (IDbConnectionFactory factory)
		{
			if (factory == null)
				throw new ArgumentNullException ("factory");

			Console.WriteLine("****");
			Console.WriteLine("****");
			Console.WriteLine("****");
			Console.WriteLine("****");
			connFactory = factory;
		}

		public bool VerifyCredentials (string username, string password)
		{
			Console.WriteLine("$$$$");
			DBUser user = null;
			Console.WriteLine ("username: {0}, password: {1}", username, password);
			using (var conn = connFactory.OpenDbConnection ()) {
				user = conn.FirstOrDefault<DBUser> (u => u.Username == username && u.Password == password);
			}
			Console.WriteLine (user);
			if (user == null)
				return false;

			if (user.IsActivated == false)
				return false;

			if (user.IsVerified == false)
				return false;

			return true;

		}

	}

	public class DatabaseBackend : DbAccessObject, IDataBackend
	{
		OAuthHandlerBase oauthHandler;

		public DatabaseBackend (IDbConnectionFactory factory, IAuthenticator auth) : base (factory)
		{
			oauthHandler = new OAuthDatabaseHandler (factory, auth);

			CreateSchema (factory);
		}

		public static void CreateSchema (IDbConnectionFactory connFactory, bool reset = false)
		{
			if (connFactory == null)
				throw new ArgumentNullException ("connFactory");

			using (var db = connFactory.OpenDbConnection ()) {
				if (reset) {
					// postgresql ormlite workaround, see issue
					var ormfac = connFactory as OrmLiteConnectionFactory;
					if (ormfac.DialectProvider == PostgreSqlDialect.Provider) {
						var cmd = db.CreateCommand ();
						cmd.CommandText = "DROP SCHEMA PUBLIC CASCADE;";
						cmd.ExecuteNonQuery ();
						cmd = db.CreateCommand ();
						cmd.CommandText = "CREATE SCHEMA public AUTHORIZATION td";
						cmd.ExecuteNonQuery ();
						db.CreateTableIfNotExists <DBUser> ();
						db.CreateTableIfNotExists <DBNote> ();
						db.CreateTableIfNotExists <DBAccessToken> ();
					} else {
						db.DropAndCreateTable <DBUser> ();
						db.DropAndCreateTable <DBNote> ();
						db.DropAndCreateTable <DBAccessToken> ();
					}
				} else {
					db.CreateTableIfNotExists <DBUser> ();
					db.CreateTableIfNotExists <DBNote> ();
					db.CreateTableIfNotExists <DBAccessToken> ();
				}
			}
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
			var rep = new DatabaseNoteRepository (this.connFactory, username);
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

		public DatabaseNoteRepository (IDbConnectionFactory factory, string username) : base (factory)
		{
			using (var db = connFactory.OpenDbConnection ()) {
				dbUser = db.First<DBUser> (u => u.Username == username);
			}

			storage = new DbStorage (factory, dbUser);
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