using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.Common;
using ServiceStack.ServiceHost;
using Tomboy;
using Rainy.Interfaces;

namespace Rainy.WebService
{
	public class NotesService : RainyServiceBase
	{
		protected static IDataBackend DataBackend;
		protected static Tomboy.Sync.DTO.GetNotesResponse GetStoredNotes (INoteRepository note_repo)
		{
			var notes = new List<Tomboy.Sync.DTO.DTONote> ();
			var stored_notes = note_repo.Engine.GetNotes ();
			
			foreach (var kvp in stored_notes) {
				var note = new Tomboy.Sync.DTO.DTONote ();
				note.PopulateWith (kvp.Value);

				// if we have a sync revision, set it	
				if (note_repo.Manifest.NoteRevisions.Keys.Contains (note.Guid)) {
					note.LastSyncRevision = note_repo.Manifest.NoteRevisions [note.Guid];
				}

				notes.Add (note);
			}
			
			var return_notes = new Tomboy.Sync.DTO.GetNotesResponse ();
			return_notes.Notes = notes;
			return_notes.LatestSyncRevision = note_repo.Manifest.LastSyncRevision;

			return return_notes;
		}

		// webservice method: HTTP GET request
		public object Get (GetNotesRequest request)
		{
			try {
				using (var note_repo = GetNotes (request.Username)) {
					var notes = GetStoredNotes (note_repo);

					// check if we need to include the note body
					bool include_note_body = true; 
					string include_notes = Request.GetParam ("include_notes");
					if (!string.IsNullOrEmpty (include_notes) && !bool.TryParse (include_notes, out include_note_body))
						throw new Exception ("unable to parse parameter include_notes to boolean");

					// if since is given, we might only need to return a subset of notes
					string since = Request.GetParam ("since");
					long since_revision = -1;
					if (!string.IsNullOrEmpty (since) && !long.TryParse (since, out since_revision))
						throw new Exception ("unable to parse parameter since to long");

					// select only those notes that changed since last sync
					// which means, only those notes that have a HIGHER revision as "since"
					var changed_notes = notes.Notes.Where (n => {
						if (note_repo.Manifest.NoteRevisions.Keys.Contains (n.Guid)) {
							if (note_repo.Manifest.NoteRevisions [n.Guid] > since_revision)
								return true;
						}
						return false;
					});

					if (include_note_body) {
						notes.Notes = changed_notes.ToList ();
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
				using (var note_repo = GetNotes (request.Username)) {

					// constraint taken from snowy source code at http://git.gnome.org/browse/snowy/tree/api/handlers.py:143
					var new_sync_rev = note_repo.Manifest.LastSyncRevision + 1;

					// TODO LatestSyncRevision is not correctly SERIALIZED
					Logger.DebugFormat ("client sent LatestSyncRevision: {0}", request.LatestSyncRevision);

					// TODO sanitize LatestSyncRevision sent by client - we don't need it to update notes
					// but a wrong LatestSyncRevision may be an indicator for a bug in the client

					//if (new_sync_rev != note_repo.Manifest.LatestSyncRevision + 1)
					//	throw new Exception ("Sync revisions differ by more than one, sth went wrong");

					foreach (var dto_note in request.Notes) {
						var note = new Note ("note://tomboy/" + dto_note.Guid);
						// map from the DTO 
						note.PopulateWith (dto_note);

						if (dto_note.Command == "delete") {
							note_repo.Engine.DeleteNote (note);
						} else {
							// track the revision of the note
							note_repo.Manifest.NoteRevisions [dto_note.Guid] = (int)new_sync_rev;
							note_repo.Engine.SaveNote (note, false);
						}
					}


					// only update the sync revision if changes were sent
					if (request.Notes.Count > 0)
						note_repo.Manifest.LastSyncRevision = new_sync_rev;

					var notes_to_return = NotesService.GetStoredNotes (note_repo);
					notes_to_return.LatestSyncRevision = new_sync_rev;
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
