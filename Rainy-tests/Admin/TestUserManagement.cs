using System;
using NUnit.Framework;
using Rainy.UserManagement;
using Rainy.WebService.Admin;
using ServiceStack.ServiceClient.Web;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using Rainy.WebService.Management;

namespace Rainy.Tests.Management
{
	[TestFixture()]
	public class TestUserManagement : TestBase
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
		public override void SetUp ()
		{
			base.SetUp ();

			adminClient = GetAdminServiceClient ();

			// add some sample users to the server
			var client = GetAdminServiceClient ();
			foreach (DTOUser user in GetSampleUser ()) {
				client.Post<UserRequest> ("/api/admin/user/", user);
			}
		}

		[Test]
		[ExpectedException(typeof(WebServiceException),ExpectedMessage="Unauthorized")]
		public void UnauthorizedAccessFails ()
		{
			var client = GetServiceClient ();
			try {
				client.Get<DTOUser[]> ("/api/admin/alluser/");
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
			user.AdditionalData = "Some more info about Michael";
			
			adminClient.Post<UserRequest> ("/api/admin/user/", user);
			
			var resp = adminClient.Get<DTOUser[]> ("/api/admin/user/michael");

			Assert.AreEqual (1, resp.Length);
			Assert.AreEqual (user.Username, resp[0].Username);
			Assert.AreEqual (user.EmailAddress, resp[0].EmailAddress);
			Assert.AreEqual (user.AdditionalData, resp[0].AdditionalData);
		}

		[Test]
		public void DeleteUser ()
		{
			adminClient.Delete<UserRequest> ("/api/admin/user/johndoe");

			// make sure johndoe is not in the list of our users
			var allusers = adminClient.Get<DTOUser[]> ("/api/admin/alluser/");

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

			adminClient.Put<UserRequest> ("/api/admin/user/", user);

			var all_users = adminClient.Get<DTOUser[]> ("/api/admin/alluser/");

			var johndoe = all_users.First (u => u.Username == "johndoe");
			Assert.AreEqual (user.Username, johndoe.Username);
			Assert.AreEqual (user.Password, johndoe.Password);
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
			adminClient.Put<DTOUser> ("/api/admin/user/", user);
		}
	}
}

