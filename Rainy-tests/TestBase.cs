using NUnit.Framework;
using ServiceStack.ServiceClient.Web;
using JsonConfig;

namespace Rainy.Tests
{
	public abstract class TestBase
	{
		protected string adminPassword = "foobar";
		protected  RainyTestServer testServer;

		[SetUp]
		public virtual void SetUp ()
		{
			testServer = new RainyTestServer ();
			testServer.Start ();

			Config.Global = Config.ApplyJson (@"{ AdminPassword: '"+ adminPassword + "' }");
		}
		[TearDown]
		public virtual void TearDown ()
		{
			testServer.Stop ();
		}

		protected JsonServiceClient GetAdminServiceClient ()
		{
			var client = new JsonServiceClient (testServer.RainyListenUrl);
			client.LocalHttpWebRequestFilter += (request) => {
				request.Headers.Add ("Authority", adminPassword);
			};

			return client;
		}
		protected JsonServiceClient GetServiceClient ()
		{
			return new JsonServiceClient (testServer.RainyListenUrl);
		}
	}
	
}
