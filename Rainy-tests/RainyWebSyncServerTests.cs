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

			RainyTestServer.BaseUri = "http://127.0.0.1:8080/johndoe/none";
			RainyTestServer.StartNewRainyStandaloneServer ();

			syncServer = new WebSyncServer (RainyTestServer.BaseUri, RainyTestServer.GetAccessToken ());
		}

		[TearDown]
		public void TearDown ()
		{
			RainyTestServer.StopRainyStandaloneServer ();
		}
	}

}
