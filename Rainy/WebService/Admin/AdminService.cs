using System;
using Rainy.Db;
using ServiceStack.OrmLite;
using ServiceStack.Common;
using ServiceStack.Common.Web;
using System.Net;
using Rainy.UserManagement;
using System.Linq;
using Rainy.WebService.Admin;
using Rainy.ErrorHandling;
using Rainy.Crypto;
using System.Collections.Generic;
using Tomboy.Db;

namespace Rainy.WebService.Management.Admin
{
	public class UserService : ServiceBase
	{
		private IDbConnectionFactory connFactory;
		public UserService (IDbConnectionFactory factory) : base ()
		{
			connFactory = factory;
		}

		// gets a list of all users
		public object Get (AllUserRequest req)
		{
			List<DTOUser> all_user;
			using (var conn = connFactory.OpenDbConnection ()) {
				all_user = conn.Select<DBUser> ().ToList<DTOUser> ();
			}
			return all_user;
		}

		public DTOUser Get (UserRequest req)
		{
			DBUser found_user;

			using (var conn = connFactory.OpenDbConnection ()) {
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
			using (var conn = connFactory.OpenDbConnection ()) {
				var stored_user = conn.FirstOrDefault<DBUser>("Username = {0}", updated_user.Username);

				if (stored_user == null) {
					// user did not exist, can't update
					return new HttpResult {
						Status = 404,
						StatusDescription = "User " + updated_user.Username + " was not found," +
							" and can't be updated. Try using HTTP POST to create a new user"
					};
				}

				// TODO automapping
				stored_user.IsActivated = updated_user.IsActivated;
				stored_user.IsVerified = updated_user.IsVerified;
				stored_user.AdditionalData = updated_user.AdditionalData;
				stored_user.EmailAddress = updated_user.EmailAddress;

				if (updated_user.Password != "") {
					throw new NotImplementedException ("Password changing is not possible due to encryption!");
				}

				conn.Update<DBUser> (stored_user, u => u.Username == updated_user.Username);
			}
			Logger.DebugFormat ("updating user information for user {0}", updated_user.Username);

			// do not return the password over the wire
			updated_user.Password = "";
			return new HttpResult (updated_user) {
				StatusCode = System.Net.HttpStatusCode.OK,
				StatusDescription = "Successfully updated user " + updated_user.Username
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
				throw new InvalidRequestDtoException { ErrorMessage = "Username was empty" };

			if (string.IsNullOrEmpty (user.Password))
				throw new InvalidRequestDtoException { ErrorMessage = "Password was empty" };

			if (string.IsNullOrEmpty (user.EmailAddress))
				throw new InvalidRequestDtoException { ErrorMessage = "Emailaddress was empty" };
			
			// TODO move into RequestFilter
			if (! (user.Username.IsOnlySafeChars ()
			    && user.Password.IsOnlySafeChars ()
				&& user.EmailAddress.Replace ("@", "").IsOnlySafeChars ())) {

				throw new ValidationException { ErrorMessage = "found unsafe/unallowed characters" };
			}

			// TODO move into RequestFilter
			// lowercase the username
			new_user.Username = new_user.Username.ToLower ();

			// TODO move into API
			new_user.CreateCryptoFields (user.Password);

			using (var conn = connFactory.OpenDbConnection ()) {
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
			using (var conn = connFactory.OpenDbConnection ()) {
				using (var trans = conn.BeginTransaction ()) {

					try {
						conn.Delete<DBUser> (u => u.Username == user.Username);
						conn.Delete<DBNote> (n => n.Username == user.Username);
						conn.Delete<DBAccessToken> (t => t.UserName == user.Username);
						conn.Delete<DBArchivedNote> (an => an.Username == user.Username);
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