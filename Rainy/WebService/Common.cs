using System;
using System.Collections.Generic;
using System.Linq;
using Funq;
using ServiceStack.Common;
using ServiceStack.ServiceClient.Web;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using ServiceStack.WebHost.Endpoints;
using System.Threading;
using Tomboy;
using System.Runtime.Serialization;
using ServiceStack.Common.Web;
using ServiceStack.Text;
using System.IO;
using ServiceStack.ServiceModel.Serialization;
using Rainy.OAuth;
using DevDefined.OAuth.Framework;
using System.Net;
using log4net;
using Rainy.WebService.OAuth;

namespace Rainy.WebService
{

	[RequestLogFilter]
	[ResponseLogFilter]
	public abstract class RainyServiceBase : Service
	{
		protected ILog logger;
		public RainyServiceBase ()
		{
			logger = LogManager.GetLogger (GetType ());
		}
	}
	
}
