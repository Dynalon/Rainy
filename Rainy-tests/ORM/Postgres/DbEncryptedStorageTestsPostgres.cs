using System.Collections.Generic;
using System.Data;
using System.Linq;
using NUnit.Framework;
using Rainy.Crypto;
using ServiceStack.OrmLite;
using Tomboy;
using Rainy.Db;
using System.Security.Cryptography;
using Tomboy.Db;

namespace Rainy.Tests.Db.Postgres
{
	[TestFixture()]
	public class DbEncryptedStorageTestsPostgres : DbEncryptedStorageTests
	{
		public DbEncryptedStorageTestsPostgres ()
		{
			this.dbScenario = "postgres";
		}
	}
}
