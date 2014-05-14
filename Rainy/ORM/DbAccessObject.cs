using System;
using System.Data;

using Rainy.OAuth;
using Rainy.Db;
using ServiceStack.OrmLite;
using Tomboy;
using Tomboy.Sync;
using Rainy.Interfaces;
using Rainy.Crypto;
using Rainy.WebService;
using DevDefined.OAuth.Storage.Basic;
using Tomboy.Db;

namespace Rainy
{
	public class DbAccessObject
	{
		protected IDbConnectionFactory connFactory;
		public DbAccessObject (IDbConnectionFactory factory)
		{
			connFactory = factory;
		}
	}

	// maybe move into DatabaseBackend as nested class

}