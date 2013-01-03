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

namespace Rainy
{

	// simple server that can be used from within unit tests
	public static class RainyTestServer
	{
		public static string BaseUri = "http://127.0.0.1:8080/johndoe/none/";
		public static string RainyListenUrl = "http://127.0.0.1:8080/";

		private static RainyStandaloneServer rainyServer;
		private static string tmpPath;

		public static void StartNewRainyStandaloneServer ()
		{
			tmpPath = Path.GetTempPath () + Path.GetRandomFileName ();
			Directory.CreateDirectory (tmpPath);
			
			// for debugging, we only use a simple single user authentication 
			OAuthAuthenticator debug_authenticator = (user,pass) => {
				if (user == "johndoe" && pass == "none") return true;
				else return false;
			};
			OAuthHandler handler = new OAuthHandler (tmpPath, debug_authenticator, 60);
			//IDataBackend backend = new RainyFileSystemDataBackend (tmpPath);
			IDataBackend backend = new DatabaseBackend (tmpPath, reset: true);

			rainyServer = new RainyStandaloneServer (handler, backend, RainyListenUrl);

			rainyServer.Start ();
			
		}
		public static void StopRainyStandaloneServer ()
		{
			rainyServer.Stop ();
			Directory.Delete (tmpPath, true);
		}

		public static JsonServiceClient GetJsonClient ()
		{
			var rest_client = new JsonServiceClient ();
			rest_client.SetAccessToken (GetAccessToken ());
			
			return rest_client;
		}

		public static ApiResponse GetRootApiRef () 
		{
			var rest_client = new JsonServiceClient ();

			return rest_client.Get<ApiResponse> (BaseUri + "/api/1.0/");
		}

		public static UserResponse GetUserInfo ()
		{
			var api_ref = GetRootApiRef ();
			var user_service_url = api_ref.UserRef.ApiRef;
	
			var rest_client = GetJsonClient ();
		
			return rest_client.Get<UserResponse> (user_service_url);
		}

		// this performs our main OAuth authentication, performing
		// the request token retrieval, authorization, and exchange
		// for an access token
		public static IToken GetAccessToken ()
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
