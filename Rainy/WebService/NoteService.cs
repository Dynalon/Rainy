using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.Common;
using ServiceStack.ServiceHost;
using Rainy.Interfaces;
using DTO = Tomboy.Sync.Web.DTO;
using Rainy.ErrorHandling;
using ServiceStack.OrmLite;
using Rainy.NoteConversion;
using Tomboy.Sync.Web.DTO;


namespace Rainy.WebService
{
	public interface IUser
	{
		string Username { get; set; }
		// this is plaintext representation of the master key!
		string EncryptionMasterKey { get; set; }
	}

	public class NotesService : RainyNoteServiceBase
	{
		public NotesService (IDataBackend backend, IDbConnectionFactory factory) : base (factory, backend)
		{
		}
		protected static DTO.GetNotesResponse GetStoredNotes (INoteRepository note_repo)
		{
			try {
			var notes = new List<DTO.DTONote> ();
			var stored_notes = note_repo.Engine.GetNotes ();
			
			foreach (var kvp in stored_notes) {
				var note = kvp.Value.ToDTONote ();

				// if we have a sync revision, set it	
				if (note_repo.Manifest.NoteRevisions.Keys.Contains (note.Guid)) {
					note.LastSyncRevision = note_repo.Manifest.NoteRevisions [note.Guid];
				}

				notes.Add (note);
			}
			
			var return_notes = new DTO.GetNotesResponse ();
			return_notes.Notes = notes;
			return_notes.LatestSyncRevision = note_repo.Manifest.LastSyncRevision;

			return return_notes;
			} catch (Exception e) {
				throw e;
			}
			return null;
		}

		public GetSingleNoteResponse Get (GetSingleNoteRequest request) 
		{
			IList<DTONote> ret = new List<DTONote> ();
			using (var note_repo = GetNotes ()) {
				var notes = note_repo.Engine.GetNotes ();
				if (!notes.ContainsKey (request.Guid)) {
					Logger.Debug ("this guid does not exist");
					throw new InvalidRequestDtoException ();
				}
				ret.Add (notes[request.Guid].ToDTONote ());
				return new GetSingleNoteResponse { Note = ret };
			}

		}

		// webservice method: HTTP GET request
		public object Get (GetNotesRequest request)
		{
			try {
				using (var note_repo = GetNotes ()) {
					var notes = GetStoredNotes (note_repo);

					// check if we need to include the note body
					bool include_note_body = true; 
					string include_notes = Request.GetParam ("include_notes");
					if (!string.IsNullOrEmpty (include_notes) && !bool.TryParse (include_notes, out include_note_body))
						throw new InvalidRequestDtoException () {ErrorMessage = "unable to parse parameter include_notes to boolean"};

					// check if we transform the note content to HTML
					bool notes_as_html = false; 
					string to_html = Request.GetParam ("notes_as_html");
					if (!string.IsNullOrEmpty (to_html) && !bool.TryParse (to_html, out notes_as_html))
						throw new InvalidRequestDtoException () {ErrorMessage = "unable to parse parameter notes_as_html to boolean"};

					// if since is given, we might only need to return a subset of notes
					string since = Request.GetParam ("since");
					long since_revision = -1;
					if (!string.IsNullOrEmpty (since) && !long.TryParse (since, out since_revision))
						throw new InvalidRequestDtoException () {ErrorMessage = "unable to parse parameter since to long"};

					// select only those notes that changed since last sync
					// which means, only those notes that have a HIGHER revision as "since"
					var changed_notes = notes.Notes.Where (n => {
						if (!note_repo.Manifest.NoteRevisions.Keys.Contains (n.Guid))
							// the note is in the storage, but not in the manifest
							// this might happen when low-level adding nodes to the storage
							return true;
						else if (note_repo.Manifest.NoteRevisions [n.Guid] > since_revision)
								return true;
						else
							return false;
					});

					if (include_note_body) {
						notes.Notes = changed_notes.ToList ();

						if (notes_as_html) {
							notes.Notes = notes.Notes.Select (n => { n.Text = n.Text.ToHtml (); return n; }).ToList ();
						}
					} else {
						// empty the note Text
						notes.Notes = changed_notes.Select (n => {
							n.Text = "";
							return n; }).ToList ();
					}

					return notes;
				}
			} catch (Exception e) {
				Logger.DebugFormat ("CAUGHT EXCEPTION: {0} {1}", e.Message, e.StackTrace);
				throw;
			}
		}

		public object Post (PutNotesRequest request)
		{
			return Put (request);
		}
		public object Put (PutNotesRequest request)
		{
			try {
				// check if we need to include the note body
				bool notes_as_html = false; 
				string as_html = Request.GetParam ("notes_as_html");
				if (!string.IsNullOrEmpty (as_html) && !bool.TryParse (as_html, out notes_as_html))
					throw new InvalidRequestDtoException () {ErrorMessage = "unable to parse parameter notes_as_html to boolean"};

				using (var note_repo = GetNotes ()) {

					// constraint taken from snowy source code at http://git.gnome.org/browse/snowy/tree/api/handlers.py:143
					var new_sync_rev = note_repo.Manifest.LastSyncRevision + 1;

					// TODO LatestSyncRevision is not correctly SERIALIZED
					Logger.DebugFormat ("client sent LatestSyncRevision: {0}", request.LatestSyncRevision);

					// TODO sanitize LatestSyncRevision sent by client - we don't need it to update notes
					// but a wrong LatestSyncRevision may be an indicator for a bug in the client

					bool notes_were_deleted_or_uploaded = false;

					foreach (var dto_note in request.Notes) {
						notes_were_deleted_or_uploaded = true;
						// map from the DTO 
						if (notes_as_html) {
							dto_note.Text = dto_note.Text.ToTomboyXml ();
						}

						var note = dto_note.ToTomboyNote ();

						if (dto_note.Command == "delete") {
							note_repo.Engine.DeleteNote (note);
						} else {
							// track the revision of the note
							note_repo.Manifest.NoteRevisions [dto_note.Guid] = (int)new_sync_rev;
							note_repo.Engine.SaveNote (note, false);
						}
					}
					var notes_to_return = NotesService.GetStoredNotes (note_repo);

					if (notes_were_deleted_or_uploaded) {
						note_repo.Manifest.LastSyncRevision = new_sync_rev;
						note_repo.Manifest.LastSyncDate = DateTime.UtcNow;
						notes_to_return.LatestSyncRevision = new_sync_rev;
					}
					return notes_to_return;
				}
			} catch (Exception e) {
				// log the error and rethrow
				Logger.DebugFormat ("CAUGHT EXCEPTION: {0} {1}", e.Message, e.StackTrace);
				throw e;
			}
		}
	}
}
