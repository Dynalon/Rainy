using System;
using ServiceStack.ServiceHost;
using ServiceStack.Text;
using DevDefined.OAuth.Framework;
using System.IO;
using System.Net;
using DevDefined.OAuth.Storage.Basic;
using Rainy.WebService;
using log4net;
using Rainy.OAuth;
using ServiceStack.WebHost.Endpoints;
using Rainy.Crypto;
using System.Linq;
using Rainy.ErrorHandling;
using System.Threading;

namespace Rainy.WebService.OAuth
{
	
	public class OAuthRequiredAttribute : Attribute, IHasRequestFilter
	{
		protected ILog Logger;

		public OAuthRequiredAttribute ()
		{
			Logger = LogManager.GetLogger (GetType ());
		}
		public IHasRequestFilter Copy ()
		{
			return this;
		}
		public int Priority { get { return 100; } }
		
		public void RequestFilter (IHttpRequest request, IHttpResponse response, object requestDto)
		{
			bool use_temp_access_token = request.Headers.AllKeys.Contains ("AccessToken");
			bool check_oauth_signature = request.Headers.AllKeys.Contains ("Authorization");

			string username = "";
			if (requestDto is UserRequest) {
				username = ((UserRequest)requestDto).Username;
			} else if (requestDto is GetNotesRequest) {
				username = ((GetNotesRequest)requestDto).Username;
			} else if (requestDto is PutNotesRequest) {
				username = ((PutNotesRequest)requestDto).Username;
			} else if (!check_oauth_signature && !use_temp_access_token) {
				throw new UnauthorizedException ();
			}
			Logger.Debug ("trying to acquire authorization");

			IOAuthContext context = null;
			AccessToken access_token;

			try {
				var oauthHandler = EndpointHost.Container.Resolve<OAuthHandler> ();
				if (check_oauth_signature) {
					var web_request = ((HttpListenerRequest)request.OriginalRequest).ToWebRequest ();
					context = new OAuthContextBuilder ().FromWebRequest (web_request, new MemoryStream ());
					// HACK ServiceStack does not inject into custom attributes
					oauthHandler.Provider.AccessProtectedResourceRequest (context);
					// check if the access token matches the username given in an url
					access_token = oauthHandler.AccessTokens.GetToken (context.Token);
				} else {
					access_token = oauthHandler.AccessTokens.GetToken (request.Headers["AccessToken"]);
				}

				if (!string.IsNullOrEmpty (username) && access_token.UserName != username) {
					// forbidden
					Logger.Debug ("username does not match the one in the access token, denying");
					throw new UnauthorizedException ();
				} else {
					// TODO remove checks - why is it run twice?
					if (!request.Items.Keys.Contains ("AccessToken")) {
						if (use_temp_access_token)
							request.Items.Add ("AccessToken", request.Headers["AccessToken"]);
						else
							request.Items.Add ("AccessToken", context.Token);
					}
					if (!request.Items.Keys.Contains ("Username"))
						request.Items.Add ("Username", access_token.UserName);
				}
			} catch (Exception e) {
				if (context != null)
					Logger.DebugFormat ("failed to obtain authorization, oauth context is: {0}", context.Dump ());
				throw new UnauthorizedException ();
			}

			Logger.DebugFormat ("authorization granted for user {0}", username);

			// possible race condition but locking is to expensive
			// at this point, rather accept non-precise values
			MainClass.ServedRequests++;
		}
	}
	
}