using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ServiceStack.OrmLite;
using Tomboy;
using Tomboy.Sync.Web.DTO;
using Rainy.Crypto;

namespace Rainy.Db
{
	public class DbStorage : DbAccessObject, IStorage, IDisposable
	{
		public readonly DBUser dbUser;
		private IDbConnection db;
		private IDbTransaction trans;

		private byte[] encryptionKey;
		private bool encryptNotes {
			get { return encryptionKey != null && encryptionKey.Length > 0; }
		}

		public DbStorage (IDbConnectionFactory factory, DBUser user, byte[] encryption_key)
			: this (factory, user)
		{
			encryptionKey = encryption_key;
		}
		public DbStorage (IDbConnectionFactory factory, DBUser user) : base (factory)
		{
			encryptionKey = "d019f8a34c5b2c0fd1444e27ba02eec1f7816739ff98a674043fb3da72bbd625".ToByteArray ();
			if (user == null)
				throw new ArgumentNullException ("user");

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
				notes.ForEach(n => n.Text = dbUser.DecryptUnicodeString (encryptionKey, n.Text.ToByteArray()));
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
			var dbNote = note.ToDTONote ().ToDBNote (dbUser);

			if (encryptNotes) {
				dbNote.Text = dbUser.EncryptUnicodeString (encryptionKey, dbNote.Text).ToHexString ();
			}

			// unforunately, we can't know if that note already exist
			// so we delete any previous incarnations of that note and
			// re-insert
			db.Delete<DBNote> (n => n.CompoundPrimaryKey == dbNote.CompoundPrimaryKey);
			db.Insert (dbNote);
		}
		public void DeleteNote (Note note)
		{
			var dbNote = note.ToDTONote ().ToDBNote (dbUser);
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
