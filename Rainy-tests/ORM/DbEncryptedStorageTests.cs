using System.Collections.Generic;
using System.Data;
using NUnit.Framework;
using Rainy.Crypto;
using ServiceStack.OrmLite;
using Tomboy;

namespace Rainy.Db
{

	public abstract class DbEncryptedStorageTests : DbTestsBase
	{
		[Test]
		public void EncryptedStorageStoresNoPlaintextNotes ()
		{
			string key = "d019f8a34c5b2c0fd1444e27ba02eec1f7816739ff98a674043fb3da72bbd625";
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
			string key = "d019f8a34c5b2c0fd1444e27ba02eec1f7816739ff98a674043fb3da72bbd625";
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
			string key = "d019f8a34c5b2c0fd1444e27ba02eec1f7816739ff98a674043fb3da72bbd625";

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

			storage = new DbStorage (this.connFactory, this.testUser);
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
