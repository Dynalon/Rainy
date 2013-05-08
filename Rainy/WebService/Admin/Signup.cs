using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using ServiceStack.Common;
using ServiceStack.Common.Web;
using ServiceStack.OrmLite;
using ServiceStack.ServiceClient.Web;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.Cors;
using Rainy.Db;
using Rainy.UserManagement;
using Rainy.WebService.Admin;
using Rainy.WebService.Management;


namespace Rainy.WebService.Signup
{
	public class SignupService : RainyServiceBase
	{
		public SignupService () : base ()
		{
		}

		public object Put (UpdateUserRequest req)
		{
			if (!string.IsNullOrEmpty(req.AdditionalData))
				throw new WebServiceException ("Setting of AdditionalData not allowed");

			throw new NotImplementedException();
		}

		public object Post (SignupUserRequest req)
		{
			req.AdditionalData = "";
			req.Username = req.Username.ToLower ();

			// assert password is safe enough
			if (!req.Password.IsSafeAsPassword ())
				throw new WebServiceException ("Password is unsafe");

			// assert username is not already taken
			using (var db = DbConfig.GetConnection ()) {
				var user = db.FirstOrDefault<DBUser> (u => u.Username == req.Username);
				if (user != null)
					throw new WebServiceException ("A user by that name already exists");
			}

			// assert email is not already registered
			using (var db = DbConfig.GetConnection ()) {
				var user = db.FirstOrDefault<DBUser> (u => u.EmailAddress == req.EmailAddress);
				if (user != null)
					throw new WebServiceException ("The emailaddress is already registered");
			}

			// assert all required fields are filled

			var db_user = new DBUser ();
			db_user.PopulateWith (req);

			db_user.IsActivated = false;
			db_user.IsVerified = false;

			var random_bytes = Encoding.UTF8.GetBytes (Path.GetRandomFileName ());
			HashAlgorithm algo = SHA1.Create ();
			string hash = BitConverter.ToString (algo.ComputeHash (random_bytes));
			db_user.VerifySecret = hash.Replace("-", "").ToLower ();

			// write user to db
			using (var db = DbConfig.GetConnection ()) {
				db.Insert<DBUser> (db_user);
			}

			return new HttpResult () {
				StatusCode = HttpStatusCode.OK
			};

		}

		public object Get (VerifyUserRequest req)
		{
			// get user for the activation key
			using (var db = DbConfig.GetConnection ()) {
				var user = db.First<DBUser> (u => u.VerifySecret == req.VerifySecret);

				if (user == null) return new HttpResult () {
					StatusCode = HttpStatusCode.NotFound
				};

				user.IsVerified = true;
				user.VerifySecret = "";
				db.Save (user);
			}
			return new HttpResult () {
				StatusCode = HttpStatusCode.OK
			};
		}

		public object Post (ActivateUserRequest req)
		{
			using (var db = DbConfig.GetConnection ()) {
				var user = db.First<DBUser> (u => u.Username == req.Username);
				user.IsActivated = true;
				db.Save (user);
			}
			return new HttpResult () {
				StatusCode = HttpStatusCode.Created
			};
		}

		public object Get (PendingActivationsRequest req)
		{
			using (var db = DbConfig.GetConnection ()) {
				var users = db.Select<DBUser> (u => u.IsActivated == false);
				return users.ToArray<DTOUser>();
			}
		}

	}
}
