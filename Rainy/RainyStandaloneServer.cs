using System;
using ServiceStack.WebHost.Endpoints;
using ServiceStack.Common.Web;

using log4net;

using Rainy.OAuth;
using Rainy.WebService;
using ServiceStack.Text;

namespace Rainy
{
	public class RainyStandaloneServer : IDisposable
	{
		public readonly string ListenUrl;

		public static OAuthHandlerBase OAuth;
		public static string Passkey;	

		public static IDataBackend DataBackend { get; private set; }
		
		private AppHost appHost;
		private ILog logger;

		public RainyStandaloneServer (IDataBackend backend, string listen_url)
		{
			ListenUrl = listen_url;
			logger = LogManager.GetLogger (this.GetType ());

			OAuth = backend.OAuth;

			DataBackend = backend;

		}
		public void Start ()
		{
			appHost = new AppHost ();
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

		public AppHost () : base("Rainy", typeof(GetNotesRequest).Assembly)
		{
		}
		public override void Configure (Funq.Container container)
		{
			JsConfig.DateHandler = JsonDateHandler.ISO8601;

			// not all tomboy clients send the correct content-type
			// so we force application/json
			SetConfig (new EndpointHostConfig {
				DefaultContentType = ContentType.Json,
				GlobalResponseHeaders = {
					{ "Access-Control-Allow-Origin", "*" },
					{ "Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS" },
					{ "Access-Control-Allow-Headers", "Content-Type, Authority" },
				},
			});
		}
	}
}