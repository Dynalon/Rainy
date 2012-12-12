using NUnit.Framework;
using Tomboy.Sync.Web;
using System.Collections.Generic;
using Tomboy;
using System.Linq;

namespace Rainy.Tests
{
	[TestFixture()]
	public class WebSyncServerTests : RainyTestBase
	{
		[SetUp]
		public void SetUp ()
		{

		}

		[Test()]
		public void WebSyncServerBasic ()
		{
			var server = new WebSyncServer (baseUri, GetAccessToken ());
			server.BeginSyncTransaction ();

			Assert.That (!string.IsNullOrEmpty (server.Id));
		}
		
		[Test()]
		public void WebSyncServerPutNotes ()
		{
			var server = new WebSyncServer (baseUri, GetAccessToken ());
			server.BeginSyncTransaction ();
			
			server.UploadNotes (sampleNotes);
			
			// after upload, we should be able to get that very same notes
			var received_notes = server.GetAllNotes (true);

			Assert.AreEqual (sampleNotes.Count, received_notes.Count);
			Assert.AreEqual (sampleNotes.Count, server.UploadedNotes.Count);


			sampleNotes.ToList().ForEach (local_note => {

				Assert.That (server.UploadedNotes.Contains (local_note));
				// pick the note from out returned notes list as the order may be
				// different
				var server_note = received_notes.Where (n => n.Guid == local_note.Guid).FirstOrDefault ();
				Assert.NotNull (server_note);

				// assert notes are equal
				Assert.AreEqual(local_note.Title, server_note.Title);
				Assert.AreEqual(local_note.Text, server_note.Text);
				Assert.AreEqual(local_note.CreateDate, server_note.CreateDate);

				// FAILs: Rainy is not allowed to save the ChangeDate in its own engine
				Assert.AreEqual(local_note.MetadataChangeDate, server_note.MetadataChangeDate);
				Assert.AreEqual(local_note.ChangeDate, server_note.ChangeDate);


			});

		}

		[Test()]
		public void WebSyncServerGetAllNotes ()
		{
			var server = new WebSyncServer (baseUri, GetAccessToken ());
			server.BeginSyncTransaction ();
			server.GetAllNotes (true);
		}

		[Test()]
		public void WebSyncServerDeleteAllNotes()
		{
			var server = new WebSyncServer (baseUri, GetAccessToken ());
			server.BeginSyncTransaction ();

			server.UploadNotes (sampleNotes);
			server.DeleteNotes (sampleNotes.Select (n => n.Guid).ToList ());
			var server_notes = server.GetAllNotes (false);

			Assert.AreEqual (0, server_notes.Count);

			server.DeletedServerNotes.ToList ().ForEach (deleted_note_guid => {
				Assert.That (sampleNotes.Select(n => n.Guid).Contains (deleted_note_guid));
			});
		}
		[Test()]
		public void WebSyncServerDeleteSingleNote ()
		{
			var server = new WebSyncServer (baseUri, GetAccessToken ());
			server.BeginSyncTransaction ();

			server.UploadNotes (sampleNotes);
			var deleted_note = sampleNotes.First ();
			server.DeleteNotes (new List<string> () { deleted_note.Guid });

			var server_notes = server.GetAllNotes (false);

			// 2 notes should remain on the server
			Assert.AreEqual (2, server_notes.Count);
			// the deleted note should not be one of them
			Assert.AreEqual (0, server_notes.Where (n => n.Guid == deleted_note.Guid).Count ());

			Assert.AreEqual (deleted_note.Guid, server.DeletedServerNotes.First ());

		}
	}
}

