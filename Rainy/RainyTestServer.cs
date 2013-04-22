using System;
using ServiceStack.ServiceClient.Web;
using DevDefined.OAuth.Consumer;
using DevDefined.OAuth.Framework;
using System.Net;
using System.Linq;
using Tomboy.Sync.DTO;
using Rainy.OAuth;
using System.IO;
using Tomboy.Sync.Web;
using DevDefined.OAuth.Storage.Basic;
using Rainy.Db;
using Rainy.Interfaces;

namespace Rainy
{
	// simple server that can be used from within unit tests
	// TODO make non-static
	public class RainyTestServer
	{
		public string TEST_USER = "johndoe";
		public string TEST_PASS = "none";
		public string RainyListenUrl = "http://127.0.0.1:8080/";

		public string BaseUri {
			// i.e. http://127.0.0.1:8080/johndoe/none/
			get {
				return RainyListenUrl + TEST_USER + "/" + TEST_PASS + "/";
			}
		}

		private RainyStandaloneServer rainyServer;
		private string tmpPath;

		public void Start (string use_backend = "sqlite")
		{
			tmpPath = "/tmp/rainy-test-data/";
			if (Directory.Exists (tmpPath)) {
				Directory.Delete (tmpPath, true);
			}	
			Directory.CreateDirectory (tmpPath);
			DbConfig.SetSqliteFile (Path.Combine (tmpPath, "rainy-test.db"));
			// tmpPath = Path.GetTempPath () + Path.GetRandomFileName ();

			// for debugging, we only use a simple single user authentication 
			CredentialsVerifier debug_authenticator = (user,pass) => {
				if (user == TEST_USER  && pass == TEST_PASS) return true;
				else return false;
			};

			IDataBackend backend;
			if (use_backend == "sqlite")
				backend = new DatabaseBackend (tmpPath, reset: true);
			else
				backend = new RainyFileSystemBackend (tmpPath, debug_authenticator);

			rainyServer = new RainyStandaloneServer (backend, RainyListenUrl, test_server: true);

			rainyServer.Start ();
		}
		public void Stop ()
		{
			rainyServer.Dispose ();
			//Directory.Delete (tmpPath, true);
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

			return rest_client.Get<ApiResponse> (BaseUri + "/api/1.0/");
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

			var restClient = new JsonServiceClient (BaseUri);
			var api_ref = restClient.Get<ApiResponse> ("/api/1.0");

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
