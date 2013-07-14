using System;
using System.Data;

using Rainy.OAuth;
using Rainy.Db;
using ServiceStack.OrmLite;
using Tomboy;
using Tomboy.Sync;
using Rainy.Interfaces;
using Rainy.Crypto;
using Rainy.WebService;
using DevDefined.OAuth.Storage.Basic;

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

	/// <summary>
	/// Authenticates a user against a database. User objects in the database always employ hashed passwords.
	/// </summary>
	public class DbAuthenticator : IAuthenticator
	{
		private IDbConnectionFactory connFactory;

		public DbAuthenticator (IDbConnectionFactory factory)
		{
			if (factory == null)
				throw new ArgumentNullException ("factory");

			connFactory = factory;
		}

		public bool VerifyCredentials (string username, string password)
		{
			DBUser user = null;
			using (var conn = connFactory.OpenDbConnection ()) {
				user = conn.FirstOrDefault<DBUser> (u => u.Username == username);
			}
			if (user == null)
				return false;

			if (user.IsActivated == false)
				return false;

			if (user.IsVerified == false)
				return false;

			var supplied_hash = user.ComputePasswordHash (password);
			if (supplied_hash == user.PasswordHash)
				return true;

			return false;

		}

	}

	public class DatabaseBackend : DbAccessObject, IDataBackend
	{
		OAuthHandler oauthHandler;

		public DatabaseBackend (IDbConnectionFactory factory, IAuthenticator auth, OAuthHandler handler) : base (factory)
		{
			oauthHandler = handler;

			CreateSchema (factory);
		}

		public static void CreateSchema (IDbConnectionFactory connFactory, bool reset = false)
		{
			if (connFactory == null)
				throw new ArgumentNullException ("connFactory");

			using (var db = connFactory.OpenDbConnection ()) {
				if (reset) {
					// postgresql ormlite workaround
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
						db.CreateTableIfNotExists <DBRequestToken> ();
					} else {
						db.DropAndCreateTable <DBUser> ();
						db.DropAndCreateTable <DBNote> ();
						db.DropAndCreateTable <DBAccessToken> ();
						db.DropAndCreateTable <DBRequestToken> ();
					}
				} else {
					db.CreateTableIfNotExists <DBUser> ();
					db.CreateTableIfNotExists <DBNote> ();
					db.CreateTableIfNotExists <DBAccessToken> ();
					db.CreateTableIfNotExists <DBRequestToken> ();
				}
			}
		}

		#region IDataBackend implementation
		public INoteRepository GetNoteRepository (IUser user)
		{
			var rep = new DatabaseNoteRepository (this.connFactory, user);
			return rep;
		}
		public OAuthHandler OAuth {
			get {
				return oauthHandler;
			}
		}

		#endregion
	}

	// maybe move into DatabaseBackend as nested class
	public class DatabaseNoteRepository : DbAccessObject, INoteRepository
	{
		private DbStorage storage;
		private Engine engine;
		private DBUser dbUser;

		public DatabaseNoteRepository (IDbConnectionFactory factory, IUser user) : base (factory)
		{
			using (var db = connFactory.OpenDbConnection ()) {
				dbUser = db.First<DBUser> (u => u.Username == user.Username);
				// TODO why doesn't ormlite raise this error?
				if (dbUser == null)
					throw new ArgumentException(user.Username);
			}

			var access_token_repo = new DbAccessTokenRepository<AccessToken> (factory);
			var access_token = access_token_repo.GetToken (user.AuthToken);

			var token_key = access_token.GetTokenKey ();
		
			var master_key = user.AuthToken.DecryptWithKey (token_key, dbUser.MasterKeySalt);

			storage = new DbStorage (factory, dbUser, master_key);
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