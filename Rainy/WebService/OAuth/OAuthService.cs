using System;
using System.IO;
using System.Net;
using DevDefined.OAuth.Framework;
using DevDefined.OAuth.Storage.Basic;
using ServiceStack.ServiceHost;
using ServiceStack.Text;
using Rainy.ErrorHandling;
using Rainy.WebService;
using Rainy.OAuth;
using Rainy.Interfaces;
using System.Security.Cryptography;
using Rainy.Crypto;
using Rainy.Db;
using ServiceStack.OrmLite;
using ServiceStack.WebHost.Endpoints.Extensions;

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

	[RequestLogFilter]
	[ResponseLogFilter]
	public class OAuthRequestTokenService : ServiceBase
	{
		private OAuthHandler oauthHandler;
		public OAuthRequestTokenService (OAuthHandler oauthHandler) : base ()
		{
			this.oauthHandler = oauthHandler;
		}
		public object Get (OAuthRequestTokenRequest request)
		{
			HttpWebRequest original_request = ((HttpListenerRequest)Request.OriginalRequest).ToWebRequest ();

			// it is not fatal that we do not really check the oauth signature for the request token request 
			// all it takes to sign such a request is the consumer secret, which is "anyone" and hard-coded
			// into the tomboy and rainy source code - and we are open source so everybody knows it anyways
			IOAuthContext context;
			context = new OAuthContextBuilder ().FromWebRequest (original_request, request.RequestStream);

			IToken token = oauthHandler.Provider.GrantRequestToken (context);
			Logger.DebugFormat ("granting request token {0} to consumer", token);
			Response.StatusCode = 200;
			Response.Write (token.ToString ());
			Response.End ();

			return null;
		}
		public object Post (OAuthRequestTokenRequest request)
		{
			return Get (request);
			// i.e. ConBoy only supported POST Auth which is valid accoding to the OAuth RFC, but not yet
			// supported in Rainy
			//throw new RainyBaseException () {ErrorMessage = "Usage of POST for OAuth authorization is currently not supported. Use GET instead."};
		}
	}

	public class OAuthAuthenticateService : ServiceBase
	{
		OAuthHandler oauthHandler;
		IAuthenticator Authenticator;
		IDbConnectionFactory connFactory;
		public OAuthAuthenticateService (IDbConnectionFactory factory, OAuthHandler oauthHandler, IAuthenticator auth) : base ()
		{
			this.connFactory = factory;
			this.Authenticator = auth;
			this.oauthHandler = oauthHandler;
		}
		public object Post (OAuthAuthenticateRequest request)
		{
			// check if the user is authorized
			string username = request.Username;

			if (username == null || !userIsAllowed (username, request.Password, out username)) {
				// unauthorized
				Logger.WarnFormat ("Failed to authenticate user {0}", username);
				Response.StatusCode = 403;
				Response.StatusDescription ="Authorization failed";
				Response.ApplyGlobalResponseHeaders ();
				Response.Write (
					"<html><h1 style='margin-top: 1em'>Authorization failed for user "
					+ "<b>" + request.Username + "</b>"
					+ " (maybe wrong password?).</h1></html>"
					);
				Response.EndServiceStackRequest ();
				return null;
			}
			// authentication successful
			Logger.InfoFormat ("Successfully authorized user: {0}", username);

			return TokenExchangeAfterAuthentication (username, request.Password, request.RequestToken);
		}
		public object Post (OAuthTemporaryAccessTokenRequest request)
		{
			string username = request.Username;
			string password = request.Password;
			if (userIsAllowed (username, password, out username)) {
				var access_token = GenerateAccessToken (username, password, DateTime.Now.AddDays (1));
				// save the access token
				var db_access_token = access_token.ToDBAccessToken ();
				// shorten the token for crypto
				db_access_token.Token = access_token.Token.ToShortToken ();
				using (var db = connFactory.OpenDbConnection ()) {
					db.Save<DBAccessToken> (db_access_token);
				}
				return new OAuthTemporaryAccessTokenResponse { AccessToken = access_token.Token };
			} else {
				throw new UnauthorizedException ();
			}
		}

		public object TokenExchangeAfterAuthentication (string username, string password, string token)
		{
			var response = new OAuthAuthenticateResponse ();
			var rng = new RNGCryptoServiceProvider ();

			// TODO surround with try/catch and present 403 or 400 if token is unknown/invalid
			var request_token = oauthHandler.RequestTokens.GetToken (token);

			// the verifier is important, it is proof that the user successfully authorized
			// the verifier is later tested by the OAuth10aInspector to macht
			request_token.Verifier = rng.Create256BitLowerCaseHexKey ();
			request_token.AccessDenied = false;

			var access_token = GenerateAccessToken (username, password);
			request_token.AccessToken = access_token;

			oauthHandler.RequestTokens.SaveToken (request_token);
			Logger.DebugFormat ("created an access token for user {0}: {1}", username, token);
	
			// redirect to the provded callback
			var redirect_url = request_token.CallbackUrl + "?oauth_verifier=" + request_token.Verifier
				+ "&oauth_token=" + request_token.Token;
		
			response.RedirectUrl = redirect_url;

			// the browser/gateway page should take the RedirectUrl and access it
			// note that the redirect url points to a tomboy listener, or tomdroid listener (tomdroid://...)
			return response;
		}
		private AccessToken GenerateAccessToken (string username, string password, DateTime? expiry = null)
		{
			if (!expiry.HasValue)
				expiry = DateTime.Now.AddYears (99);

			var rng = new RNGCryptoServiceProvider ();
			string access_token_secret = rng.Create256BitLowerCaseHexKey ();
			string token_key = rng.Create256BitLowerCaseHexKey ();

			// the token is the master key encrypted with the token key
			string access_token_token;
			using (var db = connFactory.OpenDbConnection ()) {
				DBUser user = db.First<DBUser> (u => u.Username == username);
				string master_key = user.GetPlaintextMasterKey (password).ToHexString ();
				access_token_token = master_key.EncryptWithKey (token_key, user.MasterKeySalt);
			}

			var access_token = new AccessToken () {
				ConsumerKey = "anyone",
				Realm = "Rainy",
				Token = access_token_token,
				TokenSecret = access_token_secret,
				UserName = username,
				ExpiryDate = expiry.Value
			};
			access_token.SetTokenKey (token_key);
			return access_token;
		}

		protected bool userIsAllowed (string username, string password, out string username_out)
		{
			if (username.Contains ("@")) {
				// user supplied email address, lookup the username
				using (var db = connFactory.OpenDbConnection ()) {
					username_out = db.FirstOrDefault<DBUser> (u => u.EmailAddress == username).Username;
				}
			} else {
				username_out = username;
			}
			return Authenticator.VerifyCredentials (username, password);
		}
	}

	[RequestLogFilter]
	[ResponseLogFilter]
	public class OAuthAuthorizeService : ServiceBase
	{
		OAuthHandler oauthHandler;
		IAuthenticator authenticator;
		IDbConnectionFactory connFactory;
		public OAuthAuthorizeService (IDbConnectionFactory factory, OAuthHandler oauthHandler, IAuthenticator auth) : base ()
		{
			this.connFactory = factory;
			this.authenticator = auth;
			this.oauthHandler = oauthHandler;
		}
		public object Any (OAuthAuthorizeRequest request)
		{
			if (!string.IsNullOrEmpty (request.Username) &&
			    !string.IsNullOrEmpty (request.Password)) {

				// unattended authentication, immediately perform token exchange
				// and use data from the querystring

				bool is_allowed = authenticator.VerifyCredentials (request.Username, request.Password);
				if (!is_allowed) {
					throw new UnauthorizedException ();
				}
				
				var auth_service = new OAuthAuthenticateService (connFactory, oauthHandler, authenticator);
				var resp = (OAuthAuthenticateResponse) auth_service.TokenExchangeAfterAuthentication (
					request.Username,
					request.Password,
					Request.QueryString["oauth_token"]
				);
				Response.Redirect (resp.RedirectUrl);
				return null;
			} else {
				// take all url parameters and redirect to the login page
				string prams =  new Uri (Request.RawUrl).PathAndQuery.Split (new char[] { '?' })[1];
				Response.Redirect ("/ui/manage.html#/login?" + prams);
				Response.EndServiceStackRequest ();
				return null;
			}
		}
	}

	[RequestLogFilter]
	[ResponseLogFilter]
	public class OAuthAccessTokenService : ServiceBase
	{
		OAuthHandler oauthHandler;
		IDbConnectionFactory connFactory;
		public OAuthAccessTokenService (IDbConnectionFactory factory, OAuthHandler oauthHandler) : base ()
		{
			this.oauthHandler = oauthHandler;
			this.connFactory = connFactory;
		}
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

				AccessToken access_token = (AccessToken) oauthHandler.Provider.ExchangeRequestTokenForAccessToken (context);
				Logger.DebugFormat ("permanently authorizing access token: {0}", access_token);
				oauthHandler.AccessTokens.SaveToken (access_token);

				Response.Write (access_token.ToString ());
				Response.End ();
			} catch (Exception e) {
				throw new UnauthorizedException (){ ErrorMessage = "failed to exchange request token for access token: {0}".Fmt(e.Message)};
			}
			return null;
		}
	}
}