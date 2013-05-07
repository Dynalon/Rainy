using System;
using Tomboy.Sync.Web.DTO;
using NUnit.Framework;
using System.Data;
using ServiceStack.OrmLite;
using System.Collections.Generic;
using System.IO;

namespace Rainy.Db
{
	public class DbTestsBase
	{
		protected OrmLiteConnectionFactory dbFactory;
		protected DBUser testUser;
		
		[TestFixtureSetUp]
		public void FixtureSetUp ()
		{
			DbConfig.SetSqliteFile ("/tmp/rainy-test-data/rainy-test.db");
			// remove the rainy-test.db file if it exists
			if (File.Exists (DbConfig.SqliteFile)) {
				File.Delete (DbConfig.SqliteFile);
			}

			dbFactory = new OrmLiteConnectionFactory (DbConfig.ConnectionString, SqliteDialect.Provider);

		}
		
		[SetUp]
		public void SetUp ()
		{
			testUser = new DBUser () {
				Username = "test"
			};
			// Start with empty tables in each test run
			using (var c = dbFactory.OpenDbConnection ()) {
				using (var t = c.BeginTransaction ()) {
					c.DropAndCreateTable <DBNote> ();
					c.DropAndCreateTable <DBUser> ();
					c.DropAndCreateTable <DBAccessToken> ();
	
					c.InsertParam<DBUser> (testUser);
					t.Commit ();
				}
			}
			using (var c = dbFactory.OpenDbConnection ()) {
				DBUser user = c.First<DBUser> (u => u.Username == "test");
				Console.WriteLine (user.Username);
			}
		}

		[TearDown]
		public void TearDown ()
		{
		}
		
		protected DTONote GetDTOSampleNote ()
		{
			return new DTONote () {
				Title = "My sämple title",
				Text = "My sample text",
				CreateDate = DateTime.Now.ToString (Tomboy.Writer.DATE_TIME_FORMAT),
				MetadataChangeDate = DateTime.Now.ToString (Tomboy.Writer.DATE_TIME_FORMAT),
				ChangeDate = DateTime.Now.ToString (Tomboy.Writer.DATE_TIME_FORMAT),
				OpenOnStartup = true,
				Pinned = false,
				Tags = new string[] { "school", "fun", "shopping" },
				Guid = Guid.NewGuid ().ToString ()
			};
		}
		protected DBNote GetDBSampleNote ()
		{
			var db_note = GetDTOSampleNote ().ToDBNote (testUser);
			return db_note;
		}
		protected List<DTONote> GetDTOSampleNotes (int num)
		{
			var notes = new List<DTONote> ();

			for (int i=0; i < num; i++) {
				notes.Add (GetDTOSampleNote ());
			}
			return notes;
		}
		protected List<DBNote> GetDBSampleNotes (int num)
		{
			var notes = new List<DBNote> ();

			for (int i=0; i < num; i++) {
				notes.Add (GetDBSampleNote ());
			}

			return notes;
		}
	}
}
