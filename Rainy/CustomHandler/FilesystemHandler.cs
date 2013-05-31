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
using System.Collections.Generic;
using System.IO;
using System.Web;
using ServiceStack.Common;
using ServiceStack.Common.Web;
using ServiceStack.Logging;
using ServiceStack.ServiceHost;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints.Extensions;
using HttpRequestWrapper = ServiceStack.WebHost.Endpoints.Extensions.HttpRequestWrapper;
using HttpResponseWrapper = ServiceStack.WebHost.Endpoints.Extensions.HttpResponseWrapper;
using ServiceStack.WebHost.Endpoints.Support;
using ServiceStack.WebHost.Endpoints;

namespace Rainy.CustomHandler
{
	class FilesystemHandler : IHttpHandler, IServiceStackHttpHandler
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

			var abs_path = request.GetAbsolutePath();

			// TODO case insensitive on option
			if (abs_path.StartsWith(RoutePath))
				// we want to handle this url path
				return this;
			else
				return null;
		}

        public void ProcessRequest(IHttpRequest request, IHttpResponse response, string operationName)
		{
            response.EndHttpRequest(skipClose: true, afterBody: r => {

				// i.e. /static/folder/file.html => folder/file.html
				var requested_relative_path = request.GetAbsolutePath().Substring(RoutePath.Length);

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
                            r.Redirect(request.GetPathUrl() + '/' + defaultDoc);
                            return;
                        }
                    }
					var msg = "Static File '" + request.PathInfo + "' not found.";
                    throw new HttpException(404, msg);
                }

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