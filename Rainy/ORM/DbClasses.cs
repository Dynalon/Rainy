using Tomboy.Sync.Web.DTO;
using ServiceStack.DataAnnotations;
using Tomboy.Sync;
using ServiceStack.OrmLite;
using System.Data;
using Rainy.OAuth.SimpleStore;
using System;
using DevDefined.OAuth.Storage.Basic;
using Rainy.UserManagement;
using System.Runtime.Serialization;

namespace Rainy.Db
{
	public class DBNote : DTONote 
	{
		[PrimaryKey]
		// Guid is not feasible for primary key as on an account move as user
		// might duplicate notes across different accounts
		public string CompoundPrimaryKey {
			get {
				return Username + "_" + Guid;
			}
		}

		public new string Guid { get; set; }
	
		// to associate a note to a username
		public string Username { get; set; }
	}
	
	public class DBUser : DTOUser
	{
		[PrimaryKey]
		public override string Username { get; set; }

		public SyncManifest Manifest { get; set; }

		// whether email verifcation has to take place
		public bool IsVerified { get; set; } 
		// the verification key 
		public string VerifySecret { get; set; }

		public bool IsActivated { get; set; }

		public DBUser ()
		{
			Manifest = new SyncManifest ();
		}
	}

	public class DBAccessToken : AccessToken
	{
		[PrimaryKey]
		public new string Token { get; set; }
	}

	public static class DbConfig
	{
		private static bool isInitialized = false;
		private static string sqliteFile = "rainy.db";

		public static string SqliteFile {
			get { return sqliteFile; }
		}

		public static string ConnectionString {
			get { return sqliteFile; }
		}

		private static object syncRoot = new object ();
		private static OrmLiteConnectionFactory dbFactory; 

		public static void SetSqliteFile (string filename)
		{
			lock (syncRoot) {
				if (isInitialized) {
					//throw new Exception ("Filename can't be set once the class is initialized");
				} else {
					sqliteFile = filename;
				}
			}
		}
		public static IDbConnection GetConnection ()
		{
			lock (syncRoot) {
				if (!isInitialized) {
					isInitialized = true;
					dbFactory = new OrmLiteConnectionFactory (ConnectionString, SqliteDialect.Provider);
				}
			}
			return dbFactory.OpenDbConnection ();
		}
		public static IDbTransaction GetTransaction ()
		{
			return GetConnection ().OpenTransaction ();
		}

		public static void CreateSchema (bool reset = false)
		{
			using (var conn = GetConnection ()) {
				if (reset) {
					conn.DropAndCreateTable <DBUser> ();
					conn.DropAndCreateTable <DBNote> ();
					conn.DropAndCreateTable <DBAccessToken> ();
				} else {
					conn.CreateTableIfNotExists <DBUser> ();
					conn.CreateTableIfNotExists <DBNote> ();
					conn.CreateTableIfNotExists <DBAccessToken> ();
				}
			}
		}
	}
	public static class DbClassConverter
	{
		public static DBNote ToDBNote (this DTONote dto, DBUser user, bool encrypt = false)
		{
			// ServiceStack's .PopulateWith is for some reasons
			// ORDERS of magnitudes slower than manually copying
			// TODO evaluate PopulateWith performance / bottleneck
			// or other mappers like ValueInjecter

			var db = new DBNote ();

			db.Guid = dto.Guid;
			db.Title = dto.Title;
			db.Text = dto.Text;
			db.Tags = dto.Tags;

			// dates
			db.ChangeDate = dto.ChangeDate;
			db.MetadataChangeDate = dto.MetadataChangeDate;
			db.CreateDate = dto.CreateDate;

			db.OpenOnStartup = dto.OpenOnStartup;
			db.Pinned = dto.Pinned;

			db.Username = user.Username;

			return db;
		}

		public static DTONote ToDTONote (this DBNote db)
		{
			var dto = new DTONote ();
			
			dto.Guid = db.Guid;
			dto.Title = db.Title;
			dto.Text = db.Text;
			dto.Tags = db.Tags;
			
			// dates
			dto.ChangeDate = db.ChangeDate;
			dto.MetadataChangeDate = db.MetadataChangeDate;
			dto.CreateDate = db.CreateDate;
			
			dto.OpenOnStartup = db.OpenOnStartup;
			dto.Pinned = db.Pinned;

			return dto;
		}
	}
}
