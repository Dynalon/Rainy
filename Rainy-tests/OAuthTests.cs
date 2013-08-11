using System;
using DevDefined.OAuth.Consumer;
using DevDefined.OAuth.Framework;
using NUnit.Framework;
using ServiceStack.ServiceClient.Web;
using Tomboy.Sync.Web.DTO;
using Rainy.WebService.Management.Admin;
using System.Collections.Generic;
using System.Linq;
using Rainy.OAuth;
using Rainy.WebService.OAuth;

namespace Rainy.Tests.OAuth
{
	[TestFixture()]
	public class OAuthTests : TestBase
	{
		[SetUp]
		public void SetUp ()
		{
			testServer.Start ();
		}
		[Test()]
		public void OAuthGetRequestToken ()
		{
			var consumerContext = new OAuthConsumerContext () {
				ConsumerKey = "anyone"
			};
			var api_ref = testServer.GetRootApiRef ();
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
			IToken access_token = testServer.GetAccessToken ();

			Assert.That (access_token.Token.Length > 14);
			Assert.That (access_token.TokenSecret.Length > 14);
		}
		
		// TODO implement way more security tests
		[Test]
		// since the exception name is returned in the webservice result,
		// we can't use [ExpcetedException] here
		public void AccessFailsWithoutOAuthToken ()
		{
			Exception caught_exception = new Exception ();
			try {
				var apiResponse = testServer.GetRootApiRef ();
				var restClient = new JsonServiceClient (testServer.BaseUri);
				restClient.Get<UserResponse> (apiResponse.UserRef.ApiRef);
				
				// we are not allowed to reach here
				Assert.Fail ();
			} catch (Exception e) {
				caught_exception = e;
			} finally {
				Assert.AreEqual ("Unauthorized", caught_exception.Message);
			}
		}

		[Test]
		public void BypassOAuthForTemporaryAccessToken ()
		{
			var restClient = new JsonServiceClient (testServer.ListenUrl);
			var req = new OAuthTemporaryAccessTokenRequest ();
			req.Username = RainyTestServer.TEST_USER;
			req.Password = RainyTestServer.TEST_PASS;
			var token = restClient.Post<OAuthTemporaryAccessTokenResponse> ("/oauth/temporary_access_token", req);
			Assert.That (!token.AccessToken.StartsWith ("oauth_"));
			Assert.GreaterOrEqual (400, token.AccessToken.Length);
		}
	}

	[TestFixture()]
	public class TokenApiTests : TestBase
	{
		[SetUp]
		public void SetUp ()
		{
			testServer.Start ();
		}
		[Test()]
		public void GetListOfValidAccesstokens ()
		{
			var client = testServer.GetJsonClient ();
			var url = new GetTokenRequest ().ToUrl ("GET");
			var service_url = testServer.ListenUrl + url;
			var tokens = client.Get<List<AccessTokenDto>> (service_url);
			Assert.AreEqual (1, tokens.Count);

			testServer.GetAccessToken ();
			testServer.GetAccessToken ();
			tokens = client.Get<List<AccessTokenDto>> (service_url);
			Assert.AreEqual(3, tokens.Count);

		}
		[Test()]
		public void DeleteAccessTokenRevokesTheToken ()
		{
			var token = testServer.GetAccessToken ();
			var token_part = token.Token.Substring (0, 24);
			var client = testServer.GetJsonClient ();
			var url = new DeleteTokenRequest () {
				TokenPart = token_part
			}.ToUrl ("DELETE");
			var service_url = testServer.ListenUrl + url;
			client.Delete<AccessTokenDto> (service_url);

			// check that the token has been delete from the db
			var get_url = new GetTokenRequest () { Username = RainyTestServer.TEST_USER }.ToUrl ("GET");
			var get_service_url = testServer.ListenUrl + get_url;
			List<AccessTokenDto> tokens = client.Get<List<AccessTokenDto>> (get_service_url);

			var the_token = tokens.Where (t => token.Token.StartsWith (t.TokenPart));
			Assert.That (the_token.Count () == 0);
		}

		[Test]
		public void UpdateTokenDeviceName ()
		{
			string new_device_name = "My home computer";

			var token = testServer.GetAccessToken ().Token.ToShortToken ();
			var client = testServer.GetJsonClient ();
			var url = new UpdateTokenRequest ().ToUrl ("PUT");
			var req = new AccessTokenDto { 
				TokenPart = token,
				DeviceName = new_device_name };
			var service_url = testServer.ListenUrl + url;
			client.Put<AccessTokenDto> (service_url, req);

			// check that the token has been updated
			var get_url = new GetTokenRequest () { Username = RainyTestServer.TEST_USER }.ToUrl ("GET");
			var get_service_url = testServer.ListenUrl + get_url;
			List<AccessTokenDto> tokens = client.Get<List<AccessTokenDto>> (get_service_url);
			var updated_token = tokens.First (t => token == t.TokenPart);

			Assert.AreEqual (new_device_name, updated_token.DeviceName);
		}
	}
}

