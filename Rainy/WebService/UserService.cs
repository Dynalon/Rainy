using System;
using System.Net;
using DTO = Tomboy.Sync.Web.DTO;

namespace Rainy.WebService
{

	public class UserService : RainyServiceBase
	{
		public object Get (UserRequest request)
		{
			var u = new DTO.UserResponse ();
			try {
				var baseUri = ((HttpListenerRequest)this.Request.OriginalRequest).Url;
				string baseUrl = baseUri.Scheme + "://" + baseUri.Authority + "/";

				u.Username = request.Username;
				u.Firstname = "Not";
				u.Lastname = "Important";

				u.NotesRef = new DTO.ContentRef () {
				ApiRef = baseUrl + "/api/1.0/" + request.Username + "/notes",
				Href = baseUrl + "/api/1.0/" + request.Username + "/notes"
			};
				using (var note_repo = GetNotes (request.Username)) {
					u.LatestSyncRevision = note_repo.Manifest.LastSyncRevision;
					u.CurrentSyncGuid = note_repo.Manifest.ServerId;
				}
			} catch (Exception e) {
				Logger.Debug ("CAUGHT EXCEPTION: " + e.Message);
				throw;
			}
			return u;
		}
	}
	
}
