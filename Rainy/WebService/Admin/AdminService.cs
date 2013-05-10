using System;
using Rainy.Db;
using ServiceStack.ServiceHost;
using ServiceStack.OrmLite;
using ServiceStack.Common;
using ServiceStack.Common.Web;
using System.Net;
using System.Collections.Generic;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.Cors;
using Rainy.UserManagement;
using System.Linq;
using Rainy.WebService.Admin;
using Rainy.ErrorHandling;

namespace Rainy.WebService.Management.Admin
{
	public class UserService : RainyNoteServiceBase
	{
		public UserService () : base ()
		{
		}

		// gets a list of all users
		public DTOUser[] Get (AllUserRequest req)
		{
			DTOUser[] all_user;
			using (var conn = DbConfig.GetConnection ()) {
				all_user = conn.Select<DBUser> ().ToArray<DTOUser> ();
				return all_user;
			}

		}

		public DTOUser Get (UserRequest req)
		{
			DBUser found_user;

			using (var conn = DbConfig.GetConnection ()) {
				found_user = conn.FirstOrDefault<DBUser> ("Username = {0}", req.Username);
			}

			if (found_user == null)
				throw new InvalidRequestDtoException (){ErrorMessage = "User not found!"};

			return (DTOUser) found_user;
		}

		// TODO see if we can directly use DBUser
		// update existing user
		public object Put (UserRequest updated_user)
		{
			var user = new DBUser ();
			// TODO make explicit mapping
			user.PopulateWith (updated_user);

			using (var conn = DbConfig.GetConnection ()) {
				var stored_user = conn.FirstOrDefault<DBUser>("Username = {0}", user.Username);

				if (stored_user == null) {
					// user did not exist, can't update
					return new HttpResult {
						Status = 404,
						StatusDescription = "User " + user.Username + " was not found," +
							" and can't be updated. Try using HTTP POST to create a new user"
					};
				}

				if (user.Password == "") {
					// password was not sent so use the old password
					// TODO hashing
					user.Password = stored_user.Password;
				}

				conn.Update<DBUser> (user, u => u.Username == user.Username);
			}
			Logger.DebugFormat ("updating user information for user {0}", user.Username);

			// do not return the password over the wire
			user.Password = "";
			return new HttpResult (user) {
				StatusCode = System.Net.HttpStatusCode.OK,
				StatusDescription = "Successfully updated user " + user.Username
			};
		}

		/// <summary>
		/// POST /admin/user
		/// 
		/// creates a new user.
		/// 
		/// returns HTTP Response =>
		/// 	201 Created
		/// 	Location: http://localhost/admin/user/{Username}
		/// </summary>	
		public object Post (UserRequest user)
		{
			var new_user = new DBUser ();
			// TODO explicit mapping
			new_user.PopulateWith (user);

			// TODO move into RequestFilter
			if (string.IsNullOrEmpty (user.Username))
				throw new ArgumentNullException ("user.Username");

			// TODO move into RequestFilter
			if (! (user.Username.IsOnlySafeChars ()
			    && user.Password.IsOnlySafeChars ()
				&& user.EmailAddress.Replace ("@", "").IsOnlySafeChars ())) {

				throw new ArgumentException ("found unsafe/unallowed characters");
			}

			// TODO move into RequestFilter
			// lowercase the username
			new_user.Username = new_user.Username.ToLower ();

			using (var conn = DbConfig.GetConnection ()) {
				var existing_user = conn.FirstOrDefault<DBUser> ("Username = {0}", new_user.Username);
				if (existing_user != null)
					throw new ConflictException (){ErrorMessage = "A user by that name already exists"};

				conn.Insert<DBUser> (new_user);
			}

			return new HttpResult (new_user) {
				StatusCode = HttpStatusCode.Created,
				StatusDescription = "Sucessfully created user " + new_user.Username,
				Headers = {
					{ HttpHeaders.Location, base.Request.AbsoluteUri.CombineWith (new_user.Username) }
				}
			};
		}

		/// <summary>
		/// DELETE /admin/user/{Username}
		/// 
		/// deletes a user.
		/// 
		/// returns HTTP Response =>
		/// 	204 No Content
		/// 	Location: http://localhost/admin/user/
		/// </summary>
		public object Delete (UserRequest user)
		{
			using (var conn = DbConfig.GetConnection ()) {
				using (var trans = conn.BeginTransaction ()) {

					try {
						conn.Delete<DBUser> (u => u.Username == user.Username);
						conn.Delete<DBNote> (n => n.Username == user.Username);
						trans.Commit ();
					} catch (Exception e) {
						Logger.DebugFormat ("error deleting user {0}, msg was: {1}",
					                    user.Username, e.Message);

						return new HttpResult {
							StatusCode = HttpStatusCode.InternalServerError,
							StatusDescription = "Error occured, msg was: " + e.Message
						};
					}
				}
			}

			return new HttpResult {
				StatusCode = HttpStatusCode.NoContent,
				Headers = {
					{ HttpHeaders.Location, this.RequestContext.AbsoluteUri }
				}
			};
		}
	}
}