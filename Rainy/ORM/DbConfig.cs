using ServiceStack.Text;


namespace Rainy.Db.Config
{
	public interface IDbConfig
	{
		string ConnectionString { get; }	
	}

	public class SqliteConfig : IDbConfig
	{
		// full path to the sqlite file
		public string File;
		public bool InMemory = false;
	
		public string ConnectionString {
			get {
				return "Data source=" + File + ";busy_timeout=3000";
			}
		}
	}

	public class PostgreConfig : IDbConfig
	{
		public string Host;
		public uint Port;
		public string Username;
		public string Password;
		public string Database;

		public PostgreConfig () 
		{
			Host = "localhost";
			Port = 5432;
			Username = "postgres";
			Database = "rainy";
		}

		public string ConnectionString {
			get { 
				var connection_string = "Server={0};Port={1};User Id={2};Password={3};Database={4};".Fmt (
					Host,
					Port,
					Username,
					Password,
					Database
				);
				return connection_string;
			}
		}
	}
}