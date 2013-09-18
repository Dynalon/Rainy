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
using System.Reflection;
using System.Linq;
using ServiceStack;

namespace Rainy.CustomHandler
{
	/// <summary>
	/// Serves static files that are embedded as a resource into an assembly.
	/// </summary>
	class EmbeddedResourceHandler : IHttpHandler, IServiceStackHttpHandler, IHttpHandlerDecider
	{
		public void ProcessRequest(HttpContext context)
		{
			ProcessRequest(
				new HttpRequestWrapper(null, context.Request),
				new HttpResponseWrapper(context.Response), 
				null);
		}
		
		public Assembly ResourceAssembly { get; private set; }
		public string RoutePath { get; private set; }
		public string ResourcePath { get; private set; }
		private DateTime lastModified;

		/// <summary>
		/// Initializes a new instance of the <see cref="Rainy.CustomHandler.EmbeddedResourceHandler"/> class.
		/// </summary>
		/// <param name="route_path">The virtual path the client requested url is matched against</param>
		/// <param name="resource_assembly">The assembly which holds the embedded resource</param>
		/// <param name="resource_path">Path to the embedded resource within the assembly (using dots as seperator)</param>
		public EmbeddedResourceHandler (string route_path, Assembly resource_assembly, string resource_path)
		{
			ResourceAssembly = resource_assembly;
			var asm_path = ResourceAssembly.Location;
			var fi = new FileInfo (asm_path);
			lastModified = fi.LastWriteTimeUtc;

			ResourcePath = resource_path;
				
			if (!route_path.StartsWith("/"))
				route_path = "/" + route_path;
			if (!route_path.EndsWith("/"))
				route_path = route_path + "/";
			
			RoutePath = route_path;
			
		}
		public IHttpHandler CheckAndProcess (IHttpRequest request)
		{
			var abs_path = request.GetAbsolutePath();
		
			if (abs_path.StartsWith ("/oauth/") || abs_path.StartsWith ("/api/")) {
				return null;
			} else if (abs_path.StartsWith ("/admin/") || abs_path.StartsWith ("swagger-ui")) {
				return this;
			} else if (abs_path.Count (c => c == '/') == 1) {
				return this;
			} else {
				return null;
			}
		}
		
		public void ProcessRequest(IHttpRequest request, IHttpResponse response, string operationName)
		{
			response.EndHttpHandlerRequest (skipClose: true, afterBody: r => {
				
				// i.e. /static/folder/file.html => folder/file.html
				var requested_relative_path = request.GetAbsolutePath().Substring(RoutePath.Length);
			
				if (requested_relative_path.Count (c => c == '.') == 0) {
					// append index.html
					if (requested_relative_path.EndsWith ("/")) {
						requested_relative_path += "index.html";
					} else 
						requested_relative_path += "/index.html";
				}
				var file_content = ReadInEmbeddedResource (requested_relative_path);

				if (file_content == null)
					throw new HttpException(404, "Not found");

				r.ContentType = MimeTypes.GetMimeType(requested_relative_path);

				TimeSpan maxAge;
				if (r.ContentType != null && EndpointHost.Config.AddMaxAgeForStaticMimeTypes.TryGetValue(r.ContentType, out maxAge))
				{
					r.AddHeader(HttpHeaders.CacheControl, "max-age=" + maxAge.TotalSeconds);
				}
				
				if (request.HasNotModifiedSince(lastModified))
				{
					r.StatusCode = 304;
					return;
				}
				
				try
				{
					r.AddHeaderLastModified(lastModified);
					r.ContentType = MimeTypes.GetMimeType(requested_relative_path);
					
					r.Write(file_content);
				}
				catch (Exception ex)
				{
					//log.ErrorFormat("Static file {0} forbidden: {1}", request.PathInfo, ex.Message);
					throw new HttpException(400, "Server error.");
				}
			});
		}
		
		public bool IsReusable
		{
			get { return true; }
		}
		protected string ReadInEmbeddedResource (string filename) {
			
			string[] res = ResourceAssembly.GetManifestResourceNames ();
			filename = filename.Replace("/",".");

			var res_filename = ResourcePath + "." + filename;

			res_filename = res_filename.Replace ("..", ".");

			var file = res.Where (r => res_filename == r).FirstOrDefault ();

			if (file == null) return null;	

			var stream = ResourceAssembly.GetManifestResourceStream (file);
			string file_content = new StreamReader(stream).ReadToEnd ();
			
			return file_content;
		}
	}
}