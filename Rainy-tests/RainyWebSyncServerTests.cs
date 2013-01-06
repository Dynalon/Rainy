using NUnit.Framework;
using Tomboy.Sync.Web;

namespace Rainy
{
	[TestFixture]
	public class RainyWebSyncServerTests : Tomboy.Sync.AbstractSyncServerTests 
	{

		[SetUp]
		public void SetUp ()
		{
			CreateSomeSampleNotes ();

			RainyTestServer.StartNewServer ();

			syncServer = new WebSyncServer (RainyTestServer.BaseUri, RainyTestServer.GetAccessToken ());
		}

		[TearDown]
		public void TearDown ()
		{
			RainyTestServer.Stop ();
		}
	}

}
