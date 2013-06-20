using NUnit.Framework;
using ServiceStack.ServiceClient.Web;
using JsonConfig;
using ServiceStack.OrmLite;
using System.Data;
using Rainy.Db.Config;
using Rainy.Db;
using Rainy.Interfaces;
using System.IO;
using Rainy.OAuth;

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

		public TestBase ()
		{
		}

		[SetUp]
		public virtual void SetUp ()
		{
			testServer = new RainyTestServer ();
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
				request.Headers.Add ("Authority", RainyTestServer.ADMIN_TEST_PASS);
			};

			return client;
		}
		protected JsonServiceClient GetServiceClient ()
		{
			return new JsonServiceClient (testServer.RainyListenUrl);
		}
	}
}
