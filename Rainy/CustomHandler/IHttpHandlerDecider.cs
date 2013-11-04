using System;
using System.Web;
using ServiceStack.ServiceHost;

namespace Rainy
{
	public interface IHttpHandlerDecider
	{
		IHttpHandler CheckAndProcess (IHttpRequest req);
	}
}

