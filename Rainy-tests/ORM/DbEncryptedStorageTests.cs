using System.Collections.Generic;
using System.Data;
using System.Linq;
using NUnit.Framework;
using Rainy.Crypto;
using ServiceStack.OrmLite;
using Tomboy;
using Rainy.Db;
using System.Security.Cryptography;
using Tomboy.Db;

namespace Rainy.Tests.Db
{

	public abstract class DbEncryptedStorageTests : DbTestsBase
	{
		protected string key = "d019f8a34c5b2c0fd1444e27ba02eec1f7816739ff98a674043fb3da72bbd625";

		[Test]
		public void EncryptedStorageStoresNoPlaintextNotes ()
		{
			var storage = new DbEncryptedStorage (connFactory, testUser, key);

			var sample_notes = DbStorageTests.GetSampleNotes ();
			foreach(var note in sample_notes) {
				storage.SaveNote (note);
			}
			storage.Dispose ();

			foreach(var note in sample_notes) {
				// the stored notes should only contain hex chars
				using (var db = connFactory.OpenDbConnection ()) {
					var db_note = db.First<DBNote> (n => n.Guid == note.Guid);
					// this will fail if any non-hex chars are in
					db_note.Text.ToByteArray ();
				}
			}
		}

		[Test]
		public void NoteIsAlwaysEncryptedWithSameKey ()
		{
			string first_key;
			var note = DbStorageTests.GetSampleNotes ()[0];
			// save for first time
			using (var storage = new DbEncryptedStorage (connFactory, testUser, key)) {
				storage.SaveNote (note);
			}
			using (var db = connFactory.OpenDbConnection ()) {
				var db_note = db.First<DBNote> (n => n.Guid == note.Guid);
				first_key = db_note.EncryptedKey;
				Assert.That (!string.IsNullOrEmpty (first_key));
			}

			// change the text and store note again
			note.Text = "Foobar";

			// save for first time
			using (var storage = new DbEncryptedStorage (connFactory, testUser, key)) {
				storage.SaveNote (note);
			}
			using (var db = connFactory.OpenDbConnection ()) {
				var db_note = db.First<DBNote> (n => n.Guid == note.Guid);
				Assert.AreEqual (first_key, db_note.EncryptedKey);
			}
		}

		[Test]
		public void NoteIsEncryptedIsCorrectlySet ()
		{
			// test with encrypted notes
			DbStorage storage = new DbEncryptedStorage (this.connFactory, this.testUser, key);
			var sample_notes = DbStorageTests.GetSampleNotes ();
			foreach(var note in sample_notes) {
				storage.SaveNote (note);
			}
			storage.Dispose ();

			foreach(var note in sample_notes) {
				// the stored notes should only contain hex chars
				using (var db = connFactory.OpenDbConnection ()) {
					var db_note = db.First<DBNote> (n => n.Guid == note.Guid);
					// this will fail if any non-hex chars are in
					Assert.IsTrue (db_note.IsEncypted);
				}
			}

			storage = new DbStorage (this.connFactory, testUser.Username, testUser.Manifest);
			sample_notes = DbStorageTests.GetSampleNotes ();
			foreach(var note in sample_notes) {
				storage.SaveNote (note);
			}
			storage.Dispose ();

			foreach(var note in sample_notes) {
				// the stored notes should only contain hex chars
				using (var db = connFactory.OpenDbConnection ()) {
					var db_note = db.First<DBNote> (n => n.Guid == note.Guid);
					// this will fail if any non-hex chars are in
					Assert.IsFalse (db_note.IsEncypted);
				}
			}
		}

		[Test]
		public void ReEncryptNotes ()
		{
			DbEncryptedStorage storage = new DbEncryptedStorage (this.connFactory, this.testUser, key);
			var sample_notes = DbStorageTests.GetSampleNotes ();
			foreach(var note in sample_notes) {
				storage.SaveNote (note);
			}
			storage.Dispose ();

			storage = new DbEncryptedStorage (connFactory, testUser, key);
			var new_key = CryptoHelper.Create256BitLowerCaseHexKey (new RNGCryptoServiceProvider ());

			storage.ReEncryptAllNotes (new_key);
			storage.Dispose ();

			// try to open the notes with the new key
			storage = new DbEncryptedStorage (connFactory, testUser, new_key);
			var decrypted_notes = storage.GetNotes ();
			foreach (var note in sample_notes) {
				Assert.AreEqual (note.Text, decrypted_notes.Values.First (n => n.Guid == note.Guid).Text);
			}
		}

		[Test]
		public void StorageCanBeReusedAfterReEncryptNotes ()
		{
			DbEncryptedStorage storage = new DbEncryptedStorage (this.connFactory, this.testUser, key);
			var sample_notes = DbStorageTests.GetSampleNotes ();
			foreach(var note in sample_notes) {
				storage.SaveNote (note);
			}
			storage.Dispose ();

			storage = new DbEncryptedStorage (connFactory, testUser, key);
			var new_key = CryptoHelper.Create256BitLowerCaseHexKey (new RNGCryptoServiceProvider ());

			storage.ReEncryptAllNotes (new_key);
			var decrypted_notes = storage.GetNotes ();

			foreach (var note in sample_notes) {
				Assert.AreEqual (note.Text, decrypted_notes.Values.First (n => n.Guid == note.Guid).Text);
			}
		}
	}

	[TestFixture()]
	public class DbEncryptedStorageTestsSqlite : DbEncryptedStorageTests
	{
		public DbEncryptedStorageTestsSqlite ()
		{
			this.dbScenario = "sqlite";
		}
	}

	[TestFixture()]
	public class DbEncryptedStorageTestsPostgres : DbEncryptedStorageTests
	{
		public DbEncryptedStorageTestsPostgres ()
		{
			this.dbScenario = "postgres";
		}
	}
}
