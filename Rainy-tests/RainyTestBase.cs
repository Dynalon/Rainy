using NUnit.Framework;

namespace Rainy
{
	public abstract class RainyTestBase
	{
		protected string baseUri;
		protected string listenUri;

		[SetUp]
		public void SetUp ()
		{
			RainyTestServer.StartNewRainyStandaloneServer ();
			baseUri = RainyTestServer.BaseUri;

		}
		[TearDown]
		public void TearDown ()
		{
			RainyTestServer.StopRainyStandaloneServer ();
		}
	}
	
}
