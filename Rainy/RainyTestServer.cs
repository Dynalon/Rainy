using System;
using System.IO;
using System.Linq;
using System.Net;
using DevDefined.OAuth.Consumer;
using DevDefined.OAuth.Framework;
using ServiceStack.OrmLite;
using ServiceStack.ServiceClient.Web;
using Tomboy.Sync.Web;
using Tomboy.Sync.Web.DTO;
using Rainy.Db;
using Rainy.Interfaces;
using Rainy.Db.Config;
using Rainy.OAuth;
using Rainy.Crypto;
using DevDefined.OAuth.Storage.Basic;
using DevDefined.OAuth.Storage;

namespace Rainy
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

	// simple server that can be used from within unit tests
	// TODO make non-static
	public class RainyTestServer
	{
		public static string TEST_USER = "test";
		public static string TEST_PASS = "none";
		public static string ADMIN_TEST_PASS = "foobar";
		public static Funq.Container Container;
		public string ListenUrl = "http://127.0.0.1:8080/";

		public string BaseUri {
			// i.e. http://127.0.0.1:8080/johndoe/none/
			get {
				return ListenUrl + TEST_USER + "/" + TEST_PASS + "/";
			}
		}

		private RainyStandaloneServer rainyServer;
		private string tmpPath;
		private ComposeObjectGraphDelegate ObjectGraphComposer;

		public RainyTestServer (ComposeObjectGraphDelegate composer = null)
		{
			if (composer == null) {
				// specifies which default scenario to use
				this.ScenarioSqlite ();
			}

			rainyServer = new RainyStandaloneServer (ListenUrl, (c) => {
				if (this.ObjectGraphComposer == null)
					throw new Exception ("need to setup a composer/scenario for RainyTestServer!");
				this.ObjectGraphComposer(c);
			});
		}

		public void Start ()
		{	
			rainyServer.Start ();
		}
		public void Stop ()
		{
			rainyServer.Dispose ();
		}

		public void ScenarioSqlite ()
		{
			this.ObjectGraphComposer = (c) => {
				this.WireupSqliteTestserver (c);
				this.WireupGenericTestClasses (c);
			};
		}
		public void ScenarioPostgres ()
		{
			this.ObjectGraphComposer = (c) => {
				this.WireupPostgresServer (c);
				this.WireupGenericTestClasses (c);
			};
		}


		private void WireupSqliteTestserver (Funq.Container container)
		{
			Container = container;

			container.Register<SqliteConfig> (c => {
				var test_db_file = "/tmp/rainy-test.db";
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
		}

		private void WireupPostgresServer (Funq.Container container)
		{
			Container = container;

			container.Register<PostgreConfig> (c => {
				var cnf = new PostgreConfig {
					Host = "localhost",
					Username = "td",
					Password = "foobar",
					Port = 5432,
					Database = "rainy"
				};
				return cnf;
			});

			container.Register<IDbConnectionFactory> (c => {
				var connection_string = container.Resolve<PostgreConfig> ().ConnectionString;
				return new OrmLiteConnectionFactory (connection_string, PostgreSqlDialect.Provider);
			});
		}

		private void WireupGenericTestClasses (Funq.Container container)
		{
			container.Register<IAuthenticator> (c => {
				var factory = c.Resolve<IDbConnectionFactory> ();
				var dbauth = new DbAuthenticator (factory);
				//var dbauth = new DbTestAuthenticator ();

				var test_user = new DBUser {
					Username = RainyTestServer.TEST_USER,
					IsActivated = true,
					IsVerified = true
				};
				test_user.CreateCryptoFields (RainyTestServer.TEST_PASS);

				// insert a dummy testuser
				using (var db = factory.OpenDbConnection ()) {
					db.InsertParam<DBUser> (test_user);
				}

				return dbauth;
			});

			container.Register<IAdminAuthenticator> (c => {
				var admin_auth = new DummyAdminAuthenticator (ADMIN_TEST_PASS);
				return (IAdminAuthenticator)admin_auth;
			});

			container.Register<IDataBackend> (c => {
				var factory = c.Resolve<IDbConnectionFactory> ();
				var auth = c.Resolve<IAuthenticator> ();
				var handler = c.Resolve<OAuthHandler> ();
				return new DatabaseBackend (factory, auth, handler);
			});

			container.Register<OAuthHandler> (c => {
				var auth = c.Resolve<IAuthenticator> ();
				var factory = c.Resolve<IDbConnectionFactory> ();
//				ITokenRepository<AccessToken> access_tokens = new SimpleTokenRepository<AccessToken> ();
//				ITokenRepository<RequestToken> request_tokens = new SimpleTokenRepository<RequestToken> ();
				ITokenRepository<AccessToken> access_tokens = new DbAccessTokenRepository<AccessToken> (factory);
				ITokenRepository<RequestToken> request_tokens = new DbRequestTokenRepository<RequestToken> (factory);
				ITokenStore token_store = new RainyTokenStore (access_tokens, request_tokens);
				OAuthHandler handler = new OAuthHandler (auth, access_tokens, request_tokens, token_store);
				return handler;
			});

			var connFactory = container.Resolve<IDbConnectionFactory> ();
			DatabaseBackend.CreateSchema (connFactory, true);

			// HACK so the user is inserted when a fixture SetUp is run
			container.Resolve<IAuthenticator> ();
		}

		public JsonServiceClient GetJsonClient ()
		{
			var rest_client = new JsonServiceClient ();
			rest_client.SetAccessToken (GetAccessToken ());
			
			return rest_client;
		}

		public ApiResponse GetRootApiRef () 
		{
			var rest_client = new JsonServiceClient ();
			var url = new Rainy.WebService.ApiRequest ().ToUrl("GET");

			return rest_client.Get<ApiResponse> (BaseUri + url);
		}

		public UserResponse GetUserInfo ()
		{
			var api_ref = GetRootApiRef ();
			var user_service_url = api_ref.UserRef.ApiRef;
	
			var rest_client = GetJsonClient ();
		
			return rest_client.Get<UserResponse> (user_service_url);
		}

		// this performs our main OAuth authentication, performing
		// the request token retrieval, authorization, and exchange
		// for an access token
		public IToken GetAccessToken ()
		{
			var consumerContext = new OAuthConsumerContext () {
				ConsumerKey = "anyone"
			};

			var rest_client = new JsonServiceClient (BaseUri);
			var url = new Rainy.WebService.ApiRequest ().ToUrl("GET");
			var api_ref = rest_client.Get<ApiResponse> (url);

			var session = new OAuthSession (consumerContext, api_ref.OAuthRequestTokenUrl,
			                                api_ref.OAuthAuthorizeUrl, api_ref.OAuthAccessTokenUrl);
			
			IToken request_token = session.GetRequestToken ();
		
			// we dont need a callback url
			string link = session.GetUserAuthorizationUrlForToken (request_token, "http://example.com/");
			
			// visit the link to perform the authorization (no interaction needed)
			HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create (link);
			// disallow auto redirection, since we are interested in the location header only
			req.AllowAutoRedirect = false;
			
			// the oauth_verifier we need, is part of the querystring in the (redirection)
			// 'Location:' header
			string location = ((HttpWebResponse)req.GetResponse ()).Headers ["Location"];
			var query = string.Join ("", location.Split ('?').Skip (1));
			var oauth_data = System.Web.HttpUtility.ParseQueryString (query);

			IToken access_token = session.ExchangeRequestTokenForAccessToken (request_token, oauth_data ["oauth_verifier"]);

			return access_token;
		}
	}
}
