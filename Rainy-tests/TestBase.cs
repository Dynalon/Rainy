using NUnit.Framework;
using ServiceStack.ServiceClient.Web;
using JsonConfig;
using ServiceStack.OrmLite;
using System.Data;

namespace Rainy.Tests
{
	public abstract class TestBase
	{
		protected string adminPassword = "foobar";
		protected  RainyTestServer testServer;
		protected IDbConnectionFactory factory;

		public TestBase ()
		{
		}
		[SetUp]
		public virtual void SetUp ()
		{
			testServer = new RainyTestServer ();
			testServer.Start ();
			factory = Rainy.Container.Instance.Resolve<IDbConnectionFactory> ();

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
