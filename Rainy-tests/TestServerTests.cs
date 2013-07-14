using System;
using NUnit.Framework;
using Tomboy.Sync.Web.DTO;
using Rainy.Tests;

namespace Rainy.Tests
{
	public class TestServerTests : TestBase
	{
		[SetUp]
		public void SetUp ()
		{
			testServer.ScenarioSqlite ();
			testServer.Start ();
		}
		[Test]
		public void CheckApiRef ()
		{
			var response = testServer.GetRootApiRef ();

			var rainy_listen_url = testServer.ListenUrl;
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
			var user_response = testServer.GetUserInfo ();

			Assert.AreEqual (user_response.Username, RainyTestServer.TEST_USER);
			Assert.AreEqual (user_response.LatestSyncRevision, -1);

			Assert.That (Uri.IsWellFormedUriString (user_response.NotesRef.ApiRef, UriKind.Absolute));

		}
	}
}
