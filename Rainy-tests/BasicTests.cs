using System;
using System.Linq;
using NUnit.Framework;
using ServiceStack.ServiceClient.Web;
using DevDefined.OAuth.Consumer;
using DevDefined.OAuth.Framework;
using Tomboy.Sync.DTO;
using Tomboy.Sync.Web;
using ServiceStack.Common;

namespace Rainy.Tests
{

	public class BasicTests : RainyTestBase
	{
		[Test()]
		public void CheckApiRef ()
		{
			var response = GetRootApiRef ();
	
			Assert.AreEqual ("1.0", response.ApiVersion);

			// check the OAuth urls
			Assert.That (response.OAuthAccessTokenUrl.StartsWith (baseUri));
			Assert.That (response.OAuthAuthorizeUrl.StartsWith (baseUri));
			Assert.That (response.OAuthRequestTokenUrl.StartsWith (baseUri));

			Assert.That (Uri.IsWellFormedUriString (response.OAuthAccessTokenUrl, UriKind.Absolute));
			Assert.That (Uri.IsWellFormedUriString (response.OAuthRequestTokenUrl, UriKind.Absolute));
			Assert.That (Uri.IsWellFormedUriString (response.OAuthAuthorizeUrl, UriKind.Absolute));
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
		public void GetUser ()
		{

			var user_response = GetUserInfo ();

			Assert.AreEqual (user_response.Username, "johndoe");
			Assert.AreEqual (user_response.LatestSyncRevision, -1);

			Assert.That (Uri.IsWellFormedUriString (user_response.NotesRef.ApiRef, UriKind.Absolute));

		}

		[Test()]
		public void InvalidNoteSyncRevision ()
		{
			// note sync revision is higher than global sync revision => fail!
			var restClient = new JsonServiceClient ();
			var user = GetUserInfo ();
			restClient.SetAccessToken (GetAccessToken ());

			var request = new PutNotesRequest ();

			var dto_notes = sampleNotes.Select (n => {
				var dto = new DTONote ();
				dto.PopulateWith (n);
				dto.LastSyncRevision = 0;
				return dto;
			}).ToList ();

			request.Notes = dto_notes;
			request.LatestSyncRevision = -1;

			try {
				restClient.Put<PutNotesResponse> (user.NotesRef.ApiRef, request);
			} catch (Exception e) {
				Assert.That (e is WebServiceException);
				return;
			}
			Assert.Fail ();
		}
	}
}
