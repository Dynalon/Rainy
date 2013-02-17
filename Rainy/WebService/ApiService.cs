using System;
using System.Net;

namespace Rainy.WebService
{
	public class ApiService : RainyServiceBase
	{
		public ApiService () : base ()
		{
		}
		public object Get (ApiRequest request)
		{
			string username = request.Username;
			string password = request.Password;

			Logger.Debug ("ApiRequest received");
			var response = new Tomboy.Sync.DTO.ApiResponse ();
			var baseUri = ((HttpListenerRequest)this.Request.OriginalRequest).Url;
			string baseUrl = baseUri.Scheme + "://" + baseUri.Authority + "/";

			response.UserRef = new Tomboy.Sync.DTO.ContentRef () {
				ApiRef = baseUrl + "api/1.0/" + username,
				Href = baseUrl + username
			};

			response.ApiVersion = "1.0";
			string oauthBaseUrl = baseUrl + "oauth/";
			response.OAuthRequestTokenUrl = oauthBaseUrl + "request_token";
			response.OAuthAccessTokenUrl = oauthBaseUrl + "access_token";
			// HACK we hardencode the username / password pair into the authorize step
			response.OAuthAuthorizeUrl = oauthBaseUrl + "authorize/" + username + "/" + password + "/";

			return response;
		}
	}
	
}
