using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ServiceStack.Common;
using ServiceStack.OrmLite;
using Tomboy;
using Tomboy.Sync.Web.DTO;

namespace Rainy.Db
{
	public class DbStorage : DbAccessObject, IStorage, IDisposable
	{
		public readonly DBUser dbUser;
		public readonly bool UseHistory;
		protected IDbConnection db;
		protected IDbTransaction trans;

		public DbStorage (IDbConnectionFactory factory, DBUser user, bool use_history = true) : base (factory)
		{
			if (user == null)
				throw new ArgumentNullException ("user");

			this.UseHistory = use_history;
			this.dbUser = user;
			db = factory.OpenDbConnection ();

			// start everything as a transaction
			trans = db.BeginTransaction ();

		}
		#region IStorage implementation
		public virtual Dictionary<string, Note> GetNotes ()
		{
			// TODO remove double copying
			var notes = GetDBNotes ();
			var dict = notes.ToDictionary (n => n.Guid, n => n.ToDTONote ().ToTomboyNote ());
			return dict;
		}
		protected List<DBNote> GetDBNotes ()
		{
			var db_notes = db.Select<DBNote> (dbn => dbn.Username == dbUser.Username);
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
			var db_note = note.ToDTONote ().ToDBNote (dbUser);
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

					if (dbUser.Manifest.NoteRevisions.Keys.Contains (db_note.Guid)) {
						archived_note.LastSyncRevision = dbUser.Manifest.NoteRevisions[db_note.Guid];
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
			var dbNote = note.ToDTONote ().ToDBNote (dbUser);

			if (UseHistory) {
				var archived_note = new DBArchivedNote ().PopulateWith(dbNote);
				if (dbUser.Manifest.NoteRevisions.ContainsKey (note.Guid)) {
					archived_note.LastSyncRevision = dbUser.Manifest.NoteRevisions[note.Guid];
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
		#endregion


		#region IDisposable implementation
		public void Dispose ()
		{
			trans.Commit ();
			trans.Dispose ();

			db.Dispose ();
		}
		#endregion
	}
}
