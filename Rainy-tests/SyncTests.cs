using System;
using NUnit.Framework;
using Tomboy.Sync.Web;

namespace Rainy.Tests
{
	public class RainySyncTests : Tomboy.Sync.Filesystem.Tests.SyncingTests
	{
		[SetUp]
		public override void SetUp ()
		{
//			baseShouldIgnore = true;

			if (this as RainySyncTests != null)
			Console.WriteLine ("SETUP CHILD!");

			Console.WriteLine (this.GetType ());
			if (this is RainySyncTests)
				Console.WriteLine ("OK!");
			else {
				Assert.Ignore ();
			}
		}

		protected override void InitClientOne ()
		{
			base.InitClientOne ();
		}
		protected override void InitServer ()
		{

		}

		[Test()]
		public override void FirstSyncForBothSides ()
		{
			//base.FirstSyncForBothSides ();
			/*SyncManager sync_manager = new SyncManager (this.syncClientOne, this.syncServer);
			
			// before the sync, the client should have an empty AssociatedServerId
			Assert.That (string.IsNullOrEmpty (syncClientOne.AssociatedServerId));
			Assert.That (string.IsNullOrEmpty (clientManifestOne.ServerId));
			
			sync_manager.DoSync ();
			
			var local_notes = clientEngineOne.GetNotes ().Values;
			var server_notes = serverEngine.GetNotes ().Values;
			
			// make sure each local note exists on the server
			foreach (var note in local_notes) {
				Assert.That (server_notes.Contains (note));
			}*/
			Assert.Ignore ();

		}
		[Test ()]
		public new void ClientDeletesNotesAfterFirstSync ()
		{
			Assert.Fail ();
		}
	}
}

