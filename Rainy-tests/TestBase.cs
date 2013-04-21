using NUnit.Framework;
using ServiceStack.ServiceClient.Web;

namespace Rainy.Tests
{
	public abstract class TestBase
	{
		protected string baseUri;
		protected string listenUri;
		protected string adminPassword = "foobar";
		protected  RainyTestServer testServer;

		[SetUp]
		public virtual void SetUp ()
		{
			testServer = new RainyTestServer ();
			testServer.Start ();
			baseUri = testServer.BaseUri;
			listenUri = testServer.RainyListenUrl;
		}
		[TearDown]
		public virtual void TearDown ()
		{
			testServer.Stop ();
		}

		protected JsonServiceClient GetAdminServiceClient ()
		{
			var client = new JsonServiceClient (listenUri);
			client.LocalHttpWebRequestFilter += (request) => {
				request.Headers.Add ("Authorization", adminPassword);
			};

			return client;
		}
		protected JsonServiceClient GetServiceClient ()
		{
			return new JsonServiceClient (listenUri);
		}
	}
	
}
