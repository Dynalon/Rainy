using System;
using ServiceStack.WebHost.Endpoints;
using ServiceStack.Common.Web;

using log4net;

using Rainy.OAuth;
using Rainy.WebService;

namespace Rainy
{

	
	public class RainyStandaloneServer : IDisposable
	{

		public int Port { get; set; }
		public string Hostname { get; set; }

		public static OAuthHandler OAuth;
		public static string Passkey;	

		public static IDataBackend DataBackend { get; private set; }
		
		private AppHost appHost;
		private ILog logger;

		public RainyStandaloneServer (OAuthHandler handler, IDataBackend backend)
		{
			Port = 8080;
			Hostname = "127.0.0.1";
			logger = LogManager.GetLogger (this.GetType ());

			OAuth = handler;
			DataBackend = backend;

		}
		public void Start ()
		{
			string listen_url = "http://" + Hostname + ":" + Port + "/";

			appHost = new AppHost ();
			appHost.Init ();

			logger.DebugFormat ("starting http listener at: {0}", listen_url);
			appHost.Start (listen_url);

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
			// not all tomboy clients send the correct content-type
			// so we force application/json
			SetConfig (new EndpointHostConfig {
				DefaultContentType = ContentType.Json 
			});
		}
	}
}