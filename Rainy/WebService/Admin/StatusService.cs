using ServiceStack.ServiceHost;
using Rainy.Db;
using ServiceStack.OrmLite;
using System;

namespace Rainy.WebService.Admin
{
	[Route("/admin/status/","GET")]
	public class StatusRequest : IReturn<Status>
	{
	}
	public class StatusService : RainyServiceBase
	{
		public StatusService () : base ()
		{
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
				s.AverageNotesPerUser = s.TotalNumberOfNotes / s.NumberOfUser; 
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