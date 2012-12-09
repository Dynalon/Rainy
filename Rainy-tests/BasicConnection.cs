using System;
using NUnit.Framework;
using ServiceStack.ServiceClient.Web;
using DevDefined.OAuth.Consumer;
using DevDefined.OAuth.Framework;
using System.Net;
using System.Linq;
using Tomboy.Sync.DTO;
using Rainy.OAuth;
using System.IO;
using System.Threading;

namespace Rainy.Tests
{
	[TestFixture()]
	public class BasicTests
	{
		private string baseUri = "http://127.0.0.1:8080/";
		private RainyStandaloneServer rainyServer;
		private string tmpPath;

		[SetUp]
		public virtual void SetUp ()
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

			rainyServer = new RainyStandaloneServer (handler, backend);
			rainyServer.Port = 8080;
			rainyServer.Hostname = "127.0.0.1";
			rainyServer.Start ();

		}
		[TearDown]
		public virtual void TearDown ()
		{
			rainyServer.Stop ();
			Directory.Delete (tmpPath, true);

		}

		protected ApiResponse GetRootApiRef (string user_pw_url = "/johndoe/none") 
		{
			var restClient = new JsonServiceClient (baseUri);

			return restClient.Get<ApiResponse> (user_pw_url + "/api/1.0");

		}
		// this performs our main OAuth authentication, performing
		// the request token retrieval, authorization, and exchange
		// for an access token
		protected IToken GetAccessToken ()
		{
			var consumerContext = new OAuthConsumerContext () {
				ConsumerKey = "anyone"
			};
			
			var api_ref = GetRootApiRef ();
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

			// TODO the verifier should be checked against previous verifier to
			// make sure there was no man-in-the-middle attack
			Assert.AreEqual (request_token.Token, oauth_data ["oauth_token"]);
			Assert.That (!string.IsNullOrEmpty (oauth_data ["oauth_verifier"]));
			Assert.That (oauth_data ["oauth_verifier"].Length > 12);
			
			return access_token;
		}

		[Test()]
		public void CheckApiRef ()
		{
			var response = GetRootApiRef ();
	
			Assert.AreEqual ("1.0", response.ApiVersion);

			// check the OAuth urls
			Assert.That (response.OAuthAccessTokenUrl.StartsWith (baseUri));
			Assert.That (response.OAuthAuthorizeUrl.StartsWith (baseUri));
			Assert.That (response.OAuthRequestTokenUrl.StartsWith (baseUri));
		}

		// TODO implement way more security tests
		[Test()]
		// since the exception name is returned in the webservice result,
		// we can't use [ExpcetedException] here
		public void UnauthenticatedUserAccessFails()
		{
			Exception caught_exception = new Exception ();
			try {
				var apiResponse = GetRootApiRef ("/wrong/user/");
				var restClient = new JsonServiceClient (baseUri);

				restClient.Get<UserResponse> (apiResponse.UserRef.ApiRef);

				// we are not allowed to reach here
				Assert.Fail ();
			} catch (Exception e) {
				caught_exception = e;
			} finally {
				Assert.AreEqual ("Unauthorized", caught_exception.Message);
			}
		}

		[Test()]
		public void OAuthGetRequestToken ()
		{
			var consumerContext = new OAuthConsumerContext () {
				ConsumerKey = "anyone"
			};
			var api_ref = GetRootApiRef ();
			var session = new OAuthSession (consumerContext, api_ref.OAuthRequestTokenUrl,
			                                api_ref.OAuthAuthorizeUrl, api_ref.OAuthAccessTokenUrl);

			IToken request_token = session.GetRequestToken ();

			// consumerkey "anyone" is hardcoded into tomboy
			Assert.AreEqual ("anyone", request_token.ConsumerKey);

			// tokens are of secure length
			Assert.That (request_token.Token.Length > 14);
			Assert.That (request_token.TokenSecret.Length > 14);
		}

		[Test()]
		public void OAuthFullTokenExchange ()
		{
			// the actual unit under test is GetAccessToken, but we
			// need it so often so it is its own method
			IToken access_token = GetAccessToken ();
	
			Assert.That (access_token.Token.Length > 14);
			Assert.That (access_token.TokenSecret.Length > 14);
		}

		[Test()]
		public void GetUserInfo ()
		{
			var api_ref = GetRootApiRef ();
			var user_service_url = api_ref.UserRef.ApiRef;

			var restClient = new JsonServiceClient (baseUri);
			restClient.SetAccessToken (GetAccessToken ());
		
			var response = restClient.Get<UserResponse> (user_service_url);

			Assert.AreEqual (response.Username, "johndoe");
			Assert.AreEqual (response.LatestSyncRevision, -1);
		}
	}

	public static class OAuthRestHelper
	{
		public static void SetAccessToken (this JsonServiceClient client, IToken access_token)
		{
			// we use a request filter to add the required OAuth header
			client.LocalHttpWebRequestFilter += webservice_request => {
				
				OAuthConsumerContext consumer_context = new OAuthConsumerContext ();
				
				consumer_context.SignatureMethod = "HMAC-SHA1";
				consumer_context.ConsumerKey = access_token.ConsumerKey;
				consumer_context.ConsumerSecret = "anyone";
				consumer_context.UseHeaderForOAuthParameters = true;

				// the OAuth process creates a signature, which uses several data from
				// the web request like method, hostname, headers etc.
				OAuthContext request_context = new OAuthContext ();
				request_context.Headers = webservice_request.Headers;
				request_context.RequestMethod = webservice_request.Method;
				request_context.RawUri = webservice_request.RequestUri;

				// now create the signature for that context
				consumer_context.SignContextWithToken (request_context, access_token);

				// BUG TODO the oauth_token is not included when generating the header,
				// this is a bug ing DevDefined.OAuth. We add it manually as a workaround
				request_context.AuthorizationHeaderParameters.Add ("oauth_token", access_token.Token);

				string oauth_header = request_context.GenerateOAuthParametersForHeader ();

				webservice_request.Headers.Add ("Authorization", oauth_header);

			};
		}
	}
}
