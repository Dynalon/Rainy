using System;
using System.Net;
using DTO = Tomboy.Sync.Web.DTO;
using Rainy.Interfaces;
using Rainy.WebService.OAuth;
using ServiceStack.ServiceHost;

namespace Rainy.WebService
{

	public class UserService : RainyNoteServiceBase
	{
		public UserService (IDataBackend backend) : base (backend)
		{
		}

		public object Get (UserRequest request)
		{
			var u = new DTO.UserResponse ();
			var baseUri = ((HttpListenerRequest)this.Request.OriginalRequest).Url;
			string baseUrl = baseUri.Scheme + "://" + baseUri.Authority + "/";

			u.Username = request.Username;
			u.Firstname = "Not";
			u.Lastname = "Important";

			u.NotesRef = new DTO.ContentRef () {
				ApiRef = baseUrl + "/api/1.0/" + request.Username + "/notes",
				Href = baseUrl + "/api/1.0/" + request.Username + "/notes"
			};

			using (var note_repo = GetNotes ()) {
				u.LatestSyncRevision = note_repo.Manifest.LastSyncRevision;
				u.CurrentSyncGuid = note_repo.Manifest.ServerId;
			}
			return u;
		}
	}
	
}
