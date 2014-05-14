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

	/// <summary>
	/// Authenticates a user against a database. User objects in the database always employ hashed passwords.
	/// </summary>
	public class DbAuthenticator : IAuthenticator
	{
		private IDbConnectionFactory connFactory;

		public DbAuthenticator (IDbConnectionFactory factory)
		{
			if (factory == null)
				throw new ArgumentNullException ("factory");

			connFactory = factory;
		}

		public bool VerifyCredentials (string username, string password)
		{
			DBUser user = null;
			using (var conn = connFactory.OpenDbConnection ()) {
				user = conn.FirstOrDefault<DBUser> (u => u.Username == username);
			}
			if (user == null)
				return false;

			if (user.IsActivated == false) {
				throw new Rainy.ErrorHandling.UnauthorizedException () {
					UserStatus = "Moderation required",
				};
			}

			//if (user.IsVerified == false)
			//	return false;

			var supplied_hash = user.ComputePasswordHash (password);
			if (supplied_hash == user.PasswordHash)
				return true;

			return false;

		}

	}

	// maybe move into DatabaseBackend as nested class

}