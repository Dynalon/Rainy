using System;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using ServiceStack.Text;
using DevDefined.OAuth.Provider;
using DevDefined.OAuth.Testing;
using DevDefined.OAuth.Provider.Inspectors;
using DevDefined.OAuth.Framework;
using System.Web;
using System.IO;
using System.Net;
using ServiceStack.ServiceModel.Serialization;
using Rainy.OAuth.SimpleStore;
using DevDefined.OAuth.Storage.Basic;
using DevDefined.OAuth.Storage;
using Rainy.WebService;

namespace Rainy.WebService.OAuth
{
	
	public class OAuthRequiredAttribute : Attribute, IHasRequestFilter
	{
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
			
			AppHost.OAuth.Provider.AccessProtectedResourceRequest (context);
			
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