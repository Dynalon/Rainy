using System;
using ServiceStack.OrmLite;
using ServiceStack.ServiceHost;
using System.Runtime.Serialization;
using JsonConfig;

namespace Rainy.WebService
{
	// NOTE SHARING API
	//

	// note that this url is NOT the url that should be passed along, but rather the url that the service returns should.
	[Route("/api/config/",
	       Summary = "Retrieves configuration information about the server.")]
	public class ConfigServiceRequest: IReturn<ConfigServiceResponse>
	{
	}

	public class ConfigServiceResponse
	{
		public bool AllowSignup { get; set; }
		public bool RequireEmailVerification { get; set; }
		public bool RequireModeration { get; set; }
		public bool DevelopmentMode { get; set; }
		public string ServerVersion { get { return "0.1.2"; } }
		public string ServerName { get { return "Rainy"; } }
	}

	public class ConfigService : ServiceBase
	{
		public ConfigService (IDbConnectionFactory factory) : base (factory)
		{
		}

		public object Get (ConfigServiceRequest req)
		{
			var conf = new ConfigServiceResponse ();
			conf.AllowSignup = Config.Global.AllowSignup;
			conf.RequireEmailVerification = Config.Global.RequireEmailVerification;
			conf.RequireModeration = Config.Global.RequireModeration;
			conf.DevelopmentMode = Config.Global.Development;

			return conf;
		}
	}
}
