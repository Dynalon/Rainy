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

namespace Rainy
{
	public class RainyStandaloneServer : IDisposable
	{
		public readonly string ListenUrl;	

		public static OAuthHandlerBase OAuth;

		public static Rainy.Interfaces.IDataBackend DataBackend { get; private set; }
		
		private AppHost appHost;
		private ILog logger;

		public RainyStandaloneServer (string listen_url)
		{
			appHost = new AppHost ();

			ListenUrl = listen_url;
			logger = LogManager.GetLogger (this.GetType ());

			DataBackend = Container.Instance.Resolve<IDataBackend> ();
			OAuth = DataBackend.OAuth;
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

	public class AppHost : AppHostHttpListenerBase
	{
		public AppHost () : base("Rainy", typeof(GetNotesRequest).Assembly)
		{
		}
		public AppHost (bool test_server) : this ()
		{
		}

		public override void Configure (Funq.Container container)
		{
			var factory = Rainy.Container.Instance.Resolve<IDbConnectionFactory> ();
			using (var db = factory.OpenDbConnection ()) {
				db.DropAndCreateTable<DBNote> ();
				db.InsertParam<DBNote> (new DBNote { MetadataChangeDate = DateTime.Now.ToString () });
			}

			JsConfig.DateHandler = JsonDateHandler.ISO8601;

			Plugins.Add (new SwaggerFeature ());

			// register our custom exception handling
			this.ExceptionHandler = Rainy.ErrorHandling.ExceptionHandler.CustomExceptionHandler;
			this.ServiceExceptionHandler = Rainy.ErrorHandling.ExceptionHandler.CustomServiceExceptionHandler;


			var swagger_path = Path.Combine(Path.GetDirectoryName(this.GetType ().Assembly.Location), "../../swagger-ui/");
			var swagger_handler = new FilesystemHandler ("/swagger-ui/", swagger_path);

			var embedded_handler = new EmbeddedResourceHandler ("/srv/", this.GetType ().Assembly, "Rainy.WebService.Admin.UI");

			// BUG HACK
			// GlobalResponseHeaders are not cleared between creating instances of a new config
			// this will be fatal (duplicate key error) for unit tests so we clear the headers
			EndpointHostConfig.Instance.GlobalResponseHeaders.Clear ();

			SetConfig (new EndpointHostConfig {
				// not all tomboy clients send the correct content-type
				// so we force application/json
				DefaultContentType = ContentType.Json,

				RawHttpHandlers = { 
					swagger_handler.CheckAndProcess,
					embedded_handler.CheckAndProcess
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