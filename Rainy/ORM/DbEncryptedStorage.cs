using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.OrmLite;
using Tomboy;
using Tomboy.Sync.Web.DTO;
using Rainy.Crypto;
using System.Security.Cryptography;

namespace Rainy.Db
{
	public class DbEncryptedStorage : DbStorage
	{
		private string encryptionMasterKey;
		private bool encryptNotes {
			get { return !string.IsNullOrEmpty (encryptionMasterKey); }
		}

		public DbEncryptedStorage (IDbConnectionFactory factory, DBUser user, string encryption_master_key,
		                           bool use_history = true) : base (factory, user, use_history)
		{

			if (encryption_master_key == null)
				throw new ArgumentNullException ("encryption_master_key");

			encryptionMasterKey = encryption_master_key;
		}

		public override Dictionary<string, Note> GetNotes ()
		{
			var notes = base.GetDBNotes ();
			notes.ForEach (n => DecryptNoteBody (n));
			var dict = notes.ToDictionary (n => n.Guid, n => n.ToDTONote ().ToTomboyNote ());
			return dict;
		}

		public override void SaveNote (Note note)
		{
			var db_note = note.ToDTONote ().ToDBNote (User);

			db_note.EncryptedKey = GetEncryptedNoteKey (db_note);
			EncryptNoteBody (db_note);
			base.SaveDBNote (db_note);
		}
		/// <summary>
		/// Reencrypts all notes. This may happen i.e. if the user changes his password.
		/// </summary>
		/// <param name="new_encryption_key">The new key that is used for encryption.</param>
		public void ReEncryptAllNotes (string new_encryption_key) {
			var old_notes = GetDBNotes ();
			old_notes.ForEach (n => DecryptNoteBody (n));

			this.encryptionMasterKey = new_encryption_key;
			foreach (var note in old_notes) {
				SaveDBNote (note);
			}
		}

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
				encrypted_per_note_key = rng.Create256BitLowerCaseHexKey ().EncryptWithKey (encryptionMasterKey, User.MasterKeySalt);
			}
			return encrypted_per_note_key;
		}
		private void EncryptNoteBody (DBNote note)
		{
			// decrypt the per note key
			var plaintext_key = note.EncryptedKey.DecryptWithKey (encryptionMasterKey, User.MasterKeySalt);
			note.IsEncypted = true;
			note.Text = User.EncryptString (plaintext_key.ToByteArray (), note.Text).ToHexString ();
		}
		private void DecryptNoteBody (DBNote note)
		{
			if (!note.IsEncypted)
				return;
			
			note.Decrypt (User, encryptionMasterKey);
			note.IsEncypted = false;
		}
	}
}
