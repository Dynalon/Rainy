using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.Common;
using ServiceStack.ServiceHost;
using Tomboy;
using System.Net;

namespace Rainy.WebService
{
	public class ApiService : RainyServiceBase
	{
		public ApiService () : base ()
		{
		}
		public object Get (ApiRequest request)
		{
			string username = request.Username;
			string password = request.Password;

			Logger.Debug ("ApiRequest received");
			var response = new Tomboy.Sync.DTO.ApiResponse ();
			var baseUri = ((HttpListenerRequest)this.Request.OriginalRequest).Url;
			string baseUrl = baseUri.Scheme + "://" + baseUri.Authority + "/";

			response.UserRef = new Tomboy.Sync.DTO.ContentRef () {
				ApiRef = baseUrl + "api/1.0/" + username,
				Href = baseUrl + username
			};

			response.ApiVersion = "1.0";
			string oauthBaseUrl = baseUrl + "oauth/";
			response.OAuthRequestTokenUrl = oauthBaseUrl + "request_token";
			response.OAuthAccessTokenUrl = oauthBaseUrl + "access_token";
			// HACK we hardencode the username / password pair into the authorize step
			response.OAuthAuthorizeUrl = oauthBaseUrl + "authorize/" + username + "/" + password + "/";

			return response;
		}
	}

	public class UserService : RainyServiceBase
	{
		public object Get (UserRequest request)
		{
			var baseUri = ((HttpListenerRequest)this.Request.OriginalRequest).Url;
			string baseUrl = baseUri.Scheme + "://" + baseUri.Authority + "/";

			var u = new Tomboy.Sync.DTO.UserResponse ();
			u.Username = request.Username;
			u.Firstname = "Not";
			u.Lastname = "Important";

			u.NotesRef = new Tomboy.Sync.DTO.ContentRef () {
				ApiRef = baseUrl + "/api/1.0/" + request.Username + "/notes",
				Href = baseUrl + "/api/1.0/" + request.Username + "/notes"
			};
			using (var note_repo = GetNotes (request.Username)) {
				u.LatestSyncRevision = note_repo.Manifest.LatestSyncRevision;
				u.CurrentSyncGuid = note_repo.Manifest.CurrentSyncGuid;
			}

			return u;
		}
	}

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
			return_notes.LatestSyncRevision = note_repo.Manifest.LatestSyncRevision;

			return return_notes;
		}

		// webservice method: HTTP GET request
		public object Get (GetNotesRequest request)
		{
			using (var note_repo = GetNotes (request.Username)) {
				var notes = GetStoredNotes (note_repo);

				string since = Request.GetParam ("since");

				// if no since is given, return all notes
				if (string.IsNullOrEmpty (since))
					return notes;

				long since_revision = long.Parse (since);

				// select only those notes that changed since last sync
				// which means, only those notes that have a HIGHER revision as "since"
				var changed_notes = notes.Notes.Where (n => {
					if (note_repo.Manifest.NoteRevisions.Keys.Contains (n.Guid)) {
						if (note_repo.Manifest.NoteRevisions [n.Guid] > since_revision)
							return true;
					}
					return false;
				});

				notes.Notes = changed_notes.ToList ();

				return notes;
			}
		}
		public object Post (PutNotesRequest request)
		{
			return Put (request);
		}
		public object Put (PutNotesRequest request)
		{
			using (var note_repo = GetNotes (request.Username)) {

				// constraint taken from snowy source code at http://git.gnome.org/browse/snowy/tree/api/handlers.py:143
				var new_sync_rev = note_repo.Manifest.LatestSyncRevision + 1;

				if (request.LatestSyncRevision.HasValue) {
					new_sync_rev = request.LatestSyncRevision.Value;
				}

				if (new_sync_rev != note_repo.Manifest.LatestSyncRevision + 1)
					throw new Exception ("Sync revisions differ by more than one, sth went wrong");

				foreach (var dto_note in request.Notes) {
					var note = new Note ("note://tomboy/" + dto_note.Guid);
					// map from the DTO 
					note.PopulateWith (dto_note);

					if (dto_note.Command == "delete") {
						note_repo.Engine.DeleteNote (note);
					} else {
						// track the revision of the note
						note_repo.Manifest.NoteRevisions [dto_note.Guid] = (int)new_sync_rev;
						note_repo.Engine.SaveNote (note);
					}
				}


				// only update the sync revision if changes were sent
				if (request.Notes.Count > 0)
					note_repo.Manifest.LatestSyncRevision = new_sync_rev;

				var notes_to_return = NotesService.GetStoredNotes (note_repo);
				notes_to_return.LatestSyncRevision = new_sync_rev;
				return notes_to_return;
			}
		}
	}
}
