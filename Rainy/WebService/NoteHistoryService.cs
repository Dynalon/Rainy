using DTO = Tomboy.Sync.Web.DTO;
using ServiceStack.OrmLite;

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
			var resp = new NoteHistoryResponse ();

			using (var db = connFactory.OpenDbConnection ()) {
			}
			return null;
		}
	}
}
