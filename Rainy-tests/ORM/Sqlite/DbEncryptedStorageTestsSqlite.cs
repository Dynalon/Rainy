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

namespace Rainy.Tests.Db.Sqlite
{
	[TestFixture()]
	public class DbEncryptedStorageTestsSqlite : DbEncryptedStorageTests
	{
		public DbEncryptedStorageTestsSqlite ()
		{
			this.dbScenario = "sqlite";
		}
	}
}
