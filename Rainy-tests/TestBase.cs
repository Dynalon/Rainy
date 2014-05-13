using NUnit.Framework;
using ServiceStack.ServiceClient.Web;
using Rainy.Interfaces;

namespace Rainy.Tests
{
	public class DummyAuthenticator : IAuthenticator
	{
		public bool VerifyCredentials (string username, string password)
		{
			return true;
		}
	}
	public class DummyAdminAuthenticator : IAdminAuthenticator
	{
		string Password;
		public DummyAdminAuthenticator ()
		{
		}
		public DummyAdminAuthenticator (string pass)
		{
			Password = pass;
		}
		public bool VerifyAdminPassword (string password)
		{
			if (string.IsNullOrEmpty (Password))
				return true;
			else return Password == password;
		}
	}

	public abstract class TestBase
	{
		protected RainyTestServer testServer;
		protected string adminPass;
		protected string listenUrl;

		public TestBase ()
		{
		}

		[SetUp]
		public virtual void SetUp ()
		{
			testServer = new RainyTestServer ();
			adminPass = RainyTestServer.ADMIN_TEST_PASS;
			listenUrl = testServer.ListenUrl;
		}
		[TearDown]
		public virtual void TearDown ()
		{
			testServer.Stop ();
		}

		protected JsonServiceClient GetAdminServiceClient ()
		{
			var client = new JsonServiceClient (testServer.ListenUrl);
			client.LocalHttpWebRequestFilter += (request) => {
					request.Headers.Add ("Authority", adminPass);
			};

			return client;
		}
		protected JsonServiceClient GetServiceClient ()
		{
			return new JsonServiceClient (testServer.ListenUrl);
		}
	}
}
