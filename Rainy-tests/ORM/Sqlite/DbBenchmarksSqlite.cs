using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Threading.Tasks;
using NUnit.Framework;
using Rainy.Db;
using ServiceStack.OrmLite;
using Tomboy.Db;
using Rainy.Tests.Benchmarks;
using Rainy.Tests.Db;

namespace Rainy.Tests.Db.Sqlite.Benchmarks
{
	[Ignore]
	[TestFixture]
	public class DbBenchmarksSqlite : DbBenchmarks
	{
		public DbBenchmarksSqlite ()
		{
			this.dbScenario = "sqlite";
		}
	}

}
