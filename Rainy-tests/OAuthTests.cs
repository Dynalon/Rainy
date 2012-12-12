using System;
using NUnit.Framework;
using DevDefined.OAuth.Consumer;
using DevDefined.OAuth.Framework;

namespace Rainy.Tests
{
	[TestFixture()]
	public class OAuthTests : RainyTestBase
	{
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
	}
}

