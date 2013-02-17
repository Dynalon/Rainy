using System;
using ServiceStack.ServiceHost;
using System.Linq;
using System.IO;

namespace Rainy.WebService.Admin
{
	//[AdminPasswordRequired]
	[Route("/api/admin/log","GET")]
	public class GetLogRequest : IReturn<string>
	{
	}

	public class AdminLogService : RainyServiceBase
	{
		public AdminLogService () : base ()
		{

		}
		public string Get (GetLogRequest req)
		{
			// open the debug.log file
			var file_path = "./debug.log";
			string output = "";

			if (!File.Exists (file_path))
				return "";

			// TODO: fucking log4net locks exclusive, can't even read a file we are only
			// appending to.
			var fs = new FileStream (file_path,FileMode.Open, FileAccess.Read);
			var reader = new StreamReader (fs);
			var content = reader.ReadToEnd ();
			// only give back last 500 lines
			var logs = content.Skip (content.Length - 500).ToList ();
			logs.ForEach (s => output += s + "\n");

			return output;
		}
	}
}