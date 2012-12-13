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
using Tomboy.Sync.Web;
using System.Collections.Generic;
using Tomboy;
using Tomboy.Sync;

namespace Rainy.Tests
{
	public class RainyTestBase
	{
		protected string baseUri = "http://127.0.0.1:8080/";
		protected RainyStandaloneServer rainyServer;
		protected string tmpPath;

		// helper var, swith to false when using u1/snowy/external rainy
		private bool useOwnRainyInstance = true; 

		protected IList<Note> sampleNotes;
		protected SyncManifest localManifest;

		[SetUp]
		public void SetUp ()
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

			if (useOwnRainyInstance)
				rainyServer.Start ();

			SetupSampleNotes ();
			SetupSampleManifest ();
		}

		[TearDown]
		public void TearDown ()
		{
			if (useOwnRainyInstance) {
				rainyServer.Stop ();
				Directory.Delete (tmpPath, true);
			}

		}

		protected void SetupSampleNotes ()
		{
			sampleNotes = new List<Note> ();

			sampleNotes.Add(new Note () {
				Title = "Sämplé title 1!",
				Text = "** This is the text of Sämple Note 1**",
				CreateDate = DateTime.Now,
				MetadataChangeDate = DateTime.Now,
				ChangeDate = DateTime.Now
			});

			sampleNotes.Add(new Note () {
				Title = "2nd Example",
				Text = "This is the text of the second sample note",
				CreateDate = new DateTime (1984, 04, 14, 4, 32, 0, DateTimeKind.Utc),
				ChangeDate = new DateTime (2012, 04, 14, 4, 32, 0, DateTimeKind.Utc),
				MetadataChangeDate = new DateTime (2012, 12, 12, 12, 12, 12, DateTimeKind.Utc),
			});

			sampleNotes.Add(new Note () {
				Title = "3rd exampel title",
				Text = "Another example note",
				CreateDate = DateTime.MinValue,
				ChangeDate = DateTime.MinValue,
				MetadataChangeDate = DateTime.MinValue
			});
		}

		protected void SetupSampleManifest ()
		{
			localManifest = new SyncManifest ();
			localManifest.LastSyncDate = DateTime.MinValue;
			localManifest.LastSyncRevision = -1;
		}

		protected ApiResponse GetRootApiRef (string user_pw_url = "/johndoe/none") 
		{
			var restClient = new JsonServiceClient (baseUri);

			return restClient.Get<ApiResponse> (user_pw_url + "/api/1.0");
		}

		protected UserResponse GetUserInfo ()
		{
			var api_ref = GetRootApiRef ();
			var user_service_url = api_ref.UserRef.ApiRef;
		
			var restClient = new JsonServiceClient (baseUri);
			restClient.SetAccessToken (this.GetAccessToken ());
		
			return restClient.Get<UserResponse> (user_service_url);
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
	}
}
