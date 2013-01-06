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
			RainyTestServer.StartNewRainyStandaloneServer ();

			syncServer = new WebSyncServer (RainyTestServer.BaseUri, RainyTestServer.GetAccessToken ());
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
