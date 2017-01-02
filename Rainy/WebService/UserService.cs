using System;
using System.Net;
using DTO = Tomboy.Sync.Web.DTO;
using Rainy.Interfaces;
using Rainy.WebService.OAuth;
using ServiceStack.ServiceHost;
using ServiceStack.OrmLite;

namespace Rainy.WebService
{

	public class UserService : RainyNoteServiceBase
	{
		public UserService (IDataBackend backend) : base (backend)
		{
		}
		public UserService (IDataBackend backend, IDbConnectionFactory factory) : base (factory, backend)
		{
		}

		public object Get (UserRequest request)
		{
			var u = new DTO.UserResponse ();
			string baseUrl = ((HttpListenerRequest)this.Request.OriginalRequest).GetBaseUrl ();

			u.Username = request.Username;
			u.Firstname = "Not";
			u.Lastname = "Important";

			u.NotesRef = new DTO.ContentRef () {
				ApiRef = baseUrl + "/api/1.0/" + request.Username + "/notes/",
				Href = baseUrl + "/api/1.0/" + request.Username + "/notes/"
			};

			using (var note_repo = GetNotes ()) {
				u.LatestSyncRevision = note_repo.Manifest.LastSyncRevision;
				u.CurrentSyncGuid = note_repo.Manifest.ServerId;
			}
			return u;
		}
	}
	
}
