using System;
using Rainy;
using NUnit.Framework;
using ServiceStack.ServiceClient.Web;
using Rainy.WebService;
using DevDefined.OAuth.Consumer;
using System.Security.Cryptography;
using DevDefined.OAuth.Framework;
using System.Net;
using System.Linq;

namespace Rainy.Tests
{
	[TestFixture()]
	public class BasicConnection
	{
		private string baseUri = "http://localhost:8080";

		[SetUp]
		public void StartRainyInstance ()
		{
		}

		protected ApiResponse GetRootApiRef (string user_pw_url = "/johndoe/none") 
		{
			var restClient = new JsonServiceClient (baseUri);
			return restClient.Get<ApiResponse> (user_pw_url + "/api/1.0");

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

		[Test()]
		[ExpectedException ("Forbidden")]
		public void UnauthenticatedUserAccessFails()
		{
			var apiResponse = GetRootApiRef ("/wrong/user/");

			var restClient = new JsonServiceClient (baseUri);

			restClient.Get<UserResponse> (apiResponse.UserRef.ApiRef);

		}

		[Test()]
		public void OAuthGetRequestToken ()
		{
			var resp = GetRootApiRef ();
			var consumerContext = new OAuthConsumerContext (){
				ConsumerKey = "anyone"
			};

			var session = new OAuthSession (consumerContext, resp.OAuthRequestTokenUrl,
			                                resp.OAuthAuthorizeUrl, resp.OAuthAccessTokenUrl);

			IToken request_token = session.GetRequestToken ();

			string link = session.GetUserAuthorizationUrlForToken (request_token, "http://example.com/");

			// visit the link to perform the authrization (no interaction needed)
			HttpWebRequest req = (HttpWebRequest) HttpWebRequest.Create (link);
			// disallow auto redirection, since we are interested in the location header only
			req.AllowAutoRedirect = false;

			string location = ((HttpWebResponse) req.GetResponse ()).Headers["Location"];
			Assert.That (!string.IsNullOrEmpty (location));

			var query = string.Join ("", location.Split ('?').Skip(1));
			var oauth_data = System.Web.HttpUtility.ParseQueryString (query);

			Assert.AreEqual (request_token.Token, oauth_data["oauth_token"]);
			Assert.That (!string.IsNullOrEmpty (oauth_data["oauth_verifier"]));
			Assert.That (oauth_data["oauth_verifier"].Length > 12);

			IToken accessToken = session.ExchangeRequestTokenForAccessToken(request_token, oauth_data["oauth_verifier"]);

			Assert.AreNotEqual (accessToken.Token, request_token.Token);
			Assert.AreNotEqual (accessToken.TokenSecret, request_token.TokenSecret);


		}
	}
}
