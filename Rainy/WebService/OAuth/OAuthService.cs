using System;
using ServiceStack.ServiceHost;
using DevDefined.OAuth.Framework;
using System.IO;
using System.Net;
using DevDefined.OAuth.Storage.Basic;
using Rainy.WebService;

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

	[Route("oauth/request_token")]
	public class OAuthRequestTokenRequest : IReturnVoid, IRequiresRequestStream
	{
		public Stream RequestStream { get; set; }
	}

	public class OAuthService : RainyServiceBase
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

	[Route("/oauth/authorize/{Username}/{Password}")]
	public class OAuthAuthorizeRequest : IReturnVoid
	{
		public string Username { get; set; }
		public string Password { get; set; }
	}

	public class OAuthAuthorizeService : RainyServiceBase
	{
		public object Any (OAuthAuthorizeRequest request)
		{
			// keep this line to inspect the Request in monodevelop's debugger 
			// really helps debugging API calls
			var servicestack_http_request = Request;

			// TODO the OAuth spec allows other ways of specifying the parameters besides the query string
			// (i.e. the authorization header, form-encoded POST values, etc. We have to handle those 
			// in the future.
			var original_request = (HttpListenerRequest)Request.OriginalRequest;
			var context = new OAuthContextBuilder ().FromUri (Request.HttpMethod, original_request.Url);

			// check if the user is authorized
			// TODO this is just a basic hack to enable authorization
			if (!userIsAllowed (request.Username, request.Password)) {
				// unauthorized
				Logger.WarnFormat ("Failed to authorize user {0}", request.Username);
				Response.StatusCode = 403;
				Response.StatusDescription ="Authorization failed";
				Response.Close ();
				return null;
			}
			// authorization succeeded, continue
			Logger.InfoFormat ("Successfully authorized user: {0}", request.Username);

			var request_token = Rainy.RainyStandaloneServer.OAuth.RequestTokens.GetToken (context.Token);
			request_token.Verifier = Guid.NewGuid ().ToString ();
			request_token.AccessDenied = false;

			request_token.AccessToken = new AccessToken () {
				ConsumerKey = request_token.ConsumerKey,
				Realm = request_token.Realm,
				Token = Guid.NewGuid ().ToString (),
				TokenSecret = Guid.NewGuid ().ToString (),
				UserName = request.Username,
				ExpiryDate = DateTime.Now.AddYears (99)
			};
		
			RainyStandaloneServer.OAuth.RequestTokens.SaveToken (request_token);
			Logger.DebugFormat ("created an access token for user {0}: {1}", request.Username, request_token);

			// redirect to the provded callback
			var redirect_url = request_token.CallbackUrl + "?oauth_verifier=" + request_token.Verifier + "&oauth_token=" + request_token.Token;
			Logger.DebugFormat ("redirecting user to consumer at: {1}", request.Username, redirect_url);
			Response.Redirect (redirect_url);
			return null;
		}
		protected bool userIsAllowed (string username, string password)
		{
			return RainyStandaloneServer.OAuth.Authenticator (username, password);

		}
	}

	[Route("/oauth/access_token")]
	public class OAuthAccessTokenRequest : IReturnVoid
	{
	}

	public class OAuthAccessTokenService : RainyServiceBase
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