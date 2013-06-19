using System;
using System.Data;
using NUnit.Framework;
using Tomboy;
using System.Collections.Generic;
using System.Linq;

namespace Rainy.Db
{
	[TestFixture()]
	public class DbStorageTests : DbTestsBase
	{
		protected List<Note> GetSampleNotes ()
		{
			var sample_notes = new List<Note> ();
			
			// TODO: add tags to the notes!
			
			sample_notes.Add (new Note () {
				Title = "Sämplé title 1!",
				Text = "** This is the text of Sämple Note 1**",
				CreateDate = DateTime.Now,
				MetadataChangeDate = DateTime.Now,
				ChangeDate = DateTime.Now
			});
			
			sample_notes.Add (new Note () {
				Title = "2nd Example",
				Text = "This is the text of the second sample note",
				CreateDate = new DateTime (1984, 04, 14, 4, 32, 0, DateTimeKind.Utc),
				ChangeDate = new DateTime (2012, 04, 14, 4, 32, 0, DateTimeKind.Utc),
				MetadataChangeDate = new DateTime (2012, 12, 12, 12, 12, 12, DateTimeKind.Utc),
			});
			
			// note that DateTime.MinValue is not an allowed timestamp for notes!
			sample_notes.Add (new Note () {
				Title = "3rd exampel title",
				Text = "Another example note",
				CreateDate = DateTime.MinValue + new TimeSpan (1, 0, 0, 0, 0),
				ChangeDate = DateTime.MinValue + new TimeSpan (1, 0, 0, 0, 0),
				MetadataChangeDate = DateTime.MinValue + new TimeSpan (1, 0, 0, 0, 0)
			});

			return sample_notes;
		}

		[Test]
		public void StoreSomeNotes ()
		{
			var sample_notes = GetSampleNotes ();

			using (var store = new DbStorage (factory, testUser)) {
				sample_notes.ForEach (n => store.SaveNote (n));
			}
			// now check if we have stored that notes
			using (var store = new DbStorage (factory, testUser)) {
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

			using (var store = new DbStorage (factory, testUser)) {
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
			DbStorage storage = new DbStorage(factory, testUser);

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
			DbStorage storage = new DbStorage(factory, testUser);
			
			var tomboy_note = new Note ();
			tomboy_note.ChangeDate = new DateTime (2000, 1, 1, 0, 0, 0, DateTimeKind.Local);
			tomboy_note.CreateDate = tomboy_note.ChangeDate;
			tomboy_note.MetadataChangeDate = tomboy_note.ChangeDate;
			
			storage.SaveNote (tomboy_note);
			var stored_note = storage.GetNotes ().Values.First ();
			
			storage.Dispose ();
			
			Assert.AreEqual (tomboy_note.ChangeDate, stored_note.ChangeDate.ToUniversalTime ());
			
		}
	}

}

