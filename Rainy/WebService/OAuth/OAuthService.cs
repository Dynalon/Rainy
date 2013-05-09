using System;
using ServiceStack.ServiceHost;
using DevDefined.OAuth.Framework;
using System.IO;
using System.Net;
using DevDefined.OAuth.Storage.Basic;
using Rainy.WebService;
using System.Linq;
using ServiceStack.Common.Web;
using Rainy.ErrorHandling;

namespace Rainy.WebService.OAuth
{
	public static class ExtensionMethods {
		// DevDefine.OAuth constructs the context from a HttpWebRequest, but in ServiceStack standalone mode
		// we only get HttpListenerRequest's - use this extension method to convert
		public static HttpWebRequest ToWebRequest (this HttpListenerRequest listener_request)
		{
			// convert httplistener to webrequest
			WebRequest web_request = HttpWebRequest.Create (listener_request.Url);
			web_request.Method = listener_request.HttpMethod;
			web_request.Headers.Add ("Authorization", listener_request.Headers ["Authorization"]);
			return (System.Net.HttpWebRequest)web_request;
		}
	}

	[Route("/oauth/request_token")]
	public class OAuthRequestTokenRequest : IReturnVoid, IRequiresRequestStream
	{
		public Stream RequestStream { get; set; }
	}

	[RequestLogFilter]
	[ResponseLogFilter]
	public class OAuthService : RainyNoteServiceBase
	{
		public object Any (OAuthRequestTokenRequest request)
		{
			// keep this line to inspect the Request in monodevelop's debugger 
			// really helps debugging API calls
			var servicestack_http_request = Request;

			HttpWebRequest original_request = ((HttpListenerRequest)Request.OriginalRequest).ToWebRequest ();

			try {
				IOAuthContext context = new OAuthContextBuilder ().FromWebRequest (original_request, request.RequestStream);
				IToken token = RainyStandaloneServer.OAuth.Provider.GrantRequestToken (context);
				Logger.DebugFormat ("granting request token {0} to consumer", token);

				Response.StatusCode = 200;
				Response.Write (token.ToString ());
			} catch (Exception e) {
				Logger.ErrorFormat ("Caught exception: {0}", e.Message);
				Response.StatusCode = 500;
				Response.StatusDescription = e.Message;
			} finally {
				Response.Close ();
			}
			return null;
		}
	}


	// The authenticate server is NOT part of the Tomboy/Rainy/SNowy/OAuth standard but rather a helper
	// service that we can call via JSON from Javascript to authenticate a user. The verifier
	// we receive is our proof to the server that we authenticated successfully
	[Route("/oauth/authenticate")]
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
	public class OAuthAuthenticateService : RainyNoteServiceBase
	{
		public object Get (OAuthAuthenticateRequest request)
		{
			// check if the user is authorized
			if (!userIsAllowed (request.Username, request.Password)) {
				// unauthorized
				Logger.WarnFormat ("Failed to authenticate user {0}", request.Username);
				Response.StatusCode = 403;
				Response.StatusDescription ="Authorization failed";
				Response.Write (
					"<html><h1 style='margin-top: 1em'>Authorization failed for user "
					+ "<b>" + request.Username + "</b>"
					+ " (maybe wrong password?).</h1></html>"
					);
				Response.Close ();
				return null;
			}
			// authentication successful
			Logger.InfoFormat ("Successfully authorized user: {0}", request.Username);

			return TokenExchangeAfterAuthentication (request.Username, request.Password, request.RequestToken);
		}
		public object TokenExchangeAfterAuthentication (string username, string password, string token)
		{
			var response = new OAuthAuthenticateResponse ();

			// TODO surround with try/catch and present 403 or 400 if token is unknown/invalid
			var request_token = Rainy.RainyStandaloneServer.OAuth.RequestTokens.GetToken (token);

			// the verifier is important, it is proof that the user successfully authorized
			// the verifier is later tested by the OAuth10aInspector to macht
			request_token.Verifier = Guid.NewGuid ().ToString ();
			request_token.AccessDenied = false;

			request_token.AccessToken = new AccessToken () {
				ConsumerKey = request_token.ConsumerKey,
				Realm = request_token.Realm,
				Token = Guid.NewGuid ().ToString (),
				TokenSecret = Guid.NewGuid ().ToString (),
				UserName = username,
				ExpiryDate = DateTime.Now.AddYears (99)
			};

			RainyStandaloneServer.OAuth.RequestTokens.SaveToken (request_token);
			Logger.DebugFormat ("created an access token for user {0}: {1}", username, token);
	
			// redirect to the provded callback
			var redirect_url = request_token.CallbackUrl + "?oauth_verifier=" + request_token.Verifier
				+ "&oauth_token=" + request_token.Token;
		
			response.RedirectUrl = redirect_url;

			// the browser/gateway page should take the RedirectUrl and access it
			// note that the redirect url points to a tomboy listener, or tomdroid listener (tomdroid://...)
			return response;
		}

		protected bool userIsAllowed (string username, string password)
		{
			return RainyStandaloneServer.OAuth.Authenticator (username, password);
		}
	}


	[Route("/oauth/authorize/")]
	[Route("/oauth/authorize/{Username}/{Password}/")]
	public class OAuthAuthorizeRequest : IReturnVoid
	{
		public string Username { get; set; }
		public string Password { get; set; }
	}

	[RequestLogFilter]
	[ResponseLogFilter]
	public class OAuthAuthorizeService : RainyNoteServiceBase
	{
		public object Any (OAuthAuthorizeRequest request)
		{
			if (!string.IsNullOrEmpty (request.Username) &&
			    !string.IsNullOrEmpty (request.Password)) {

				// unattended authentication, immediately perform token exchange
				// and use data from the querystring

				bool is_allowed = RainyStandaloneServer.OAuth.Authenticator (request.Username, request.Password);
				if (!is_allowed) {
					throw new UnauthorizedException ();
				}
				
				var auth_service = new OAuthAuthenticateService ();
				var resp = (OAuthAuthenticateResponse) auth_service.TokenExchangeAfterAuthentication (
					request.Username,
					request.Password,
					Request.QueryString["oauth_token"]
				);
				Response.Redirect (resp.RedirectUrl);
				return null;
			} else {
				TextReader reader = new StreamReader ("/Users/td/gateway.html");
				string resp = reader.ReadToEnd ();
				reader.Close ();
				return resp;
			}
		}

	}

	[Route("/oauth/access_token")]
	public class OAuthAccessTokenRequest : IReturnVoid
	{
	}

	[RequestLogFilter]
	[ResponseLogFilter]
	public class OAuthAccessTokenService : RainyNoteServiceBase
	{
		public object Any (OAuthAccessTokenRequest request)
		{
			// keep this line to inspect the Request in monodevelop's debugger 
			// really helps debugging API calls
			var servicestack_http_request = this.Request;
			
			// TODO the OAuth spec allows other ways of specifying the parameters besides the query string
			// (i.e. the authorization header, form-encoded POST values, etc. We have to handle those 
			// in the future
			var original_request = ((HttpListenerRequest)Request.OriginalRequest).ToWebRequest ();

			try {
				var context = new OAuthContextBuilder ()
					.FromWebRequest (original_request, new MemoryStream ());
				AccessToken access_token = (AccessToken) RainyStandaloneServer.OAuth.Provider.ExchangeRequestTokenForAccessToken (context);

				Logger.DebugFormat ("permanently authorizting access token: {0}", access_token);
				RainyStandaloneServer.OAuth.AccessTokens.SaveToken (access_token);
				Response.Write (access_token.ToString ());

			} catch (Exception e) {
				Logger.ErrorFormat ("failed to exchange request token for access token, exception was: {0}", e.Message);
				Response.StatusCode = 500;
				Response.StatusDescription = e.Message;
			} finally {
				Response.Close ();
			}
			return null;
		}
	}
}