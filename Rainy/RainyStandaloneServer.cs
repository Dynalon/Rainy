using System;
using System.IO;
using ServiceStack.Api.Swagger;
using ServiceStack.Common.Web;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints;
using log4net;
using Rainy.CustomHandler;
using Rainy.Interfaces;
using Rainy.OAuth;
using Rainy.WebService;
using ServiceStack.OrmLite.PostgreSQL;
using ServiceStack.OrmLite;
using Rainy.Db;
using JsonConfig;
using Rainy.Db.Config;

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