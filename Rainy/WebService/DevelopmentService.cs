using System;
using Rainy.Interfaces;

namespace Rainy.WebService
{
	public class DevelopmentService : RainyNoteServiceBase
	{
		public DevelopmentService (IDataBackend backend) : base (backend)
		{
		}
		public void Get (ClearUserNotesRequest req)
		{
			this.dataBackend.ClearNotes (this.requestingUser);
		}
	}
}

