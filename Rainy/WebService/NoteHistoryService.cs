using DTO = Tomboy.Sync.Web.DTO;
using ServiceStack.OrmLite;
using Rainy.Db;
using System.Linq;
using Tomboy.Sync.Web.DTO;

namespace Rainy.WebService
{
	public class NoteHistoryService : OAuthServiceBase
	{
		IDbConnectionFactory connFactory;
		public NoteHistoryService (IDbConnectionFactory factory) : base ()
		{
			connFactory = factory;
		}

		public object Get (GetNoteHistoryRequest request)
		{
			var resp = new NoteHistoryResponse () {
				CurrentRevision = -1,
				Versions = new NoteHistory[] {}
			};

			using (var db = connFactory.OpenDbConnection ()) {
				var revisions = db.First<DBUser> (u => u.Username == requestingUser.Username).Manifest.NoteRevisions;
				if (revisions.ContainsKey (request.Guid)) {
					// note was not deleted in the meantime
					resp.CurrentRevision = revisions[request.Guid];
				}

				var archived_notes = db.Select<DBArchivedNote> (n => n.Username == requestingUser.Username && n.Guid == request.Guid);
				resp.Versions = archived_notes.Select (note => new NoteHistory () { Revision = note.LastSyncRevision, Title = note.Title}).ToArray ();

			}
			return resp;
		}

		public object Get (GetArchivedNoteRequest request)
		{
			DTONote resp;
			using (var db = connFactory.OpenDbConnection ()) {
				var archived_note = db.FirstOrDefault<DBArchivedNote> (an => an.Username == requestingUser.Username && an.Guid == request.Guid);
				if (archived_note != null)
					return archived_note.ToDTONote ();
				else
					throw new Rainy.ErrorHandling.InvalidRequestDtoException ();
			}
		}
	}
}
