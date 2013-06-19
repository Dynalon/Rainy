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
using JsonConfig;

namespace Rainy
{
	// simple server that can be used from within unit tests
	// TODO make non-static
	public class RainyTestServer
	{
		public static string TEST_USER = "test";
		public static string TEST_PASS = "none";
		public static string ADMIN_TEST_PASS = "foobar";
		public string RainyListenUrl = "http://127.0.0.1:8080/";

		public string BaseUri {
			// i.e. http://127.0.0.1:8080/johndoe/none/
			get {
				return RainyListenUrl + TEST_USER + "/" + TEST_PASS + "/";
			}
		}

		private RainyStandaloneServer rainyServer;
		private string tmpPath;

		public RainyTestServer (ComposeObjectGraphDelegate composer)
		{
			rainyServer = new RainyStandaloneServer (RainyListenUrl, composer);
		}

		public void Start ()
		{	
			rainyServer.Start ();
		}
		public void Stop ()
		{
			rainyServer.Dispose ();
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
