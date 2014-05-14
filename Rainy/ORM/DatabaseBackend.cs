using System;
using System.Data;
using Rainy.Db;
using ServiceStack.OrmLite;
using Tomboy.Db;
using Rainy.Interfaces;
using Rainy.OAuth;
using Rainy.WebService;

namespace Rainy
{
	public class DatabaseBackend : DbAccessObject, IDataBackend
	{
		OAuthHandler oauthHandler;
		DbStorageFactory storageFactory;

		public DatabaseBackend (IDbConnectionFactory conn_factory, DbStorageFactory storage_factory, IAuthenticator auth,
		                        OAuthHandler handler) : base (conn_factory)
		{
			oauthHandler = handler;
			storageFactory = storage_factory;

			// TODO move this into (Encrypted)DbStorageFactory implementation
			CreateSchema (conn_factory);
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
						try {
						var cmd = db.CreateCommand ();
						cmd.CommandText = "DROP SCHEMA IF EXISTS PUBLIC CASCADE;";
						cmd.ExecuteNonQuery ();
						cmd = db.CreateCommand ();
						cmd.CommandText = "CREATE SCHEMA public AUTHORIZATION rainy";
						cmd.ExecuteNonQuery ();
						} catch (Exception e) {
							Console.WriteLine (e.Message);
						}
						db.CreateTableIfNotExists <DBUser> ();
						db.CreateTableIfNotExists <DBNote> ();
						db.CreateTableIfNotExists <DBArchivedNote> ();
						db.CreateTableIfNotExists <DBAccessToken> ();
						db.CreateTableIfNotExists <DBRequestToken> ();
					} else {
						db.DropAndCreateTable <DBUser> ();
						db.DropAndCreateTable <DBNote> ();
						db.DropAndCreateTable <DBArchivedNote> ();
						db.DropAndCreateTable <DBAccessToken> ();
						db.DropAndCreateTable <DBRequestToken> ();
					}
				} else {
					db.CreateTableIfNotExists <DBUser> ();
					db.CreateTableIfNotExists <DBNote> ();
					db.CreateTableIfNotExists <DBArchivedNote> ();
					db.CreateTableIfNotExists <DBAccessToken> ();
					db.CreateTableIfNotExists <DBRequestToken> ();
				}
			}
		}

		#region IDataBackend implementation
		public INoteRepository GetNoteRepository (IUser user)
		{
			if (user == null)
				throw new ArgumentNullException ("user");

			var rep = new DatabaseNoteRepository (connFactory, storageFactory, user);
			return rep;
		}
		public OAuthHandler OAuth {
			get {
				return oauthHandler;
			}
		}

		public void ClearNotes (IUser user)
		{
			using (var db = connFactory.OpenDbConnection ()) {
				using (var trans = db.BeginTransaction ()) {
					var db_user = db.First<DBUser> (u => u.Username == user.Username);

					// delete the users notes
					db.Delete<DBNote> (n => n.Username == user.Username);

					// reset the sync manifest
					db_user.Manifest = new Tomboy.Sync.SyncManifest ();
					db.UpdateOnly (db_user, u => u.Manifest, u => u.Username == user.Username);

					trans.Commit ();
				}
			}
		}

		#endregion
	}
}