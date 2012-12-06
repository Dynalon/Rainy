using System;
using Rainy;
using NUnit.Framework;
using ServiceStack.ServiceClient.Web;
using Rainy.WebService;

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
	}
}
