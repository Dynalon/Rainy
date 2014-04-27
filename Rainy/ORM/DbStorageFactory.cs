using System;
using ServiceStack.OrmLite;
using Rainy.WebService;

namespace Rainy.Db
{
	public class DbStorageFactory
	{
		protected bool useHistory;
		protected bool useEncryption;
		protected IDbConnectionFactory connFactory;
		public DbStorageFactory (IDbConnectionFactory conn_factory, bool use_encryption, bool use_history)
		{
			this.useEncryption = use_encryption;
			this.useHistory = use_history;
			this.connFactory = conn_factory;
		}
		public DbStorage GetDbStorage (IUser user) {
			DBUser db_user;
			using (var db = connFactory.OpenDbConnection ()) {
				db_user = db.First<DBUser> (u => u.Username == user.Username);
				if (db_user == null)
					throw new ArgumentException (user.Username);
			}
			if (useEncryption) {
				if (string.IsNullOrEmpty (user.EncryptionMasterKey)) {
					throw new ArgumentException ("MasterKey is required", "EncryptionMasterKey");
				}
				var master_key = user.EncryptionMasterKey;
				return (DbStorage)new DbEncryptedStorage (connFactory, db_user, master_key, useHistory);
			} else {
				return new DbStorage (connFactory, db_user.Username, db_user.Manifest, useHistory);
			}
		}
	}
	
}
