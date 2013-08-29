using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Rainy.UserManagement;
using Rainy.WebService.Management;
using ServiceStack.ServiceClient.Web;
using Rainy.Tests.Db;

namespace Rainy.Tests.RestApi.Management
{
	[TestFixture()]
	public class TestUserManagement : DbTestsBase
	{
		protected JsonServiceClient adminClient;
		protected DTOUser[] GetSampleUser ()
		{
			var user = new List<DTOUser> ();

			user.Add (new DTOUser () {
				Username = "johndoe",
				Password = "none",
				EmailAddress = "john@doe.com",
				FirstName = "John",
				LastName = "Doe",
				AdditionalData = ""
			});
			user.Add (new DTOUser () {
				Username = "janedoe",
				Password = "none",
				EmailAddress = "jane@doe.com",
				FirstName = "Jane",
				LastName = "Doe",
				AdditionalData = "Jane, John's wife"
			});

			return user.ToArray<DTOUser> ();
		}

		[SetUp]
		public new void SetUp ()
		{
			adminClient = GetAdminServiceClient ();

			// add some sample users to the server
			var client = GetAdminServiceClient ();
			var url = new UserRequest ().ToUrl("POST");
			foreach (DTOUser user in GetSampleUser ()) {
				client.Post<UserRequest> (url, user);
			}
		}

		[Test]
		[ExpectedException(typeof(WebServiceException),ExpectedMessage="Unauthorized.")]
		public void UnauthorizedAccessFails ()
		{
			var alluser_url = new AllUserRequest ().ToUrl("GET");
			var client = GetServiceClient ();
			try {
				client.Get<DTOUser[]> (alluser_url);
			} catch (WebServiceException e) {
				Assert.AreEqual (401, e.StatusCode);
				throw e;
			}
		}

		[Test]
		public void AddNewUser ()
		{
			var user = new DTOUser ();
			user.Username = "michael";
			user.EmailAddress = "michael@knight.com";
			user.Password = "none";
			user.AdditionalData = "Some more info about Michael";
		
			var user_url = new UserRequest ().ToUrl("POST");
			adminClient.Post<UserRequest> (user_url, user);
		
			var user_get_url = new UserRequest () { Username = "michael" }.ToUrl("GET");
			var resp = adminClient.Get<DTOUser[]> (user_get_url);
			
			Assert.AreEqual (1, resp.Length);
			Assert.AreEqual (user.Username, resp[0].Username);
			Assert.AreEqual (user.EmailAddress, resp[0].EmailAddress);
			Assert.AreEqual (user.AdditionalData, resp[0].AdditionalData);

		}

		[Test]
		[ExpectedException(typeof(WebServiceException))]
		public void AddNewUserWithEmptyPasswordFails ()
		{
			var user = new DTOUser ();
			user.Username = "michael";
			user.EmailAddress = "michael@knight.com";
			user.Password = "";
			user.AdditionalData = "Some more info about Michael";

			var user_url = new UserRequest ().ToUrl("POST");
			try {
				adminClient.Post<UserRequest> (user_url, user);
			} catch (WebServiceException e) {
				Assert.AreEqual (400, e.StatusCode);
				throw e;
			}
		}

		[Test]
		public void ChangeUserPassword ()
		{
			var user = new DTOUser ();
			user.Username = "michael";
			user.EmailAddress = "michael@knight.com";
			user.Password = "thisissecret";
			user.AdditionalData = "Some more info about Michael";

			var user_url = new UserRequest ().ToUrl("POST");
			adminClient.Post<UserRequest> (user_url, user);

			user.Password = "thisismynewpassword";
			var update_url = new Rainy.WebService.UserRequest ().ToUrl ("PUT");
			adminClient.Put<UserRequest> (update_url, user);

			// authorization with the old password fails for the user
			Assert.Fail ("TODO: Implement me");

			// TODO: authorization with the new password works
		}

		[Test]
		public void DeleteUser ()
		{
			var user_delete_url = new UserRequest () { Username = "johndoe" }.ToUrl ("DELETE");
			adminClient.Delete<UserRequest> (user_delete_url);

			// make sure johndoe is not in the list of our users
			var alluser_url = new AllUserRequest ().ToUrl ("GET");
			var allusers = adminClient.Get<DTOUser[]> (alluser_url);

			var list_of_johndoes = allusers.Where(u => u.Username == "johndoe").ToArray ();
			Assert.AreEqual (0, list_of_johndoes.Count ());
		}

		[Test]
		public void UpdateUser ()
		{
			var user = new DTOUser () {
				Username = "johndoe",
				Password = "abc123",
				EmailAddress = "some@foo.com",
				AdditionalData = "some text",
				FirstName = "Jane",
				LastName = "Doeson"
			};

			var user_url = new UserRequest ().ToUrl ("PUT");
			adminClient.Put<UserRequest> (user_url, user);

			var all_users_url = new AllUserRequest ().ToUrl ("GET");
			var all_users = adminClient.Get<DTOUser[]> (all_users_url);

			var johndoe = all_users.First (u => u.Username == "johndoe");
			Assert.AreEqual (user.Username, johndoe.Username);
			//password is not returned
			Assert.AreEqual (string.Empty, johndoe.Password);
			Assert.AreEqual (user.EmailAddress, johndoe.EmailAddress);
			Assert.AreEqual (user.AdditionalData, johndoe.AdditionalData);
			Assert.AreEqual (user.FirstName, johndoe.FirstName);
			Assert.AreEqual (user.LastName, johndoe.LastName);

		}

	
		[Test]
		[ExpectedException (typeof(WebServiceException))]
		public void UpdateUserForUnknownUsername ()
		{
			var user = new DTOUser () {
				Username = "foobar"
			};
			var user_url = new UserRequest ().ToUrl ("PUT");
			adminClient.Put<DTOUser> (user_url, user);
		}
	}
}

