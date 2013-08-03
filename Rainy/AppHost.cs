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
using ServiceStack.WebHost.Endpoints.Extensions;

namespace Rainy
{
	public delegate void ComposeObjectGraphDelegate (Funq.Container container);

	public class AppHost : AppHostHttpListenerBase
	{
		private ComposeObjectGraphDelegate ComposeObjectGraph;
		public AppHost (ComposeObjectGraphDelegate graph_composer) : base("Rainy", typeof(GetNotesRequest).Assembly)
		{
			this.ComposeObjectGraph = graph_composer;
		}

		public override void Configure (Funq.Container container)
		{
			this.ComposeObjectGraph (container);

			JsConfig.DateHandler = JsonDateHandler.ISO8601;

			Plugins.Add (new SwaggerFeature ());

			// register our custom exception handling
			this.ExceptionHandler = Rainy.ErrorHandling.ExceptionHandler.CustomExceptionHandler;
			this.ServiceExceptionHandler = Rainy.ErrorHandling.ExceptionHandler.CustomServiceExceptionHandler;

			var swagger_path = Path.Combine(Path.GetDirectoryName(this.GetType ().Assembly.Location), "../../swagger-ui/");
			var swagger_handler = new FilesystemHandler ("/swagger-ui/", swagger_path);

			var adminui_path = Path.Combine(Path.GetDirectoryName(this.GetType ().Assembly.Location), "../../../Rainy.UI/dist/");
			var adminui_handler = new FilesystemHandler ("/ui/", adminui_path);
			//var embedded_handler = new EmbeddedResourceHandler ("/adminui/", this.GetType ().Assembly, "Rainy.WebService.Admin.UI");

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
					adminui_handler.CheckAndProcess
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