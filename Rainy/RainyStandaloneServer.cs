using System;
using log4net;

namespace Rainy
{
	public class RainyStandaloneServer : IDisposable
	{
		public readonly string ListenUrl;	

		private AppHost appHost;
		private ILog logger;

		public RainyStandaloneServer (string listen_url, ComposeObjectGraphDelegate composer)
		{
			logger = LogManager.GetLogger (this.GetType ());

			this.appHost = new AppHost(composer);

			ListenUrl = listen_url;
		}
		public void Start ()
		{
			appHost.Init ();

			logger.DebugFormat ("starting http listener at: {0}", ListenUrl);
			appHost.Start (ListenUrl);

		}
		public void Stop ()
		{
			appHost.Stop ();
		}
		public void Dispose ()
		{
			Stop ();
			appHost.Dispose ();
		}
	}

}