using System;
using Tomboy.Sync.Web.DTO;
using NUnit.Framework;
using System.Data;
using ServiceStack.OrmLite;
using System.Collections.Generic;
using System.IO;
using Rainy.Db.Config;
using Rainy.Tests;

namespace Rainy.Db
{
	public class DbTestsBase : TestBase
	{
		protected DBUser testUser;
		protected IDbConnectionFactory factory;
		protected string dbScenario;

		[TestFixtureSetUp]
		public void FixtureSetUp ()
		{
		}
		
		[SetUp]
		public new void SetUp ()
		{
			testServer = new RainyTestServer ();
			if (dbScenario == "postgres") {
				testServer.ScenarioPostgres ();
			} else if (dbScenario == "sqlite" || string.IsNullOrEmpty (dbScenario)) {
				testServer.ScenarioSqlite ();
			}

			testServer.Start ();

			this.factory = RainyTestServer.Container.Resolve<IDbConnectionFactory> ();
			using (var db = factory.OpenDbConnection ()) {
				testUser = db.First<DBUser> (u => u.Username == RainyTestServer.TEST_USER);
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
