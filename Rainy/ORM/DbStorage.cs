using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ServiceStack.OrmLite;
using Tomboy;
using Tomboy.Sync.Web.DTO;
using Rainy.Crypto;
using System.Security.Cryptography;
using ServiceStack.Common;

namespace Rainy.Db
{
	public class DbStorage : DbAccessObject, IStorage, IDisposable
	{
		public readonly DBUser dbUser;
		public readonly bool UseHistory;
		private IDbConnection db;
		private IDbTransaction trans;

		private string encryptionMasterKey;
		private bool encryptNotes {
			get { return !string.IsNullOrEmpty (encryptionMasterKey); }
		}

		public DbStorage (IDbConnectionFactory factory, DBUser user, string encryption_master_key, bool use_history = true)
			: this (factory, user, use_history)
		{
			if (encryption_master_key == null)
				throw new ArgumentNullException ("encryption_master_key");

			encryptionMasterKey = encryption_master_key;
		}
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
		public Dictionary<string, Note> GetNotes ()
		{
			var notes = db.Select<DBNote> (dbn => dbn.Username == dbUser.Username);

			if (encryptNotes) {
				notes.ForEach(n => DecryptNoteBody (n));
			}

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
			var db_note = note.ToDTONote ().ToDBNote (dbUser);

			if (encryptNotes) {
				// we need to check if the note exists, and if so, use the same encryption key
				db_note.EncryptedKey = GetEncryptedNoteKey (db_note);
				EncryptNoteBody (db_note);
			}

			// archive any previously existing note into its own table
			// TODO: evaluate how we could re-use the same DBNote table, which will save us
			// a select + reinsert operation
			if (UseHistory) {
				var old_note = db.FirstOrDefault<DBNote> (n => n.CompoundPrimaryKey == db_note.CompoundPrimaryKey);
				if (old_note != null) {
					var archived_note = new DBArchivedNote ().PopulateWith (old_note);
					// set the last known revision

					if (dbUser.Manifest.NoteRevisions.Keys.Contains (note.Guid)) {
						archived_note.LastSyncRevision = dbUser.Manifest.NoteRevisions[note.Guid];
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

		private string GetEncryptedNoteKey (DBNote note)
		{
			// re-use the same key when saving a note
			string encrypted_per_note_key;

			var saved_note = db.FirstOrDefault<DBNote> (n => n.CompoundPrimaryKey == note.CompoundPrimaryKey);
			if (saved_note != null) {
				encrypted_per_note_key = saved_note.EncryptedKey;
			} else {
				// new note, generate a new key
				var rng = new RNGCryptoServiceProvider ();
				encrypted_per_note_key = rng.Create256BitLowerCaseHexKey ().EncryptWithKey (encryptionMasterKey, dbUser.MasterKeySalt);
			}
			return encrypted_per_note_key;
		}
		private void EncryptNoteBody (DBNote note)
		{
			// decrypt the per note key
			var plaintext_key = note.EncryptedKey.DecryptWithKey (encryptionMasterKey, dbUser.MasterKeySalt);
			note.IsEncypted = true;
			note.Text = dbUser.EncryptString (plaintext_key.ToByteArray (), note.Text).ToHexString ();
		}
		private void DecryptNoteBody (DBNote note)
		{
			if (!note.IsEncypted)
				return;

			note.Decrypt (dbUser, encryptionMasterKey);
		}

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
