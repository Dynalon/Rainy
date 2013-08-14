using ServiceStack.ServiceHost;
using System.IO;
using System;

namespace Rainy.WebService.OAuth
{
	
	[Route("/oauth/request_token", "GET, POST")]
	public class OAuthRequestTokenRequest : IReturnVoid, IRequiresRequestStream
	{
		public Stream RequestStream { get; set; }
	}

	[Route("/oauth/authorize/", "GET")]
	[Route("/oauth/authorize/{Username}/{Password}/", "GET")]
	public class OAuthAuthorizeRequest : IReturnVoid
	{
		public string Username { get; set; }
		public string Password { get; set; }
	}

	[Route("/oauth/access_token", "GET,POST")]
	// returns void since we will write the access token to the output manually
	public class OAuthAccessTokenRequest : IReturnVoid
	{
	}

	// The authenticate service is NOT part of the Tomboy/Rainy/SNowy/OAuth standard but rather a helper
	// service that we can call via JSON from Javascript to authenticate a user. The verifier
	// we receive is our proof to the server that we authenticated successfully
	[Route("/oauth/authenticate", "POST")]
	public class OAuthAuthenticateRequest : IReturn<OAuthAuthenticateResponse>
	{
		public string Username { get; set; }
		public string Password { get; set; }
		public string RequestToken { get; set; }
	}
	
	public class OAuthAuthenticateResponse
	{
		public string Verifier { get; set; }
		public string RedirectUrl { get; set; }
	}

	[Route("/oauth/temporary_access_token", "POST")]
	public class OAuthTemporaryAccessTokenRequest : IReturn<OAuthTemporaryAccessTokenResponse>
	{
		[ApiMember (Description="Can either be the username or email the user is registered with",
		            DataType="string")]
		public string Username { get; set; }

		[ApiMember (Description="Password", DataType="string")]
		public string Password { get; set; }
	
		[ApiMember (Description="Amount of minutes after issuing after which the token will become invalid.",
		            DataType="integer")]
		public int Expiry { get; set; }

	}
	public class OAuthTemporaryAccessTokenResponse
	{
		public string AccessToken { get; set; }
		public DateTime ValidUntil { get; set; }
	}
}
