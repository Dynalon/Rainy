using System;
using System.Data;
using Rainy.Db;
using ServiceStack.OrmLite;
using Tomboy;
using Tomboy.Sync;
using Rainy.Interfaces;
using Rainy.WebService;

namespace Rainy
{

	// maybe move into DatabaseBackend as nested class
	public class DatabaseNoteRepository : DbAccessObject, INoteRepository
	{
		private DbStorage storage;
		private Engine engine;
		private DBUser dbUser;

		public DatabaseNoteRepository (IDbConnectionFactory factory, DbStorageFactory storageFactory, IUser user) : base (factory)
		{
			this.storage = storageFactory.GetDbStorage (user);
			engine = new Engine (storage);

			using (var db = connFactory.OpenDbConnection ()) {
				this.dbUser = db.Select<DBUser> (u => u.Username == user.Username)[0];
			}

			if (dbUser.Manifest == null || string.IsNullOrEmpty (dbUser.Manifest.ServerId)) {
				// the user may not yet have synced
				dbUser.Manifest.ServerId = Guid.NewGuid ().ToString ();
			}
		}

		#region IDisposable implementation
		public void Dispose ()
		{
			storage.Dispose ();

			// write back the user's Manifest which has likely changed
			using (var db = connFactory.OpenDbConnection ()) {
				using (var trans = db.OpenTransaction ()) {
					db.UpdateOnly (dbUser, u => u.Manifest, u => u.Username == dbUser.Username);
					trans.Commit ();
				}
			}
		}
		#endregion
		#region INoteRepository implementation
		public Engine Engine {
			get {
				return engine;
			}
		}
		public SyncManifest Manifest {
			get {
				return dbUser.Manifest;
			}
		}
		#endregion
	}

}