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
		}
		
		[Test()]
		public void WebSyncServerPutNotes ()
		{
			var server = new WebSyncServer (baseUri, GetAccessToken ());
			server.BeginSyncTransaction ();
			
			server.UploadNotes (sampleNotes);
			
			// after upload, we should be able to get that very same notes
			var received_notes = server.GetAllNotes (true);

			Assert.AreEqual(sampleNotes.Count, received_notes.Count);
			sampleNotes.ToList().ForEach (local_note => {

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
	}
}

