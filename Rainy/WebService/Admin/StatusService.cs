using ServiceStack.ServiceHost;
using Rainy.Db;
using ServiceStack.OrmLite;
using System;
using ServiceStack.Common.Web;
using Tomboy.Db;

namespace Rainy.WebService.Admin
{
	[Route("/api/admin/status/","GET, OPTIONS",
	       Summary = "Get status information about the server.")]
	[AdminPasswordRequired]
	public class StatusRequest : IReturn<Status>
	{
	}

	public class StatusService : ServiceBase {

		public StatusService (IDbConnectionFactory fac) : base (fac)
		{
		}

		public Status Get (StatusRequest req)
		{
			var s = new Status ();
			s.Uptime = MainClass.Uptime;
			s.NumberOfRequests = MainClass.ServedRequests;

			// determine number of users
			using (var conn = connFactory.OpenDbConnection ()) {
				s.NumberOfUser = (int)conn.Count<DBUser> ();
				s.TotalNumberOfNotes = (int)conn.Count<DBNote> ();

				if (s.NumberOfUser > 0)
					s.AverageNotesPerUser = (float)s.TotalNumberOfNotes / (float)s.NumberOfUser; 
			};
			return s;
		}
	}

	public class Status
	{
		public DateTime Uptime { get; set; }
		public int NumberOfUser { get; set; }
		public long NumberOfRequests { get; set; }
		public int TotalNumberOfNotes { get; set; }
		public float AverageNotesPerUser { get; set; }
	}
}