using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using NUnit.Framework;
using ServiceStack.OrmLite;
using Tomboy;
using Rainy.Db;

namespace Rainy.Tests.Db
{
	public abstract class DbStorageTests : DbTestsBase
	{
		[Test]
		public void StoreSomeNotes ()
		{
			var sample_notes = GetSampleNotes ();

			using (var store = new DbStorage (connFactory, testUser.Username, testUser.Manifest)) {
				foreach (var note in sample_notes) {
					store.SaveNote (note);
				}
			}
			// now check if we have stored that notes
			using (var store = new DbStorage (connFactory, testUser.Username, testUser.Manifest)) {
				var stored_notes = store.GetNotes ().Values.ToList ();

				Assert.AreEqual (sample_notes.Count, stored_notes.Count);
				stored_notes.ForEach(n => Assert.Contains (n, sample_notes));

				// check that the dates are still the same
				stored_notes.ForEach(n => {
					var sample_note = sample_notes.First(sn => sn.Guid == n.Guid);
					Assert.AreEqual (n.ChangeDate, sample_note.ChangeDate);
				});

			}
		}

		[Test]
		public void StoreAndDelete ()
		{
			StoreSomeNotes ();

			using (var store = new DbStorage (connFactory, testUser.Username, testUser.Manifest)) {
				var stored_notes = store.GetNotes ().Values.ToList ();

				var deleted_note = stored_notes[0];
				store.DeleteNote (deleted_note);
				Assert.AreEqual (stored_notes.Count - 1, store.GetNotes ().Values.Count);
				Assert.That (! store.GetNotes ().Values.Contains (deleted_note));

				deleted_note = stored_notes[1];
				store.DeleteNote (deleted_note);
				Assert.AreEqual (stored_notes.Count - 2, store.GetNotes ().Values.Count);
				Assert.That (! store.GetNotes ().Values.Contains (deleted_note));

				deleted_note = stored_notes[2];
				store.DeleteNote (deleted_note);
				Assert.AreEqual (stored_notes.Count - 3, store.GetNotes ().Values.Count);
				Assert.That (! store.GetNotes ().Values.Contains (deleted_note));
			}
		}

		[Test]
		public void DateUtcIsCorrectlyStored ()
		{
			DbStorage storage = new DbStorage(connFactory, testUser.Username, testUser.Manifest);

			var tomboy_note = new Note ();
			tomboy_note.ChangeDate = new DateTime (2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
			tomboy_note.CreateDate = tomboy_note.ChangeDate;
			tomboy_note.MetadataChangeDate = tomboy_note.ChangeDate;

			storage.SaveNote (tomboy_note);
			var stored_note = storage.GetNotes ().Values.First ();

			storage.Dispose ();

			Assert.AreEqual (tomboy_note.ChangeDate, stored_note.ChangeDate.ToUniversalTime ());

		}
		[Test]
		public void DateLocalIsCorrectlyStored ()
		{
			DbStorage storage = new DbStorage(connFactory, testUser.Username, testUser.Manifest);
			
			var tomboy_note = new Note ();
			tomboy_note.ChangeDate = new DateTime (2000, 1, 1, 0, 0, 0, DateTimeKind.Local);
			tomboy_note.CreateDate = tomboy_note.ChangeDate;
			tomboy_note.MetadataChangeDate = tomboy_note.ChangeDate;
			
			storage.SaveNote (tomboy_note);
			var stored_note = storage.GetNotes ().Values.First ();
			
			storage.Dispose ();
			
			Assert.AreEqual (tomboy_note.ChangeDate, stored_note.ChangeDate.ToUniversalTime ());
			
		}
		
		// note history tests
		[Test]
		public void NoteHistoryIsSaved ()
		{
			var sample_notes = DbStorageTests.GetSampleNotes ();

			using (var storage = new DbStorage (this.connFactory, this.testUser.Username, this.testUser.Manifest, use_history: true)) {
				foreach(var note in sample_notes) {
					storage.SaveNote (note);
				}
			}

			// modify the notes
			using (var storage = new DbStorage (this.connFactory, this.testUser.Username, this.testUser.Manifest, use_history: true)) {
				foreach(var note in sample_notes) {
					note.Title = "Random new title";
					storage.SaveNote (note);
				}
			}

			// for each note there should exist a backup copy
			foreach (var note in sample_notes) {
				using (var db = connFactory.OpenDbConnection ()) {
					var archived_note = db.FirstOrDefault<DBArchivedNote> (n => n.Guid == note.Guid);
					Assert.IsNotNull (archived_note);
					Assert.AreNotEqual ("Random new title", archived_note.Title);
				}
			}
		}
	}
}

