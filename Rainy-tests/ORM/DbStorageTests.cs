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
		private IDbConnection dbConnection;

		[SetUp]
		public void SetUp ()
		{
			this.dbConnection = this.dbFactory.OpenDbConnection ();
		}

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
			string username = "test";
			var sample_notes = GetSampleNotes ();

			using (var store = new DbStorage (username)) {
				sample_notes.ForEach (n => store.SaveNote (n));
			}
			// now check if we have stored that notes
			using (var store = new DbStorage (username)) {
				var stored_notes = store.GetNotes ().Values.ToList ();

				Assert.AreEqual (sample_notes.Count, stored_notes.Count);
				stored_notes.ForEach(n => Assert.Contains (n, sample_notes));

			}
		}

		[Test]
		public void StoreAndDelete ()
		{
			StoreSomeNotes ();

			using (var store = new DbStorage ("test")) {
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
	}
}

