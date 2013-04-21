using NUnit.Framework;
using Tomboy.Sync.Web;

namespace Rainy.Tests.SyncManager
{

	[TestFixture]
	public class SyncManagerTests : Tomboy.Sync.AbstractSyncManagerTests
	{
		protected RainyTestServer testServer;
		[SetUp]
		public new void SetUp ()
		{
			testServer = new RainyTestServer ();
			testServer.Start ();

			syncServer = new WebSyncServer (testServer.BaseUri, testServer.GetAccessToken ());
		}

		[TearDown]
		public new void TearDown ()
		{
			testServer.Stop ();
		}

		protected override void ClearServer (bool reset = false)
		{
			return;
		}
	}
}
