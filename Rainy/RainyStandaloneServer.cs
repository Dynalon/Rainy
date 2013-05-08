using System;
using ServiceStack.WebHost.Endpoints;
using ServiceStack.Common.Web;

using log4net;

using Rainy.OAuth;
using Rainy.WebService;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints.Extensions;
using ServiceStack.ServiceHost;
using System.Web;
using ServiceStack.Api.Swagger;
using System.IO;
using System.Net;

namespace Rainy
{
	public class RainyStandaloneServer : IDisposable
	{
		public readonly string ListenUrl;	

		public static OAuthHandlerBase OAuth;
		public static string Passkey;	

		public static Rainy.Interfaces.IDataBackend DataBackend { get; private set; }
		
		private AppHost appHost;
		private ILog logger;

		private bool testServer = false;

		public RainyStandaloneServer (Rainy.Interfaces.IDataBackend backend,
		                              string listen_url,
		                              bool test_server = false)
		{
			ListenUrl = listen_url;
			logger = LogManager.GetLogger (this.GetType ());
			testServer = test_server;

			OAuth = backend.OAuth;

			DataBackend = backend;

		}
		public void Start ()
		{
			appHost = new AppHost (testServer);
			appHost.Init ();

			logger.DebugFormat ("starting http listener at: {0}", ListenUrl);
			appHost.Start (ListenUrl);

		}
		public void Stop ()
		{
			appHost.Stop ();
			appHost.Dispose ();
		}
		public void Dispose ()
		{
			Stop ();
		}
	}

	public class AppHost : AppHostHttpListenerBase
	{
		bool testServer = false;

		public AppHost () : base("Rainy", typeof(GetNotesRequest).Assembly)
		{
		}
		public AppHost (bool test_server) : this ()
		{
			testServer = test_server;
		}
		public IHttpHandler CheckAndCreateStaticHttpHandler (IHttpRequest req)
		{
			var uri = new Uri (req.AbsoluteUri);
			if (uri.PathAndQuery.StartsWithIgnoreCase ("/srv/")) {
				return null;
			}
			return null;
		}
		public override void Configure (Funq.Container container)
		{
			JsConfig.DateHandler = JsonDateHandler.ISO8601;

			Plugins.Add(new SwaggerFeature ());

			// BUG HACK TODO
			// ServiceStack SetConfig somehow does not like beeing called twice 
			// which is fatal when running with unit tests, so don't call the 
			// SetConfig when running as a testserver
			if (testServer) return;

			ResponseFilters.Add((httpReq, httpRes, dto) =>
			                                       {
				using (var ms = new MemoryStream())
				{
					httpRes.ContentType = "application/json";

					EndpointHost.ContentTypeFilter.SerializeToStream(
						new SerializationContext(httpReq.ResponseContentType), dto, ms);
					
					var bytes = ms.ToArray();
					
					var listenerResponse = (HttpListenerResponse)httpRes.OriginalResponse;
					httpRes.ContentType = "application/json";
					listenerResponse.SendChunked = false;
					listenerResponse.ContentLength64 = bytes.Length;
					listenerResponse.OutputStream.Write(bytes, 0, bytes.Length);
					httpRes.EndServiceStackRequest();
				}
			});

			SetConfig (new EndpointHostConfig {
				// not all tomboy clients send the correct content-type
				// so we force application/json
				DefaultContentType = ContentType.Json,


				RawHttpHandlers = { 
					CheckAndCreateStaticHttpHandler,
				},

				// enable cors
				GlobalResponseHeaders = {
					{ "Access-Control-Allow-Origin", "*" },
					{ "Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS" },
					// time in seconds preflight responses can be cached by the client
					{ "Access-Control-Max-Age", "1728000" },
//					{ "Access-Control-Max-Age", "1" },

					// the Authority header must be whitelisted; it is sent be the rainy-ui
					// for authentication
					{ "Access-Control-Allow-Headers", "Content-Type, Authority" },
				},
			});
		}
	}
}