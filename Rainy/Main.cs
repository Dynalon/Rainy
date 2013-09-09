using System;
using System.Threading;
using System.Collections.Generic;
using System.IO;
using log4net;

using JsonConfig;
using Mono.Options;
using Rainy.Db;
using System.Diagnostics;
using log4net.Appender;
using Rainy.Interfaces;
using Mono.Unix;
using Mono.Unix.Native;
using Rainy.Db.Config;
using ServiceStack.OrmLite;
using Rainy.OAuth;
using DevDefined.OAuth.Storage.Basic;
using DevDefined.OAuth.Storage;
using Rainy.Crypto;

namespace Rainy
{
	public class MainClass
	{
		// HACK a dictionary holding usernames and their repos
		// can be used for locking
		public static Dictionary<string, Semaphore> UserLocks;

		// some Status/Diagnostics
		public static DateTime Uptime;
		public static long ServedRequests;
		public static string DataPath;
		protected static ILog logger;


		protected static void SetupLogging (int loglevel)
		{
			// console appender
			log4net.Appender.ConsoleAppender appender;
			appender = new log4net.Appender.ConsoleAppender ();

			switch (loglevel) {
			case 0: appender.Threshold = log4net.Core.Level.Error; break;
			case 1: appender.Threshold = log4net.Core.Level.Warn; break;
			case 2: appender.Threshold = log4net.Core.Level.Info; break;
			case 3: appender.Threshold = log4net.Core.Level.Debug; break;
			case 4: appender.Threshold = log4net.Core.Level.All; break;
			}

			string pattern_layout;
			if (loglevel <= 1) {
				pattern_layout = "[%-5level] %message%newline";
			} else {
				pattern_layout = "%-4utcdate{yy/MM/dd_HH:mm:ss.fff} [%-5level] %logger->%M - %message%newline";
			}
			appender.Layout = new log4net.Layout.PatternLayout (pattern_layout);

			log4net.Config.BasicConfigurator.Configure (appender);
			logger = LogManager.GetLogger("Main");
			logger.Debug ("logsystem initialized");

			if (loglevel >= 3) {
				var appender2 = new log4net.Appender.FileAppender (appender.Layout, "./debug.log", true);
				log4net.Config.BasicConfigurator.Configure (appender2);
				logger.Debug ("Writing all log messages to file: debug.log");
			}

			/* ColoredConsoleAppender is win32 only. A managed version was introduced to log4net svn
			and should be available when log4net 1.2.12 comes out.
		
			Below codes is not tested/working!	
				
			log4net.Appender.ColoredConsoleAppender appender;
			appender = new log4net.Appender.ColoredConsoleAppender ();
			appender.Layout = new log4net.Layout.PatternLayout ("%date [%thread] %-5level %logger [%property{NDC}] - %message%newline");
			log4net.Config.XmlConfigurator.Configure(new System.IO.FileInfo("/Users/td/log4net.config"));
			colors.BackColor = log4net.Appender.ColoredConsoleAppender.Colors.HighIntensity;
			colors.ForeColor = log4net.Appender.ColoredConsoleAppender.Colors.Blue;
			colors.Level = log4net.Core.Level.Debug;
			appender.AddMapping(colors);	
			*/	
		}

		// This is the "Composition Root" in the IoC pattern that wires all
		// our objects/implementations up, based on a given configuration.
		// config sanity checks SHOULD NOT go here

		private static void ComposeObjectGraph(Funq.Container container)
		{
			var config = Config.Global;

			container.Register<SqliteConfig> (c => new SqliteConfig {
				File = Path.Combine (config.DataPath, "rainy.db")
			});

			container.Register<PostgreConfig> (c => {
				dynamic txt_conf = config.Postgre;
				var psql_conf = new PostgreConfig ();
				if (!string.IsNullOrEmpty (txt_conf.Username)) psql_conf.Username = txt_conf.Username;
				if (!string.IsNullOrEmpty (txt_conf.Password)) psql_conf.Password = txt_conf.Password;
				if (!string.IsNullOrEmpty (txt_conf.Database)) psql_conf.Database = txt_conf.Database;
				if (!string.IsNullOrEmpty (txt_conf.Host)) psql_conf.Host = txt_conf.Host;
				if (txt_conf.Port > 0) psql_conf.Port = (uint) txt_conf.Port;

				return psql_conf;
			});

			if (config.Backend == "xml") {

				// use username/password pairs from the config file
				container.Register<IAuthenticator> (c => {
					return new ConfigFileAuthenticator(config.User);
				});

				// we store notes in XML files in the DataPath
				container.Register<IDataBackend> (c => {
					var auth = c.Resolve<IAuthenticator> ();
					var factory = c.Resolve<IDbConnectionFactory> ();
					var oauth_handler = c.Resolve<OAuthHandler> ();
					return new FileSystemBackend (config.DataPath, factory, auth, oauth_handler, false);
				});

			} else {
				// database based backends
				switch ((string) config.Backend) {
					case "sqlite":
					container.Register<IDbConnectionFactory> (c => {
						var conf = container.Resolve<SqliteConfig> ();
						var connection_string = conf.ConnectionString;
						var factory = new OrmLiteConnectionFactory (connection_string, SqliteDialect.Provider);

						if (!File.Exists (conf.File)) {
							DatabaseBackend.CreateSchema (factory);
						}

						return (IDbConnectionFactory) factory;
					});
					break;
					case "postgre":
					container.Register<IDbConnectionFactory> (c => {
						var connection_string = container.Resolve<PostgreConfig> ().ConnectionString;
						var factory = new OrmLiteConnectionFactory (connection_string, PostgreSqlDialect.Provider);
						DatabaseBackend.CreateSchema (factory);
						return factory;
					});
					break;
				}
				if (Config.Global.Development == true) {
					// create a dummy user
					var fac = container.Resolve<IDbConnectionFactory> ();
					using (var db = fac.OpenDbConnection ()) {

						if (db.FirstOrDefault<DBUser> (u => u.Username == "dummy") == null) {

							var user = new DBUser ();
							user.Username = "dummy";
							user.CreateCryptoFields ("foobar123");
							user.FirstName = "John Dummy";
							user.LastName = "Doe";
							user.AdditionalData  = "Dummy user that is created when in development mode";
							user.IsActivated = true;
							user.IsVerified = true;
							user.EmailAddress = "dummy@doe.com";
							db.Insert<DBUser> (user);
						}
					}
				}
//
				container.Register<IAuthenticator> (c => {
					var factory = c.Resolve<IDbConnectionFactory> ();
//					var sfactory = new OrmLiteConnectionFactory ();
					var dbauth = new DbAuthenticator (factory);
					//var dbauth = new ConfigFileAuthenticator (Config.Global.Users);

					// we have to make sure users from the config file exist with the configured password
					// in the db
					// TODO delete old users? or restrict to webinterface?
					if (dbauth is ConfigFileAuthenticator) {
						foreach (dynamic user in Config.Global.Users) {
							string username = user.Username;
							string password = user.Password;
							using (var db = factory.OpenDbConnection ()) {
								var db_user = db.FirstOrDefault<DBUser> (u => u.Username == username);
								if (db_user != null) { 
									var need_update = db_user.UpdatePassword (password);
									if (need_update)
										db.UpdateOnly (new DBUser { PasswordHash = db_user.PasswordHash }, u => new { u.PasswordHash }, (DBUser p) => p.Username == username);
								} else {
									// create the user in the db
									var new_user = new DBUser ();
									new_user.Username = username;
									new_user.CreateCryptoFields (password);
									new_user.UpdatePassword (password); 
									db.Insert<DBUser> (new_user);
								}
							}
						}
					}
					return dbauth;
				});
//
				container.Register<IAdminAuthenticator> (c => {
					var auth = new ConfigFileAdminAuthenticator ();
					return auth;
				});

				container.Register<OAuthHandler> (c => {
					var auth = c.Resolve<IAuthenticator> ();
					var factory = c.Resolve<IDbConnectionFactory> ();
					//				ITokenRepository<AccessToken> access_tokens = new SimpleTokenRepository<AccessToken> ();
					//				ITokenRepository<RequestToken> request_tokens = new SimpleTokenRepository<RequestToken> ();
					ITokenRepository<AccessToken> access_tokens = new DbAccessTokenRepository<AccessToken> (factory);
					ITokenRepository<RequestToken> request_tokens = new DbRequestTokenRepository<RequestToken> (factory);
					ITokenStore token_store = new RainyTokenStore (access_tokens, request_tokens);
					OAuthHandler handler = new OAuthHandler (auth, access_tokens, request_tokens, token_store);
					return handler;
				});

				container.Register<IDbStorageFactory> (c => {
					var conn_factory = c.Resolve<IDbConnectionFactory> ();

					IDbStorageFactory storage_factory;
					storage_factory = new DbEncryptedStorageFactory (conn_factory, use_history: true);

					return (IDbStorageFactory) storage_factory;
				});

				container.Register<IDataBackend> (c => {
					var conn_factory = c.Resolve<IDbConnectionFactory> ();
					var storage_factory = c.Resolve<IDbStorageFactory> ();
					var handler = c.Resolve<OAuthHandler> ();
					var auth = c.Resolve<IAuthenticator> ();
					return new DatabaseBackend (conn_factory, storage_factory, auth, handler);
				});

/*				container.Register<OAuthHandler> (c => {
					var factory = c.Resolve<IDbConnectionFactory> ();
					var access_token_repo = new DbAccessTokenRepository<AccessToken> (factory);
					var request_token_repo = new SimpleTokenRepository<RequestToken> ();
					var auth = c.Resolve<IAuthenticator> ();
					var token_store = new Rainy.OAuth.SimpleStore.SimpleTokenStore (access_token_repo, request_token_repo);

					var handler = new OAuthHandler (auth, token_store);
					return handler;
				});
*/
			}

		}

		public static void Main (string[] args)
		{
			// parse command line arguments
			string config_file = "settings.conf";
			string cert_file = null, pvk_file = null;

			int loglevel = 0;
			bool show_help = false;
			bool open_browser = true;

			var p = new OptionSet () {
				{ "c|config=", "use config file",
					(string file) => config_file = file },
				{ "v", "increase log level, where -vvvv is highest",
					v => { if (v != null) ++loglevel; } },
				{ "h|help",  "show this message and exit", 
					v => show_help = v != null },
				{ "cert=",  "use this certificate for SSL", 
					(string file) => cert_file = file },
				{ "pvk=",  "use private key for certSSL", 
					(string file2) => pvk_file = file2 },

				{ "b|nobrowser",  "do not open browser window upon start",
					v => { if (v != null) open_browser = false; } },
			};
			p.Parse (args);

			if (show_help) {
				p.WriteOptionDescriptions (Console.Out);
				return;
			}

			if (!File.Exists (config_file)) {
				Console.WriteLine ("Could not find a configuration file (try the -c flag)!");
				return;
			}

			// set the configuration from the specified file
			Config.Global = Config.ApplyJsonFromPath (config_file);

			DataPath = Config.Global.DataPath;
			if (string.IsNullOrEmpty (DataPath)) {
				DataPath = Directory.GetCurrentDirectory ();
			} else {
				if (!Directory.Exists (DataPath))
					Directory.CreateDirectory (DataPath);
			}
			SetupLogging (loglevel);
			logger = LogManager.GetLogger ("Main");

			string listen_url = Config.Global.ListenUrl;
			if (string.IsNullOrEmpty (listen_url)) {
				listen_url = "https://localhost:443/";
				logger.InfoFormat ("no ListenUrl set in the settings.conf, using the default: {0}",
				                   listen_url);
			}
			// servicestack expects trailing slash, else error is thrown
			if (!listen_url.EndsWith ("/")) listen_url += "/";

			ConfigureSslCerts (listen_url, cert_file, pvk_file);

			// by default we use the filesystem backend
			if (string.IsNullOrEmpty (Config.Global.Backend)) {
				Config.Global.Backend = "filesystem";
			}

			if (Config.Global.Backend != "filesystem" && string.IsNullOrEmpty (Config.Global.AdminPassword)) {
				if (Config.Global.Development == true)
					Config.Global.AdminPassword = "foobar";
				else {
					logger.Fatal ("An administrator password must be set");
					Environment.Exit (-1);
				}
			}

			open_browser = open_browser && !string.IsNullOrEmpty (Environment.GetEnvironmentVariable ("DISPLAY"));
			open_browser = open_browser && !string.IsNullOrEmpty (Config.Global.AdminPassword);
			open_browser = open_browser && Config.Global.Backend != "filesystem";
			string admin_ui_url = listen_url.Replace ("*", "localhost");
			admin_ui_url += "admin/";

			ComposeObjectGraphDelegate object_graph_composer = ComposeObjectGraph;

			using (var listener = new RainyStandaloneServer (Config.Global.ListenUrl, object_graph_composer)) {

				listener.Start ();
				Uptime = DateTime.UtcNow;

				if (open_browser) {
					Process.Start (admin_ui_url);
				}

				if (Environment.OSVersion.Platform != PlatformID.Unix &&
				    Environment.OSVersion.Platform != PlatformID.MacOSX) {
					// we run on windows, can't wait for unix signals
					Console.WriteLine ("Press return to stop Rainy");
					Console.ReadLine ();
					Environment.Exit(0);
				} else {
					// we run UNIX
					UnixSignal [] signals = new UnixSignal[] {
						new UnixSignal(Signum.SIGINT),
						new UnixSignal(Signum.SIGTERM),
					};

					// Wait for a unix signal
					for (bool exit = false; !exit; )
					{
						int id = UnixSignal.WaitAny(signals);

						if (id >= 0 && id < signals.Length)
						{
							if (signals[id].IsSet) exit = true;
							logger.Debug ("received signal, exiting");
						}
					}
				}
			}
		}


		public static void ConfigureSslCerts (string listen_url, string cert_file, string pvk_file)
		{
			bool ssl_enabled = listen_url.ToLower ().StartsWith ("https") ? true : false;
			listen_url = listen_url.Replace ("*", "localhost");
			if (!ssl_enabled)
				return;

			// cert management requires mono
			if (Type.GetType ("Mono.Runtime") == null) {
				logger.Info ("SSL certification handling is only supported when using the" +
				             "mono runtime. On Windows, use the httpcfg.exe tool to setup SSL");
				return;
			}

			string default_ssl_cert_path = Path.Combine (DataPath, "ssl-cert.cer");
			string default_ssl_privkey_path = Path.Combine (DataPath, "ssl-cert.pvk");

			if (string.IsNullOrEmpty (cert_file)) {

				// try to load a cert from the default location
				if (!File.Exists (default_ssl_cert_path)) {
					logger.Info ("No default SSL certificate found but SSL was enabled");

					if (listen_url.Contains ("*")) {
						logger.Fatal ("SSL certificate generation for wildcard * urls is not possible");
						logger.Fatal ("Please generate certificates manually or remove wildcard from ListenUrl");
						Environment.Exit (-1);
					}

					// create a ssl cert using the makecert tool
					var listen_domain = new Uri (listen_url).DnsSafeHost;
					logger.InfoFormat ("Creating a default self-signed certificate" +
						" for domain {0} as {1}", listen_domain, default_ssl_cert_path);

					var makecert_args = new string[] {
						"-n", "CN=" + listen_domain,
						"-sv", default_ssl_privkey_path,
						default_ssl_cert_path
					};
					Mono.Tools.MakeCert.MakeCertMain (makecert_args);

					if (!File.Exists (default_ssl_cert_path) || !File.Exists (default_ssl_privkey_path)) {
						logger.Fatal ("SSL cert generation failed");
						Environment.Exit (-1);
					}
				}
				// we got defaults, use them
				cert_file = default_ssl_cert_path;
				pvk_file = default_ssl_privkey_path;
			}

			// cmdline specified certs
			if (!File.Exists (cert_file)) {
					Console.WriteLine ("Certificate file {0} does not exist!", cert_file);
					Environment.Exit(-1);
			}
			if (!File.Exists (pvk_file)) {
				Console.WriteLine ("Private key file {0} does not exist!", pvk_file);
				Environment.Exit(-1);
			}

			logger.DebugFormat ("using SSL cert {0} with private key file {1}", cert_file, pvk_file);

			// HttpListener can not be setup for SSL via API (neither in MS.NET nor
			// mono). Mono requires for every port a .cer/.pvk pair to be placed
			// 	$HOME/.config/mono/httplistener/<port>.cer
			// 	$HOME/.config/mono/httplistener/<port>.pvk
			//
			// and then we can start listening to https://*:<port>
			// We therefore copy the cert/pvk there every time, overwriting
			// any previous setups.
			string dirname = Environment.GetFolderPath (Environment.SpecialFolder.ApplicationData);

			string path = Path.Combine (dirname, ".mono");
			if (!Directory.Exists (path)) Directory.CreateDirectory (path);

			path = Path.Combine (path, "httplistener");
			if (!Directory.Exists (path)) Directory.CreateDirectory (path);

			string port = new Uri(listen_url).Port.ToString ();
			string cert_dst = Path.Combine (path, String.Format ("{0}.cer", port));
			string pvk_dst = Path.Combine (path, String.Format ("{0}.pvk", port));

			File.Copy (cert_file, cert_dst, true);
			File.Copy (pvk_file, pvk_dst, true);
		}
	}
}