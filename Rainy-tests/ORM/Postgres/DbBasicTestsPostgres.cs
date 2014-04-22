using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using DevDefined.OAuth.Framework;
using DevDefined.OAuth.Storage.Basic;
using NUnit.Framework;
using Rainy.Interfaces;
using Rainy.OAuth;
using ServiceStack.Common;
using ServiceStack.OrmLite;
using Tomboy.Sync.Web.DTO;
using Rainy.WebService;
using Rainy.Db;
using Tomboy.Db;

namespace Rainy.Tests.Db.Postgres
{
	public class DbBasicTestsPostgres : DbBasicTests
	{
		public DbBasicTestsPostgres ()
		{
			this.dbScenario = "postgres";
		}
	}
}
