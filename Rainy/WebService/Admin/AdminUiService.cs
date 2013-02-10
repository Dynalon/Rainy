using ServiceStack.ServiceHost;
using System.Linq;
using System;
using System.IO;
using ServiceStack.Common.Web;
using System.Net;

namespace Rainy.WebService.Admin
{
	[Route("/admin","GET")]
	public class AdminUiRequest : IReturn<string>
	{
	}

	[Route("/admin/{Filename}","GET")]
	public class ContentRequest: IReturn<string>
	{
		public string Filename { get; set; }
	}

	public class AdminUiService : RainyServiceBase
	{
		public AdminUiService () : base ()
		{

		}
		public HttpResult Get (AdminUiRequest req)
		{
			return new HttpResult {
				StatusCode = HttpStatusCode.Redirect,
				Headers = {
					{ HttpHeaders.Location, "/admin/index.html" }
				}
			};
		}
		public string Get (ContentRequest req)
		{
			//Response.AddHeader ("Cache-Control: max-age: 3600");
			return ReadInEmbeddedFile(req.Filename);
		}
		protected string ReadInEmbeddedFile (string filename) {
			var assembly = typeof(AdminUiService).Assembly;

			string[] res = assembly.GetManifestResourceNames ();

			var file = res.Where (r => r.EndsWith(filename))
				.FirstOrDefault ();

			var stream = assembly.GetManifestResourceStream (file);
			string file_content = new StreamReader(stream).ReadToEnd ();

			return file_content;
		}
	}
}