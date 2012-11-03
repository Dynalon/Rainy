using System;
using ServiceStack.ServiceHost;
using ServiceStack.Text;
using DevDefined.OAuth.Framework;
using System.IO;
using System.Net;
using DevDefined.OAuth.Storage.Basic;
using Rainy.WebService;
using log4net;

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
			string Username = "";
			if (requestDto is UserRequest) {
				Username = ((UserRequest)requestDto).Username;
			} else if (requestDto is NotesRequest) {
				Username = ((NotesRequest)requestDto).Username;
			} else {
				response.ReturnAuthRequired ();
				return;
			}
			
			var web_request = ((HttpListenerRequest)request.OriginalRequest).ToWebRequest ();
			IOAuthContext context = new OAuthContextBuilder ().FromWebRequest (web_request, new MemoryStream ());
		
			try {
				Logger.Debug ("trying to acquire authorization");
				AppHost.OAuth.Provider.AccessProtectedResourceRequest (context);
			} catch {
				Logger.DebugFormat ("failed to obtain authorization, oauth context is: {0}", context.Dump ());
				response.ReturnAuthRequired ();
			}
			
			// check if the access token matches the username
			var access_token = AppHost.OAuth.AccessTokens.GetToken (context.Token);
			if (access_token.UserName != Username) {
				// forbidden
				response.ReturnAuthRequired ();
				return;
			}
		}
	}
	
}