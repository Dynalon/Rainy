using ServiceStack.ServiceInterface;
using log4net;
using System;
using ServiceStack.ServiceHost;
using Rainy.WebService.OAuth;
using System.Linq;
using ServiceStack.Text;

namespace Rainy.WebService
{
	public class RequestLogFilterAttribute : Attribute, IHasRequestFilter
	{
		protected ILog Logger;
		public RequestLogFilterAttribute ()
		{
			Logger = LogManager.GetLogger (GetType ());
		}
		public void RequestFilter (IHttpRequest req, IHttpResponse res, object requestDto)
		{
			if (requestDto is OAuthRequestTokenRequest) {
				return;
			}
			Logger.DebugFormat ("Received request at: {0}\nDeserialized data (JSV):\n{1}", req.RawUrl, requestDto.Dump ());

			Logger.Debug ("Received request headers:\n");
			foreach(var key in req.Headers.AllKeys) {
				Logger.DebugFormat ("\t {0}: {1}", key, req.Headers[key]);
			}

		}
		public IHasRequestFilter Copy ()
		{
			return this;
		}
		public int Priority {
			get { return 1; }
		}
	}

	public class ResponseLogFilterAttribute : Attribute, IHasResponseFilter
	{
		protected ILog Logger;
		public ResponseLogFilterAttribute ()
		{
			Logger = LogManager.GetLogger (GetType ());
		}
		public void ResponseFilter (IHttpRequest req, IHttpResponse res, object responseDto)
		{
			Logger.Debug ("Unserialized response data to send (JSV):\n" + responseDto.Dump ());
		}
		public IHasResponseFilter Copy ()
		{
			return this;
		}
		public int Priority {
			get { return 1; }
		}
	}

	[RequestLogFilter]
	[ResponseLogFilter]
	public abstract class RainyServiceBase : Service
	{
		protected Rainy.Interfaces.INoteRepository GetNotes (string username)
		{
			return RainyStandaloneServer.DataBackend.GetNoteRepository (username);
		}
		protected ILog Logger;
		public RainyServiceBase ()
		{
			Logger = LogManager.GetLogger (GetType ());
		}
	}
}
