using System;
using System.Collections.Generic;
using System.Data;
using NUnit.Framework;
using Rainy.Db;
using ServiceStack.OrmLite;
using Tomboy.Sync.Web.DTO;
using Rainy.Tests;
using Tomboy.Db;

namespace Rainy.Tests.Db
{
	public abstract class DbTestsBase : TestBase
	{
		protected DBUser testUser;
		protected IDbConnectionFactory connFactory;
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

			this.connFactory = RainyTestServer.Container.Resolve<IDbConnectionFactory> ();
			using (var db = connFactory.OpenDbConnection ()) {
				testUser = db.First<DBUser> (u => u.Username == RainyTestServer.TEST_USER);
			}
		}

		[TearDown]
		public new void TearDown ()
		{
		}
		
		protected DTONote GetDTOSampleNote ()
		{
			return new DTONote () {
				Title = "My sämple title",
				Text = "My sample text",
				CreateDate = DateTime.Now.ToString (Tomboy.Xml.XmlSettings.DATE_TIME_FORMAT),
				MetadataChangeDate = DateTime.Now.ToString (Tomboy.Xml.XmlSettings.DATE_TIME_FORMAT),
				ChangeDate = DateTime.Now.ToString (Tomboy.Xml.XmlSettings.DATE_TIME_FORMAT),
				OpenOnStartup = true,
				Pinned = false,
				Tags = new string[] { "school", "fun", "shopping" },
				Guid = Guid.NewGuid ().ToString ()
			};
		}
		protected DBNote GetDBSampleNote ()
		{
			var db_note = GetDTOSampleNote ().ToDBNote (testUser.Username);
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
