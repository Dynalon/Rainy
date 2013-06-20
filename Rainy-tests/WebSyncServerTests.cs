using NUnit.Framework;
using Tomboy.Sync.Web;
using Rainy.Db;
using ServiceStack.OrmLite;

namespace Rainy.Tests
{

	[TestFixture]
	public class WebSyncServerTestsSqlite : Tomboy.Sync.AbstractSyncManagerTests
	{
		protected RainyTestServer testServer;
		protected DBUser testUser;

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

		protected override void ClearServer (bool reset = false)
		{
			return;
		}
	}
	[TestFixture]
	public class WebSyncServerTestsPostgres : Tomboy.Sync.AbstractSyncManagerTests
	{
		protected RainyTestServer testServer;
		protected DBUser testUser;

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

		protected override void ClearServer (bool reset = false)
		{
			return;
		}
	}
}
