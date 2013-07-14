using System;
using DevDefined.OAuth.Consumer;
using DevDefined.OAuth.Framework;
using NUnit.Framework;
using ServiceStack.ServiceClient.Web;
using Tomboy.Sync.Web.DTO;
using Rainy.WebService.Management.Admin;
using System.Collections.Generic;

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
			//testServer.GetAccessToken ();
			var client = testServer.GetJsonClient ();
			var url = new GetTokenRequest () { Username = RainyTestServer.TEST_USER }.ToUrl ("GET");
			var service_url = testServer.ListenUrl + url;
			var tokens = client.Get<List<AccessTokenDto>> (service_url);
		}
	}
}

