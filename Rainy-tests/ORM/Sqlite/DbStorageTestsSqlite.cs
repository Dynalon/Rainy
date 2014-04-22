using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using NUnit.Framework;
using ServiceStack.OrmLite;
using Tomboy;
using Rainy.Db;

namespace Rainy.Tests.Db.Sqlite
{
	[TestFixture()]
	public class DbStorageTestsSqlite : DbStorageTests
	{
		public DbStorageTestsSqlite ()
		{
			this.dbScenario = "sqlite";
		}
	}
}
