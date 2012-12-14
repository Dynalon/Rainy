using System;
using NUnit.Framework;
using Rainy.Tests;
using Tomboy.Sync.Web;
using Tomboy.Sync.Filesystem;
using Tomboy.Sync;

namespace Rainy.Tests
{
	[TestFixture()]
	public class SyncManagerTests : RainyTestBase
	{
		[SetUp]
		public void SetUp ()
		{
			this.syncServer = new WebSyncServer (baseUri, GetAccessToken ());

		}

		protected override void ClearServer (bool reset=false)
		{
			if (reset) {
				StopRainyStandaloneServer ();
				StartNewRainyStandaloneServer ();
			}
			// if reset == false we do not need to do anything here	
		}
		[Test]
		public void FirstSyncForBothSides ()
		{
			base.FirstSyncForBothSides ();

			var local_notes = clientEngineOne.GetNotes ().Values;
			var server_notes = syncServer.GetAllNotes (true);

			// make sure each local note exists on the server
			foreach (var note in local_notes) {
				Assert.That (server_notes.Contains (note));
			}

			// client manifest Ids should be equal to server id and not empty
			Assert.AreEqual (clientManifestOne.ServerId, syncServer.Id);
			Assert.That (!string.IsNullOrEmpty (clientManifestOne.ServerId));

			Assert.AreEqual (sampleNotes.Count, syncServer.UploadedNotes.Count);
		}

		[Test]
		public void NoteDatesAfterSync ()
		{
			base.NoteDatesAfterSync ();
		}

		[Test]
		public new void MakeSureTextIsSynced ()
		{
			base.MakeSureTextIsSynced ();
		}

		[Test]
		public new void ClientSyncsToNewServer ()
		{
			base.ClientSyncsToNewServer ();

			// setup new server
			StopRainyStandaloneServer ();
			StartNewRainyStandaloneServer ();
			syncServer = new WebSyncServer (baseUri, GetAccessToken ());

			// sync with that new server
			var sync_manager = new SyncManager (syncClientOne, syncServer);
			sync_manager.DoSync ();
			
			// three notes should have been uploaded
			Assert.AreEqual (3, syncServer.UploadedNotes.Count);
			
			// zero notes should have been deleted from Server
			Assert.AreEqual (0, syncServer.DeletedServerNotes.Count);
			
			// zero notes should have been deleted from client
			Assert.AreEqual (0, syncClientOne.DeletedNotes.Count);
			
			// make sure the client and the server notes are equal
			var local_notes = clientEngineOne.GetNotes ().Values;
			var server_notes = syncServer.GetAllNotes (true);
			foreach (var note in local_notes) {
				Assert.That (server_notes.Contains (note));
			}
			
			// after the sync the client should carry the associated ServerId
			// from the new server
			Assert.That (!string.IsNullOrEmpty (syncClientOne.AssociatedServerId));
			Assert.AreEqual (syncClientOne.AssociatedServerId, syncServer.Id);
			
			Assert.AreEqual (clientManifestOne.ServerId, syncServer.Id);
		}

		[Test]
		public new void ClientDeletesNotesAfterFirstSync ()
		{
			base.ClientDeletesNotesAfterFirstSync ();
		}

		[Test]
		public new void NoSyncingNeededIfNoChangesAreMade()
		{
			base.NoSyncingNeededIfNoChangesAreMade ();
		}

		[Test]
		[Ignore]
		public new void MassiveAmountOfNotes ()
		{
			base.MassiveAmountOfNotes ();
		}

	}
}

