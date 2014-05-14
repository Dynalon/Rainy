using System;
using NUnit.Framework;
using Tomboy.Sync;
using Tomboy.Sync.Web.Developer;
using Tomboy.OAuth;
using Tomboy.Sync.Web;
using System.Linq;

namespace Rainy.Tests
{
	[TestFixture]
	public class DeveloperServiceTests : AbstractSyncManagerTestsBase
	{
		RainyTestServer testServer;
		IOAuthToken accessToken;

		public DeveloperServiceTests ()
		{
		}
		
		#region implemented abstract members of AbstractSyncManagerTestsBase
		protected override void ClearServer (bool reset = false)
		{
			return;
		}
		#endregion

		[SetUp]
		public new void SetUp ()
		{
			testServer = new RainyTestServer ();
			testServer.Start ();

			accessToken = testServer.GetAccessToken ();
			this.syncServer = new WebSyncServer (testServer.ListenUrl, accessToken);
		}
		[TearDown]
		public new void TearDown ()
		{
			testServer.Stop ();
		}

		[Test]
		public void ClearAllNotesForUser ()
		{
			FirstSyncForBothSides ();
	
			DeveloperServiceClient client = new DeveloperServiceClient (testServer.ListenUrl, accessToken);
			client.ClearAllNotes (RainyTestServer.TEST_USER);

			var all_notes = this.syncServer.GetAllNotes (false);
			Assert.AreEqual (0, all_notes.Count ());
		}
	}
}

