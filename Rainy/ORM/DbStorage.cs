using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ServiceStack.Common;
using ServiceStack.OrmLite;
using Tomboy;
using Tomboy.Sync.Web.DTO;
using Tomboy.Sync;
using Tomboy.Db;

namespace Rainy.Db
{
	public class DbStorage : IStorage, IDisposable
	{
		public readonly DBUser User;
		public readonly bool UseHistory;
		protected IDbConnection db;
		protected IDbConnectionFactory connFactory;
		protected SyncManifest Manifest;
		protected IDbTransaction trans;

		public DbStorage (IDbConnectionFactory factory, string username, bool use_history = true)
		{
			if (factory == null)
				throw new ArgumentNullException ("factory");
			this.connFactory = factory;

			using (var dbu = connFactory.OpenDbConnection ()) {
				this.User = dbu.Select<DBUser> (u => u.Username == username)[0];
			}
			this.Manifest = this.User.Manifest;

			this.UseHistory = use_history;
			db = factory.OpenDbConnection ();

			// start everything as a transaction
			trans = db.BeginTransaction ();

		}
		public virtual Dictionary<string, Note> GetNotes ()
		{
			// TODO remove double copying
			var notes = GetDBNotes ();

			if (notes.Any (n => n.IsEncypted))
				throw new Exception ("Found encrypted note but using non-encryption backend that can't decrypt.");

			var dict = notes.ToDictionary (n => n.Guid, n => n.ToDTONote ().ToTomboyNote ());
			return dict;
		}
		protected List<DBNote> GetDBNotes ()
		{
			var db_notes = db.Select<DBNote> (dbn => dbn.Username == User.Username);

			return db_notes;
		}
		public void SetPath (string path)
		{
			throw new NotImplementedException ();
		}
		public void SetBackupPath (string path)
		{
			throw new NotImplementedException ();
		}
		public virtual void SaveNote (Note note)
		{
			var db_note = note.ToDTONote ().ToDBNote (User);
			SaveDBNote (db_note);
		}
		protected void SaveDBNote (DBNote db_note)
		{
			// archive any previously existing note into its own table
			// TODO: evaluate how we could re-use the same DBNote table, which will save us
			// a select + reinsert operation
			if (UseHistory) {
				var old_note = db.FirstOrDefault<DBNote> (n => n.CompoundPrimaryKey == db_note.CompoundPrimaryKey);
				if (old_note != null) {
					var archived_note = new DBArchivedNote ().PopulateWith (old_note);
					// set the last known revision

					if (Manifest.NoteRevisions.Keys.Contains (db_note.Guid)) {
						archived_note.LastSyncRevision = Manifest.NoteRevisions[db_note.Guid];
					}
					db.Insert<DBArchivedNote> (archived_note);
				}
			}

			// unforunately, we can't know if that note already exist
			// so we delete any previous incarnations of that note and
			// re-insert
			db.Delete<DBNote> (n => n.CompoundPrimaryKey == db_note.CompoundPrimaryKey);
			db.Insert (db_note);
		}

		public void DeleteNote (Note note)
		{
			var dbNote = note.ToDTONote ().ToDBNote (User);

			if (UseHistory) {
				var archived_note = new DBArchivedNote ().PopulateWith(dbNote);
				if (Manifest.NoteRevisions.ContainsKey (note.Guid)) {
					archived_note.LastSyncRevision = Manifest.NoteRevisions[note.Guid];
				}
				var stored_note = db.FirstOrDefault<DBArchivedNote> (n => n.CompoundPrimaryKey == archived_note.CompoundPrimaryKey);
				// if that revision already exists, do not store
				if (stored_note == null)
					db.Insert<DBArchivedNote> (archived_note);
			}

			db.Delete<DBNote> (n => n.CompoundPrimaryKey == dbNote.CompoundPrimaryKey);
		}
		public void SetConfigVariable (string key, string value)
		{
			throw new System.NotImplementedException ();
		}
		public string GetConfigVariable (string key)
		{
			throw new System.NotImplementedException ();
		}
		public string Backup ()
		{
			throw new System.NotImplementedException ();
		}

		public void Dispose ()
		{
			trans.Commit ();
			trans.Dispose ();

			db.Dispose ();
		}
	}
}
