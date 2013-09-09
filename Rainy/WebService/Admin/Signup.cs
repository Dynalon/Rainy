using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using ServiceStack.Common;
using ServiceStack.Common.Web;
using ServiceStack.OrmLite;
using Rainy.Db;
using Rainy.UserManagement;
using Rainy.WebService.Management;
using Rainy.ErrorHandling;
using Rainy.Crypto;


namespace Rainy.WebService.Signup
{
	public class SignupService : ServiceBase
	{
		public SignupService (IDbConnectionFactory factory) : base (factory)
		{
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
			//if (!req.Password.IsSafeAsPassword ())
			//	throw new ValidationException () {ErrorMessage = "Password is unsafe"};

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
			if (JsonConfig.Config.Global.RequireModeration == false)
				db_user.IsActivated = true;

			db_user.IsVerified = true;

			db_user.VerifySecret = Guid.NewGuid ().ToString ().Replace("-", "");

			db_user.CreateCryptoFields (req.Password);
			db_user.Password = "";

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

		public object Get (CheckUsernameRequest req)
		{
			req.Username = req.Username.ToLower ();

			var resp = new CheckUsernameResponse {
				Username = req.Username,
				Available = false
			};

			using (var db = connFactory.OpenDbConnection ()) {
				var user = db.FirstOrDefault<DBUser> (u => u.Username == req.Username);
				if (user == null)
					resp.Available = true;
			}
			return resp;
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
