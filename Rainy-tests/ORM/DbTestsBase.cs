using System;
using Tomboy.Sync.DTO;
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
			// Start with empty tables in each test run
			using (var c = dbFactory.OpenDbConnection ()) {
				c.DropAndCreateTable <DBNote> ();
				c.DropAndCreateTable <DBUser> ();
				c.DropAndCreateTable <DBAccessToken> ();
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
		protected DBNote GetDBSampleNote (string username = "test")
		{
			var db_note = GetDTOSampleNote ().ToDBNote ();
			db_note.Username = username;
			return db_note;
		}
		protected List<DTONote> GetDTOSampleNotes (int num, string username = "test")
		{
			var notes = new List<DTONote> ();

			for (int i=0; i < num; i++) {
				notes.Add (GetDTOSampleNote ());
			}
			return notes;
		}
		protected List<DBNote> GetDBSampleNotes (int num, string username = "test")
		{
			var notes = new List<DBNote> ();

			for (int i=0; i < num; i++) {
				notes.Add (GetDBSampleNote (username));
			}

			return notes;
		}
	}
}
