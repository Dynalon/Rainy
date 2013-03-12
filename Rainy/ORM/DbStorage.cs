using Tomboy;
using ServiceStack.OrmLite;
using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using Tomboy.Sync.DTO;

namespace Rainy.Db
{
	public class DbStorage : IStorage, IDisposable
	{
		public readonly string Username;
		private IDbConnection db;
		private IDbTransaction trans;

		public DbStorage (string username)
		{
			this.db = DbConfig.GetConnection ();
			this.Username = username;

			// start everything as a transaction
			trans = db.BeginTransaction ();

		}
		#region IStorage implementation
		public Dictionary<string, Note> GetNotes ()
		{
			var notes = db.Select<DBNote> (dbn => dbn.Username == this.Username);
			// TODO remove the double copying
			var dict = notes.ToDictionary (n => n.Guid, n => n.ToDTONote ().ToTomboyNote ());
			return dict;

		}
		public void SetPath (string path)
		{
		}
		public void SetBackupPath (string path)
		{
		}
		public void SaveNote (Note note)
		{
			var dbNote = note.ToDTONote ().ToDBNote ();
			dbNote.Username = Username;

			// unforunately, we can't know if that note already exist
			// so we delete any previous incarnations of that note and
			// re-insert
			db.Delete<DBNote> (n => n.Username == Username && n.Guid == dbNote.Guid);
			db.Insert (dbNote);
		}
		public void DeleteNote (Note note)
		{
			db.Delete<DBNote> (n => Username == n.Username &&  n.Guid == note.Guid);
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
