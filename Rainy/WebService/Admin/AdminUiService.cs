using ServiceStack.ServiceHost;

namespace Rainy.WebService.Admin
{
	[Route("/adminui/","GET")]
	public class AdminUiRequest : IReturn<string>
	{
	}
	public class AdminUiService : RainyServiceBase
	{
		public AdminUiService () : base ()
		{
		}
		public string Get (AdminUiRequest req)
		{
			return "<html><h1>Hello world!</h1></html>";
		}
	}
	
}