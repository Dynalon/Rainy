using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.Common;
using ServiceStack.ServiceHost;
using Tomboy;
using ServiceStack.Text;
using System.Net;
using log4net;
using Rainy.WebService.OAuth;

namespace Rainy.WebService
{
	[Route("/{Username}/{Password}/api/1.0/")]
	public class ApiRequest : IReturn<ApiResponse>
	{
		public string Username { get; set; }
		public string Password { get; set; }
	}

	public class ApiService : RainyServiceBase
	{
		public ApiService () : base ()
		{
		}
		public object Get (ApiRequest request)
		{
			Logger.Debug ("ApiRequest received");
			var response = new ApiResponse ();
			var baseUri = ((HttpListenerRequest)this.Request.OriginalRequest).Url;
			string baseUrl = baseUri.Scheme + "://" + baseUri.Authority + "/";

			response.UserRef = new ContentRef () {
				ApiRef = baseUrl + "api/1.0/" + request.Username,
				Href = baseUrl + request.Username
			};

			string oauthBaseUrl = baseUrl + "oauth/";
			response.OAuthRequestTokenUrl = oauthBaseUrl + "request_token";
			response.OAuthAccessTokenUrl = oauthBaseUrl + "access_token";
			// HACK we hardencode the username / password pair into the authorize step
			response.OAuthAuthorizeUrl = oauthBaseUrl + "authorize/" + request.Username + "/" + request.Password + "/";

			return response;
		}
	}

	[Route("/api/1.0/{Username}/")]
	public class UserRequest : IReturn<UserResponse>
	{
		public string Username { get; set; }
	}

	public class UserService : RainyServiceBase
	{
		public object Get (UserRequest request)
		{
			var baseUri = ((HttpListenerRequest)this.Request.OriginalRequest).Url;
			string baseUrl = baseUri.Scheme + "://" + baseUri.Authority + "/";

			var u = new UserResponse ();
			u.Username = request.Username;
			u.Firstname = "Not";
			u.Lastname = "Important";

			u.NotesRef = new ContentRef () {
				ApiRef = baseUrl + "/api/1.0/" + request.Username + "/notes",
				Href = baseUrl + "/api/1.0/" + request.Username + "/notes"
			};
			using (var note_repo = new NoteRepository (request.Username)) {
				u.LatestSyncRevision = note_repo.LatestSyncRevision;
				u.CurrentSyncGuid = note_repo.CurrentSyncGuid;
			}

			return u;
		}
	}

	[Route("/api/1.0/{Username}/notes", "GET")]
	[OAuthRequiredAttribute]
	public class NotesRequest : IReturn<GetNotesResponse>
	{
		public string Username { get; set; }
	}

	public class NotesService : RainyServiceBase
	{
		protected static GetNotesResponse GetStoredNotes (NoteRepository note_repo)
		{
			var notes = new List<DTONote> ();
			var stored_notes = note_repo.NoteEngine.GetNotes ();
			
			foreach (var kvp in stored_notes) {
				var note = new DTONote ();
				note.PopulateWith (kvp.Value);

				// if we have a sync revision, set it	
				if (note_repo.NoteRevisions.Keys.Contains (note.Guid)) {
					note.LastSyncRevision = note_repo.NoteRevisions [note.Guid];
				}

				notes.Add (note);
			}
			
			var return_notes = new GetNotesResponse ();
			return_notes.Notes = notes;
			return_notes.LatestSyncRevision = note_repo.LatestSyncRevision;

			return return_notes;
		}

		// webservice method: HTTP GET request
		public object Get (NotesRequest request)
		{
			using (var note_repo = new NoteRepository (request.Username)) {
				var notes = GetStoredNotes (note_repo);

				string since = Request.GetParam ("since");

				// if no since is given, return all notes
				if (string.IsNullOrEmpty (since))
					return notes;

				long since_revision = long.Parse (since);

				// select only those notes that changed since last sync
				// which means, only those notes that have a HIGHER revision as "since"
				var changed_notes = notes.Notes.Where (n => {
					if (note_repo.NoteRevisions.Keys.Contains (n.Guid)) {
						if (note_repo.NoteRevisions [n.Guid] > since_revision)
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
			using (var note_repo = new NoteRepository (request.Username)) {

				// constraint taken from snowy source code at http://git.gnome.org/browse/snowy/tree/api/handlers.py:143
				var new_sync_rev = note_repo.LatestSyncRevision + 1;

				if (request.LatestSyncRevision.HasValue) {
					new_sync_rev = request.LatestSyncRevision.Value;
				}

				if (new_sync_rev != note_repo.LatestSyncRevision + 1)
					throw new Exception ("Sync revisions differ by more than one, sth went wrong");

				foreach (var dto_note in request.Notes) {
					var note = new Note ("note://tomboy/" + dto_note.Guid);
					// map from the DTO 
					note.PopulateWith (dto_note);

					if (dto_note.Command == "delete") {
						note_repo.NoteEngine.DeleteNote (note);
					} else {
						// track the revision of the note
						note_repo.NoteRevisions [dto_note.Guid] = (int)new_sync_rev;

						note_repo.NoteEngine.SaveNote (note);
					}
				}

				// only update the sync revision if changes were sent
				if (request.Notes.Count > 0)
					note_repo.LatestSyncRevision = new_sync_rev;

				var notes_to_return = NotesService.GetStoredNotes (note_repo);
				notes_to_return.LatestSyncRevision = new_sync_rev;
				return notes_to_return;
			}
		}
	}
}
