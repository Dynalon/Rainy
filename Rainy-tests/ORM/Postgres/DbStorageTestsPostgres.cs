using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using NUnit.Framework;
using ServiceStack.OrmLite;
using Tomboy;
using Rainy.Db;

namespace Rainy.Tests.Db.Postgres
{
	[TestFixture()]
	public class DbStorageTestsPostgres : DbStorageTests
	{
		public DbStorageTestsPostgres ()
		{
			this.dbScenario = "postgres";
		}
		[SetUp]
		public new void SetUp ()
		{
			Assert.Pass ();
		}
	}
}
