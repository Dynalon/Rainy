using System;
using System.Linq;
using NUnit.Framework;
using Rainy.Db;
using Rainy.WebService;
using ServiceStack.ServiceClient.Web;
using Tomboy.Sync;
using Tomboy.Sync.Web;
using Rainy.Tests;
using Tomboy.Sync.Web.DTO;

namespace Rainy.Tests
{
	public abstract class NoteHistoryTestsBase : Tomboy.Sync.AbstractSyncManagerTestsBase
	{
		protected RainyTestServer testServer;
		protected DBUser testUser;

		protected string GetNoteHistoryUrl (string guid) {
			var url = new GetNoteHistoryRequest () {
				Guid = guid,
				Username = RainyTestServer.TEST_USER
			}.ToUrl ("GET");
			return testServer.ListenUrl + url;
		}
		protected string GetArchivedNoteUrl (string guid, long revision) {
			var url = new GetArchivedNoteRequest () {
				Guid = guid,
				Username = RainyTestServer.TEST_USER,
				Revision = revision
			}.ToUrl ("GET");
			return testServer.ListenUrl + url;
		}

		[Test]
		public void NoteHistoryIsEmpty ()
		{
			this.FirstSyncForBothSides ();
			var client = testServer.GetJsonClient ();

			var first_note = this.clientEngineOne.GetNotes ().Values.First ();
			var url = GetNoteHistoryUrl (first_note.Guid);
			var resp = client.Get<NoteHistoryResponse> (url);

			Assert.AreEqual (0, resp.CurrentRevision);
			//Assert.AreEqual (0, resp.Versions.Length);
		}

		[Test]
		public void NoteHistoryRevisionForUnknownNote ()
		{
			var client = testServer.GetJsonClient ();

			var url = GetNoteHistoryUrl (Guid.NewGuid ().ToString ());
			var resp = client.Get<NoteHistoryResponse> (url);

			Assert.AreEqual (-1, resp.CurrentRevision);
			//Assert.AreEqual (0, resp.Versions.Length);
		}

		[Test]
		public void NoteHistoryIsPresentWithOneNoteAfterChange ()
		{
			this.FirstSyncForBothSides ();

			var first_note = this.clientEngineOne.GetNotes ().Values.First ();
			var new_title = "Some other title";
			var old_title = first_note.Title;
			first_note.Title = new_title;
			clientEngineOne.SaveNote (first_note);

			var sync_manager = new SyncManager (this.syncClientOne, this.syncServer);
			sync_manager.DoSync ();

			var client = testServer.GetJsonClient ();
			var url = GetNoteHistoryUrl (first_note.Guid);
			var resp = client.Get<NoteHistoryResponse> (url);

			Assert.AreEqual (1, resp.Versions.Length);
			Assert.AreEqual (1, resp.Versions[0].Revision);
			Assert.AreEqual (old_title, resp.Versions[0].Title);

		}

		[Test]
		public void RetrieveAnArchivedVersionOfANote ()
		{
			this.FirstSyncForBothSides ();

			var first_note = this.clientEngineOne.GetNotes ().Values.First ();

			var new_title = "Some other title";
			var old_title = first_note.Title;
			var new_text = "Some new text";
			var old_text = first_note.Text;

			first_note.Title = new_title;
			first_note.Text = new_text;

			clientEngineOne.SaveNote (first_note);

			var sync_manager = new SyncManager (this.syncClientOne, this.syncServer);
			sync_manager.DoSync ();

			var client = testServer.GetJsonClient ();
			var url = GetNoteHistoryUrl (first_note.Guid);
			var resp = client.Get<NoteHistoryResponse> (url);

			var rev = resp.Versions[0].Revision;

			url = GetArchivedNoteUrl (first_note.Guid, rev);
			var note = client.Get<DTONote> (url);

			//Assert.AreEqual (old_text, note.Text);
			Assert.AreEqual (old_title, note.Title);
			Assert.AreEqual (first_note.ChangeDate, note.ChangeDate);
			Assert.AreEqual (first_note.Tags, note.Tags);

		}

		protected override void ClearServer (bool reset = false)
		{
			return;
		}
	}

	public class NoteHistoryTestsSqlite : NoteHistoryTestsBase
	{

		[SetUp]
		public new void SetUp ()
		{
			testServer = new RainyTestServer ();
			testServer.ScenarioSqlite ();
			testServer.Start ();

			syncServer = new WebSyncServer (testServer.BaseUri, testServer.GetAccessToken ());
		}

		[TearDown]
		public new void TearDown ()
		{
			testServer.Stop ();
		}
	}

	public class NoteHistoryTestsPostgres : NoteHistoryTestsBase
	{

		[SetUp]
		public new void SetUp ()
		{
			testServer = new RainyTestServer ();
			testServer.ScenarioPostgres ();
			testServer.Start ();

			syncServer = new WebSyncServer (testServer.BaseUri, testServer.GetAccessToken ());
		}

		[TearDown]
		public new void TearDown ()
		{
			testServer.Stop ();
		}
	}
	
}
