using ServiceStack.ServiceHost;
using Rainy.Db;
using ServiceStack.OrmLite;
using System;
using ServiceStack.ServiceInterface.Cors;
using ServiceStack.Common.Web;

namespace Rainy.WebService.Admin
{
	[Route("/api/admin/status/","GET, OPTIONS")]
	[AdminPasswordRequired]
	public class StatusRequest : IReturn<Status>
	{
	}

	public class StatusService : RainyNoteServiceBase
	{
		public StatusService () : base ()
		{
		}

		[EnableCors]
		public HttpResult Options (StatusRequest req)
		{
			return new HttpResult ();
		}
		public Status Get (StatusRequest req)
		{
			var s = new Status ();
			s.Uptime = MainClass.Uptime;
			s.NumberOfRequests = MainClass.ServedRequests;

			// determine number of users
			using (var conn = DbConfig.GetConnection ()) {
				s.NumberOfUser = conn.Scalar<int>("SELECT COUNT(*) FROM DBUser");
				s.TotalNumberOfNotes = conn.Scalar<int>("SELECT COUNT(*) FROM DBNote");
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