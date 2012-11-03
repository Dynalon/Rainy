using ServiceStack.ServiceInterface;
using log4net;

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
