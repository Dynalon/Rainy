using System;
using System.Net;
using System.Linq;
using DTO = Tomboy.Sync.Web.DTO;
using Rainy.Interfaces;
using Rainy.OAuth;

namespace Rainy.WebService
{
	public class ApiService : ServiceBase
	{
		protected OAuthHandler oauthHandler;
		public ApiService (OAuthHandler oauthHandler) : base ()
		{
			this.oauthHandler = oauthHandler;
		}
		public object Get (ApiRequest request)
		{
			string username = "";
			string password = "";
			bool unattended_auth = false;

			if (!string.IsNullOrEmpty (request.Username) &&
			    !string.IsNullOrEmpty (request.Password)) {
				unattended_auth = true;
				username = request.Username;
				password = request.Password;
			}

			// if the user sends oauth data, we look for an oauth token
			// and checks if is valid. If so, we provide the right user-ref url
			if (Request.Headers.AllKeys.Contains ("Authorization")) {
				var auth_header = Request.Headers["Authorization"];
				var splits = auth_header.Split (new string[] { "oauth_token=\"" }, StringSplitOptions.None);
				var next_quote = splits[1].IndexOf("\"");
				var token_string = splits[1].Substring(0, next_quote);
				var token = oauthHandler.AccessTokens.GetToken (token_string);
				username = token.UserName;
			}

			Logger.Debug ("ApiRequest received");
			var response = new DTO.ApiResponse ();
			var baseUri = ((HttpListenerRequest)this.Request.OriginalRequest).Url;
			string baseUrl = baseUri.Scheme + "://" + baseUri.Authority + "/";

			// should only be set if authenticated
			response.UserRef = new DTO.ContentRef () {
				ApiRef = baseUrl + "api/1.0/" + username,
				Href = baseUrl + username
			};

			response.ApiVersion = "1.0";
			string oauthBaseUrl = baseUrl + "oauth/";
			response.OAuthRequestTokenUrl = oauthBaseUrl + "request_token";
			response.OAuthAccessTokenUrl = oauthBaseUrl + "access_token";
		
			if (unattended_auth)
				response.OAuthAuthorizeUrl = oauthBaseUrl + "authorize/"
					+ username + "/"
					+ password + "/";
			else
				response.OAuthAuthorizeUrl = oauthBaseUrl + "authorize/";

			return response;
		}
	}
}
