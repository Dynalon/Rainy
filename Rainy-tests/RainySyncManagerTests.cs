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
			RainyTestServer.StartNewServer ();

			syncServer = new WebSyncServer (RainyTestServer.BaseUri, RainyTestServer.GetAccessToken ());
		}

		[TearDown]
		public new void TearDown ()
		{
			RainyTestServer.Stop ();
		}

		protected override void ClearServer (bool reset = false)
		{
			return;
		}
	}
}
