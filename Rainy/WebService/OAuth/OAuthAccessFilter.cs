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
			string username = "";
			if (requestDto is UserRequest) {
				username = ((UserRequest)requestDto).Username;
			} else if (requestDto is GetNotesRequest) {
				username = ((GetNotesRequest)requestDto).Username;
			} else if (requestDto is PutNotesRequest) {
				username = ((PutNotesRequest)requestDto).Username;
			} else {
				response.ReturnAuthRequired ();
				return;
			}
			
			var web_request = ((HttpListenerRequest)request.OriginalRequest).ToWebRequest ();
			IOAuthContext context = new OAuthContextBuilder ().FromWebRequest (web_request, new MemoryStream ());


			// HACK ServiceStack does not inject into custom attributes
			var oauthHandler = EndpointHost.Container.Resolve<OAuthHandler> ();

			try {
				Logger.Debug ("trying to acquire authorization");
				oauthHandler.Provider.AccessProtectedResourceRequest (context);

				// check if the access token matches the username
				var access_token = oauthHandler.AccessTokens.GetToken (context.Token);
				if (access_token.UserName != username) {
					// forbidden
					Logger.Debug ("username does not match the one in the access token, denying");
					response.ReturnAuthRequired ();
					return;
				} else {
					//context.Token.DecryptWithKey (atkn.MasterKeyHalf, user.MasterKeySalt);
					request.Items.Add ("AccessToken", context.Token);
					request.Items.Add ("Username", username);
				}
			} catch (Exception e) {
				Logger.DebugFormat ("failed to obtain authorization, oauth context is: {0}", context.Dump ());
				response.ReturnAuthRequired ();
			}

			Logger.DebugFormat ("authorization granted for user {0}", username);

			// possible race condition but locking is to expensive
			// at this point, rather accept non-precise values
			MainClass.ServedRequests++;
		}
	}
	
}