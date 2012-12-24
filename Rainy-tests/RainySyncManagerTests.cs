using NUnit.Framework;
using Tomboy.Sync.Web;

namespace Rainy
{

	[TestFixture]
	public class RainySyncManagerTests : Tomboy.Sync.AbstractSyncManagerTests
	{
		[SetUp]
		public new void SetUp ()
		{
			RainyTestServer.BaseUri = "http://127.0.0.1:8080/johndoe/none";
			RainyTestServer.StartNewRainyStandaloneServer ();

			syncServer = new WebSyncServer ("http://127.0.0.1:8080/johndoe/none", RainyTestServer.GetAccessToken ());

		}

		[TearDown]
		public new void TearDown ()
		{
			RainyTestServer.StopRainyStandaloneServer ();
		}

		protected override void ClearServer (bool reset = false)
		{
			return;
		}
	}
}
