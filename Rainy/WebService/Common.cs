using ServiceStack.ServiceInterface;
using log4net;
using System;
using ServiceStack.ServiceHost;
using Rainy.WebService.OAuth;
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
			// bug: .Dump () on a NullStream will throw an exception
			if (requestDto is OAuthRequestTokenRequest)
				return;

			Logger.DebugFormat ("Received request at: {0}\nJSON Data received:\n{1}", req.RawUrl, requestDto.Dump ());
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
			Logger.Debug ("Sending response JSON:\n" + responseDto.Dump ());
		}
		public IHasResponseFilter Copy ()
		{
			return this;
		}
		public int Priority {
			get { return 1; }
		}
	}
	[RequestLogFilterAttribute]
	[ResponseLogFilterAttribute]
	public abstract class RainyServiceBase : Service
	{
		protected ILog Logger;
		public RainyServiceBase ()
		{
			Logger = LogManager.GetLogger (GetType ());
		}
	}
}
