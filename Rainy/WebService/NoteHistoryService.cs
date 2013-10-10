using System.Linq;
using ServiceStack.OrmLite;
using ServiceStack.ServiceHost;
using Tomboy.Sync.Web.DTO;
using Rainy.Db;
using Rainy.ErrorHandling;
using DTO = Tomboy.Sync.Web.DTO;
using Rainy.Crypto;
using Tomboy.Db;

namespace Rainy.WebService
{
	public class NoteHistoryService : OAuthServiceBase
	{
		public NoteHistoryService () : base ()
		{
		}
		public NoteHistoryService (IDbConnectionFactory factory) : base (factory)
		{
		}

		public object Get (GetNoteHistoryRequest request)
		{
			string include_note_text = Request.GetParam ("include_text");
			bool include_text = false;
			if (!string.IsNullOrEmpty (include_note_text) && !bool.TryParse (include_note_text, out include_text))
				throw new InvalidRequestDtoException () {ErrorMessage = "unable to parse parameter include_text to boolean"};

			var resp = new NoteHistoryResponse () {
				CurrentRevision = -1,
				Versions = new NoteHistory[] {}
			};

			using (var db = connFactory.OpenDbConnection ()) {
				var revisions = db.First<DBUser> (u => u.Username == requestingUser.Username).Manifest.NoteRevisions;
				if (revisions.ContainsKey (request.Guid)) {
					// note was not deleted in the meantime
					resp.CurrentRevision = revisions[request.Guid];
				} else {
					throw new Rainy.ErrorHandling.InvalidRequestDtoException ();
				}

				var archived_notes = db.Select<DBArchivedNote> (n => n.Username == requestingUser.Username && n.Guid == request.Guid);

				DBUser user = null;
				resp.Versions = archived_notes.Select (note => { 
					var history = new NoteHistory () { Revision = note.LastSyncRevision };

					if (note.IsEncypted && include_text) {
						if (user == null) 
							user = db.First<DBUser> (u => u.Username == requestingUser.Username);

						note.Decrypt (user, requestingUser.EncryptionMasterKey);
					} else if (!include_text) {
						note.Text = "";
					}

					history.Note = ((DBNote) note).ToDTONote ();
					return history;
				}).ToArray ();
			}
			return resp;
		}

		public object Get (GetArchivedNoteRequest request)
		{
			using (var db = connFactory.OpenDbConnection ()) {
				var archived_note = db.FirstOrDefault<DBArchivedNote> (an => an.Username == requestingUser.Username && an.Guid == request.Guid);

				if (archived_note == null)
					throw new Rainy.ErrorHandling.InvalidRequestDtoException ();

				DBUser db_user = null;
				if (archived_note.IsEncypted) {
					if (db_user == null) 
						db_user = db.First<DBUser> (u => u.Username == requestingUser.Username);

					archived_note.Decrypt (db_user, requestingUser.EncryptionMasterKey);
				}

				return archived_note.ToDTONote ();
			}
		}

		public object Get (GetNoteArchiveRequest request)
		{
			var resp = new NoteArchiveResponse ();
			using (var db = connFactory.OpenDbConnection ()) {
				var full_archive = db.Select<DBArchivedNote> (an => an.Username == requestingUser.Username);
				resp.Notes = full_archive.Select (an => {
					var dto_note = an.ToDTONote ();
					dto_note.Text = "";
					return dto_note;
				}).ToArray ();

				resp.Guids = full_archive.Select (an => an.Guid).ToArray<string> ();
			}
			return resp;
		}


		// TODO move into own service
		public object Get (GetPublicUrlForNote request)
		{
			throw new Rainy.ErrorHandling.InvalidRequestDtoException ();
		}
	}
}
