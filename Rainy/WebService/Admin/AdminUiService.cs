using ServiceStack.ServiceHost;
using System.Linq;
using System;
using System.IO;
using ServiceStack.Common.Web;
using System.Net;

namespace Rainy.WebService.Admin
{

	[Route("/static/{Filename}","GET")]
	public class ContentRequest: IReturn<string>
	{
		public string Filename { get; set; }
	}

	public class AdminUiService : RainyNoteServiceBase
	{
		public AdminUiService () : base ()
		{
		}
		public object Get (ContentRequest req)
		{
			//Response.AddHeader ("Cache-Control: max-age: 3600");
			switch (this.Request.RawUrl) {
			case "/admin":
			case "/login":

				// we need to append a / so that relative urls work
				return new HttpResult {
					StatusCode = HttpStatusCode.Redirect,
					Headers = {
						{ HttpHeaders.Location, this.Request.RawUrl + "/" }
					}
				};

			case "/admin/":
			case "/admin/index.html":
				return ReadInEmbeddedFile ("admin.html");

			case "/login/":
			case "/login/index.html":
			case "/login/login.html":
				return ReadInEmbeddedFile ("login.html");

			default:
				return ReadInEmbeddedFile (req.Filename);
			}
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