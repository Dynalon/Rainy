using System;
using Rainy.Db;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceHost;
using ServiceStack.OrmLite;
using ServiceStack.Common;
using ServiceStack.Text;

namespace Rainy.WebService.Admin
{
	// TODO only a logged in admin
	// should be able to access
	[Route("/admin/user/{Username}","GET")]
	public class AdminUserRequest : IReturn<DBUser>
	{
		public string Username { get; set; }
	}

	[Route("/admin/user/","POST,PUT")]
	public class AdminAddOrUpdateUserRequest : DBUser, IReturn<DBUser>
	{
	}

	public class AdminUserService : RainyServiceBase
	{
		public AdminUserService () : base ()
		{
		}
		public object Get (AdminUserRequest req)
		{
			DBUser user;
			using (var conn = DbConfig.GetConnection ()) {
				user = conn.FirstOrDefault<DBUser> ("Username = {0}", req.Username);
			}
			if (user == null) throw new Exception ("User not found!");
			return user;
		}

		public object Post (AdminAddOrUpdateUserRequest user)
		{
			var new_user = new DBUser ();
			new_user.PopulateWith (user);
			user.Password ="blubb";
			user.EmailAddress = "bla@blubb.com";
			user.PrintDump ();

			using (var conn = DbConfig.GetConnection ()) {
				using (var trans = conn.BeginTransaction ()) {
					var stored_user = conn.FirstOrDefault<DBUser>("Username = {0}", new_user.Username);

					if (stored_user != null && new_user.Password == "") {
						// user exists, but password was not updated so use the old password
						// TODO hashing
						new_user.Password = stored_user.Password;
					} else {
						// we can delete the user as we have all data for a reinsert
						conn.Delete<DBUser> ("Username = {0}", new_user.Username);
					}

					conn.Insert<DBUser> (new_user);
					trans.Commit ();
				}
			}
			Logger.DebugFormat ("inserting/updating user information for user {0}", new_user.Username);
			new_user.PrintDump ();
			// do not return the password over the wire
			new_user.Password = "";
			return new_user;
		}

		public object Put (AdminAddOrUpdateUserRequest user)
		{
			return Post (user);
		}
	}
}