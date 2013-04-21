using NUnit.Framework;
using Tomboy.Sync.Web;

namespace Rainy.Tests.WebSync
{
	[TestFixture]
	public class WebSyncServerTests : Tomboy.Sync.AbstractSyncServerTests 
	{
		protected RainyTestServer testServer;

		[SetUp]
		public void SetUp ()
		{
			CreateSomeSampleNotes ();

			testServer = new RainyTestServer ();
			testServer.Start ();
			syncServer = new WebSyncServer (testServer.BaseUri, testServer.GetAccessToken ());
		}

		[TearDown]
		public void TearDown ()
		{
			testServer.Stop ();
		}
	}

}
