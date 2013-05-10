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

		public RainyStandaloneServer (Rainy.Interfaces.IDataBackend backend, string listen_url, bool test_server = false)
		{
			logger = LogManager.GetLogger (this.GetType ());

			// servicestack expects trailing slash, else error is thrown
			if (!listen_url.EndsWith ("/")) listen_url += "/";

			// TODOD BUG WORKAROUND
			// if the ListenUrl has not an explicit port specified and using https
			// the HttpListener will not bind to port 443 - this might be a mono bug
			// in HttpListener
			if (listen_url == "http://*/") {
				ListenUrl = "http://*:80/";
			} else if (listen_url == "https://*/") {
				ListenUrl = "https://*:443/";
			} else {
				var uri = new Uri(listen_url);
				if (uri.Scheme.ToLower () == "https" && !uri.Authority.Contains (":")) {
					ListenUrl = "https://" + uri.Authority + ":443" + uri.PathAndQuery;
				} else if (uri.Scheme.ToLower () == "http" && !uri.Authority.Contains (":")) {
					ListenUrl = "http://" + uri.Authority + ":80" + uri.PathAndQuery;
				} else {
					ListenUrl = listen_url;
				}
			} // END WORKAROUND

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
		public override void Configure (Funq.Container container)
		{
			JsConfig.DateHandler = JsonDateHandler.ISO8601;

			// BUG HACK TODO
			// ServiceStack SetConfig somehow does not like beeing called twice 
			// which is fatal when running with unit tests, so don't call the 
			// SetConfig when running as a testserver
			if (testServer) return;

			SetConfig (new EndpointHostConfig {
				// not all tomboy clients send the correct content-type
				// so we force application/json
				DefaultContentType = ContentType.Json,

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