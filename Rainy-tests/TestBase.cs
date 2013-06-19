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
	public abstract class TestBase
	{
		protected RainyTestServer testServer;
		protected IDbConnectionFactory factory;
		protected Funq.Container container;

		public TestBase ()
		{
		}

		protected void CreateDatabaseSchema (bool reset = true)
		{
			DatabaseBackend.CreateSchema (factory, reset);
		}

		// TODO maybe go into DbTestBase
		private void WireupSqliteTestserver (Funq.Container container)
		{
			this.container = container;

			container.Register<SqliteConfig> (c => {
				var test_db_file = "rainy-test.db";
				if (File.Exists (test_db_file))
					File.Delete (test_db_file);

				SqliteConfig cnf = new SqliteConfig () {
					File = test_db_file
				};
				return cnf;
			});

			container.Register<IDbConnectionFactory> (c => {
				var connection_string = container.Resolve<SqliteConfig> ().ConnectionString;
				return new OrmLiteConnectionFactory (connection_string, SqliteDialect.Provider);
			});

			container.Register<IAuthenticator> (c => {
				var factory = c.Resolve<IDbConnectionFactory> ();
				var dbauth = new DbAuthenticator (factory);
				//var dbauth = new DbTestAuthenticator ();

				// insert a dummy testuser
				using (var db = factory.OpenDbConnection ()) {
					db.InsertParam<DBUser> (new DBUser {
						Username = RainyTestServer.TEST_USER,
						Password = RainyTestServer.TEST_PASS,
						IsActivated = true,
						IsVerified = true
					});
				}

				return dbauth;
			});

			container.Register<IAdminAuthenticator> (c => {
				return new DummyAdminAuthenticator (RainyTestServer.ADMIN_TEST_PASS);
			});

			container.Register<IDataBackend> (c => {
				var factory = c.Resolve<IDbConnectionFactory> ();
				var auth = c.Resolve<IAuthenticator> ();
				return new DatabaseBackend (factory, auth);
			});

			container.Register<OAuthHandlerBase> (c => {
				var auth = c.Resolve<IAuthenticator> ();
				var factory = c.Resolve<IDbConnectionFactory> ();
				var handler = new OAuthDatabaseHandler (factory, auth);
				return handler;
			});

			this.factory = container.Resolve<IDbConnectionFactory> ();
			CreateDatabaseSchema ();

			// HACK so the user is inserted when a fixture SetUp is run
			container.Resolve<IAuthenticator> ();
		}

		[SetUp]
		public virtual void SetUp ()
		{
			testServer = new RainyTestServer (WireupSqliteTestserver);

			testServer.Start ();
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
