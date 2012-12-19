using System;
using ServiceStack.ServiceClient.Web;
using DevDefined.OAuth.Consumer;
using DevDefined.OAuth.Framework;
using System.Net;
using System.Linq;
using Tomboy.Sync.DTO;
using Rainy.OAuth;
using System.IO;

namespace Rainy.Tests
{

	// simple server that can be used for unit tests
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
			IDataBackend backend = new RainyFileSystemDataBackend (tmpPath);
			
			rainyServer = new RainyStandaloneServer (handler, backend, RainyListenUrl);

			rainyServer.Start ();
			
		}
		public static void StopRainyStandaloneServer ()
		{
			rainyServer.Stop ();
			Directory.Delete (tmpPath, true);
		}

/*

		protected void SetupSampleManifest ()
		{
			localManifest = new SyncManifest ();
			localManifest.LastSyncDate = DateTime.MinValue + new TimeSpan (0, 0, 0, 1, 0);
			localManifest.LastSyncRevision = -1;
		}

		protected ApiResponse GetRootApiRef () 
		{
			var restClient = new JsonServiceClient ();

			return restClient.Get<ApiResponse> (baseUri + "/api/1.0/");
		}

		protected UserResponse GetUserInfo ()
		{
			var api_ref = GetRootApiRef ();
			var user_service_url = api_ref.UserRef.ApiRef;
		
			var restClient = new JsonServiceClient (baseUri);
			restClient.SetAccessToken (this.GetAccessToken ());
		
			return restClient.Get<UserResponse> (user_service_url);
		}

*/

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
