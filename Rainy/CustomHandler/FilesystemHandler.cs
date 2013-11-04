//
// System.Web.StaticFileHandler
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//  Timo Dörr (timo@latecrew.de)
//
// (C) 2002 Ximian, Inc (http://www.ximian.com)
// (C) 2013 Timo Dörr

//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.IO;
using System.Web;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints.Extensions;
using HttpRequestWrapper = ServiceStack.WebHost.Endpoints.Extensions.HttpRequestWrapper;
using HttpResponseWrapper = ServiceStack.WebHost.Endpoints.Extensions.HttpResponseWrapper;
using ServiceStack.WebHost.Endpoints.Support;
using ServiceStack.WebHost.Endpoints;
using System.Linq;
using ServiceStack;

namespace Rainy.CustomHandler
{
	class FilesystemHandler : IHttpHandler, IServiceStackHttpHandler, IHttpHandlerDecider
	{
		public void ProcessRequest(HttpContext context)
		{
			ProcessRequest(
			new HttpRequestWrapper(null, context.Request),
			new HttpResponseWrapper(context.Response), 
			null);
		}
	
		public string HtdocsPath { get; private set; }
		public string RoutePath { get; private set; }
		public string DefaultFilename { get; set; }
		private bool active = true;

		public FilesystemHandler(string route_path, string htdocs_path)
		{
			if (!Directory.Exists(htdocs_path)) {
				// TODO make case insensitive
				//throw new ArgumentException(htdocs_path);
				active = false;
			}
			HtdocsPath = htdocs_path;

			if (!route_path.StartsWith("/"))
				route_path = "/" + route_path;
			if (!route_path.EndsWith("/"))
				route_path = route_path + "/";

			RoutePath = route_path;

			DefaultFilename = "index.html";
		}
		public IHttpHandler CheckAndProcess (IHttpRequest request)
		{
			if (!active)
				return null;

			string abs_path = request.GetAbsolutePath();

			if (this.HtdocsPath.Contains ("swagger-ui")) {

				if (abs_path.StartsWith ("/swagger-ui/")) {
					return this;
				}
			}
			if (abs_path.StartsWith ("/resource")) {
				return null;
			}
			
			if (abs_path.StartsWith ("/oauth/") || abs_path.StartsWith ("/api/")) {
				return null;
			} else if (abs_path.StartsWith ("/admin/")) {
				return this;
			} else if (abs_path.Count (c => c == '/') == 1) {
				return this;
			} else {
				return null;
			}

			/*
			// TODO case insensitive on option
			if (abs_path.StartsWith(RoutePath) && is_html_request)
				// we want to handle this url path
				return this;
			else
				return null;
			*/
		}

        public void ProcessRequest(IHttpRequest request, IHttpResponse response, string operationName)
		{
            response.EndHttpHandlerRequest(skipClose: true, afterBody: r => {

				// i.e. /static/folder/file.html => folder/file.html
				var abs_path = request.GetAbsolutePath();
				string requested_relative_path = "";
				if (abs_path == "/")
					requested_relative_path = "./";
				else
					requested_relative_path = abs_path.Substring(RoutePath.Length);

				var fileName = Path.Combine (HtdocsPath, requested_relative_path);
                var fi = new FileInfo(fileName);
                if (!fi.Exists)
                {
					// append default filename if feasible (i.e. index.html)
                    if ((fi.Attributes & FileAttributes.Directory) != 0)
                    {
                        foreach (var defaultDoc in EndpointHost.Config.DefaultDocuments)
                        {
                            var defaultFileName = Path.Combine(fi.FullName, defaultDoc);
                            if (!File.Exists(defaultFileName)) continue;
							fi = new FileInfo (defaultFileName);
							fileName = defaultFileName;
                        }
                    }
					if (!fi.Exists) {
						var msg = "Static File '" + request.PathInfo + "' not found.";
						throw new HttpException(404, msg);
					}
                }

                r.ContentType = MimeTypes.GetMimeType(fileName);
                TimeSpan maxAge;
                if (r.ContentType != null && EndpointHost.Config.AddMaxAgeForStaticMimeTypes.TryGetValue(r.ContentType, out maxAge))
                {
                    r.AddHeader(HttpHeaders.CacheControl, "max-age=" + maxAge.TotalSeconds);
                }

                if (request.HasNotModifiedSince(fi.LastWriteTime))
                {
                    r.ContentType = MimeTypes.GetMimeType(fileName);
                    r.StatusCode = 304;
                    return;
                }

                try
                {
                    r.AddHeaderLastModified(fi.LastWriteTime);
                    r.ContentType = MimeTypes.GetMimeType(fileName);

                    if (!Env.IsMono)
                    {
                        r.TransmitFile(fileName);
                    }
                    else
                    {
                        r.WriteFile(fileName);
                    }
                }
                catch (Exception ex)
                {
                    //log.ErrorFormat("Static file {0} forbidden: {1}", request.PathInfo, ex.Message);
                    throw new HttpException(403, "Forbidden.");
                }
            });
		}

	    public bool IsReusable
		{
			get { return true; }
		}
	}
}