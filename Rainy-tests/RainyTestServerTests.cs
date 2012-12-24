using System;
using NUnit.Framework;
using Tomboy.Sync.DTO;

namespace Rainy
{
	public class RainyTestServerTests : RainyTestBase
	{
		[Test]
		public void CheckApiRef ()
		{
			var response = RainyTestServer.GetRootApiRef ();

			var rainy_listen_url = RainyTestServer.RainyListenUrl;
			Assert.AreEqual ("1.0", response.ApiVersion);

			// check the OAuth urls
			Assert.That (response.OAuthAccessTokenUrl.StartsWith (rainy_listen_url));
			Assert.That (response.OAuthAuthorizeUrl.StartsWith (rainy_listen_url));
			Assert.That (response.OAuthRequestTokenUrl.StartsWith (rainy_listen_url));

			Assert.That (Uri.IsWellFormedUriString (response.OAuthAccessTokenUrl, UriKind.Absolute));
			Assert.That (Uri.IsWellFormedUriString (response.OAuthRequestTokenUrl, UriKind.Absolute));
			Assert.That (Uri.IsWellFormedUriString (response.OAuthAuthorizeUrl, UriKind.Absolute));
		}


		[Test]
		public void GetUser ()
		{
			var user_response = RainyTestServer.GetUserInfo ();

			Assert.AreEqual (user_response.Username, "johndoe");
			Assert.AreEqual (user_response.LatestSyncRevision, -1);

			Assert.That (Uri.IsWellFormedUriString (user_response.NotesRef.ApiRef, UriKind.Absolute));

		}
	}
}
