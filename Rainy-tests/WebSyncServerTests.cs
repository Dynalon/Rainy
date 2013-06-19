using NUnit.Framework;
using Tomboy.Sync.Web;
using Rainy.Db;
using ServiceStack.OrmLite;

namespace Rainy.Tests
{

	[TestFixture]
	public class WebSyncServerTests : Tomboy.Sync.AbstractSyncManagerTests
	{
		protected DBUser testUser;
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
