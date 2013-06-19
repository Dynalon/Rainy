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
using Rainy.ErrorHandling;


namespace Rainy.WebService.Signup
{
	public class SignupService : RainyServiceBase
	{
		private IDbConnectionFactory connFactory;
		public SignupService (IDbConnectionFactory factory) : base ()
		{
			connFactory = factory;
		}

		public object Put (UpdateUserRequest req)
		{
			if (!string.IsNullOrEmpty(req.AdditionalData))
				throw new InvalidRequestDtoException () {ErrorMessage = "Setting of AdditionalData not allowed"};

			throw new NotImplementedException();
		}

		public object Post (SignupUserRequest req)
		{
			req.AdditionalData = "";
			req.Username = req.Username.ToLower ();

			// assert password is safe enough
			if (!req.Password.IsSafeAsPassword ())
				throw new ValidationException () {ErrorMessage = "Password is unsafe"};

			// assert username is not already taken
			using (var db = connFactory.OpenDbConnection ()) {
				var user = db.FirstOrDefault<DBUser> (u => u.Username == req.Username);
				if (user != null)
					throw new ConflictException () {ErrorMessage = "A user by that name already exists"};

				// assert email is not already registered
				user = db.FirstOrDefault<DBUser> (u => u.EmailAddress == req.EmailAddress);
				if (user != null)
					throw new ConflictException () {ErrorMessage = "The emailaddress is already registered"};
			}

			// assert all required fields are filled

			var db_user = new DBUser ();
			db_user.PopulateWith (req);

			db_user.IsActivated = false;
			db_user.IsVerified = false;

			db_user.VerifySecret = Guid.NewGuid ().ToString ().Replace("-", "");

			// write user to db
			using (var db = connFactory.OpenDbConnection ()) {
				db.Insert<DBUser> (db_user);
			}

			return new HttpResult () {
				StatusCode = HttpStatusCode.OK
			};

		}

		public object Get (VerifyUserRequest req)
		{
			// get user for the activation key
			using (var db = connFactory.OpenDbConnection ()) {
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
			using (var db = connFactory.OpenDbConnection ()) {
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
			using (var db = connFactory.OpenDbConnection ()) {
				var users = db.Select<DBUser> (u => u.IsActivated == false);
				return users.ToArray<DTOUser>();
			}
		}

	}
}
