using System;
using NUnit.Framework;
using ServiceStack.ServiceClient.Web;
using Rainy.UserManagement;
using Rainy.Db;
using ServiceStack.OrmLite;
using Rainy.WebService.Management;
using System.Linq;
using System.Net;
using ServiceStack.WebHost.Endpoints;

namespace Rainy.Tests.Management
{
	[TestFixture()]
	public class TestUserSignup : DbTestsBase
	{
		protected JsonServiceClient client;
		protected JsonServiceClient adminClient;

		protected DTOUser getTestUser ()
		{
			var user = new DTOUser () {
				Username = "someuser",
				FirstName = "John",
				LastName = "Doe",
				EmailAddress = "some@foo.com",
				Password = "Foobar.123"
			};
			return user;
		}

		[SetUp]
		public new void SetUp ()
		{
			client = GetServiceClient ();
			adminClient = GetAdminServiceClient ();
		}

		[Test()]
		public void SignupWorks()
		{
			var user = getTestUser ();
			client.Post<DTOUser> ("/api/user/signup/new/", user);
		}

		[Test]
		public void SignupWithVerifyWorks ()
		{
			var user = getTestUser ();
			client.Post<DTOUser> ("/api/user/signup/new/", user);

			// lookup activation key
			var secret = "";
			using (var db = connFactory.OpenDbConnection ()) {
				var db_user = db.First<DBUser> (u => u.Username == user.Username);
				secret = db_user.VerifySecret;
				client.Get<VerifyUserRequest> ("/api/user/signup/verify/" + secret + "/");
			}
			using (var db = connFactory.OpenDbConnection ()) {
				var db_user = db.First<DBUser> (u => u.Username == user.Username);
				Assert.IsTrue (db_user.IsVerified);
				Assert.IsEmpty (db_user.VerifySecret);
			}
		}
		[Test]
		[ExpectedException(typeof(WebException))]
		public void UnverifiedUserCannotAcquireAccessToken ()
		{
			var user = getTestUser ();
			client.Post<DTOUser> ("/api/user/signup/new/", user);
			testServer.GetAccessToken ();
		}

		[Test]
		[ExpectedException(typeof(WebException))]
		public void UnactivatedUserCannotAcquireAccessToken ()
		{
			var user = getTestUser ();
			client.Post<DTOUser> ("/api/user/signup/new/", user);

			// lookup activation key
			var secret = "";
			using (var db = connFactory.OpenDbConnection ()) {
				var db_user = db.First<DBUser> (u => u.Username == user.Username);
				secret = db_user.VerifySecret;
			}
			client.Get<VerifyUserRequest> ("/api/user/signup/verify/" + secret + "/");
			testServer.GetAccessToken ();
		}


		[Test]
		[ExpectedException(typeof(WebServiceException))]
		public void SignupUserWithUnsafePasswordFails ()
		{
			var user = new DTOUser () {
				Username = "testuser",
				Password = "abc123",
				FirstName = "John",
				LastName = "Doe",
				EmailAddress = "johndoe@foo.com"
			};
			client.Post<DTOUser> ("/api/user/signup/new/", user);
		}

		[Test]
		[ExpectedException(typeof(WebServiceException))]
		public void SignupUsernameTwice ()
		{
			var user = getTestUser ();
			client.Post<DTOUser> ("/api/user/signup/new/", user);
			client.Post<DTOUser> ("/api/user/signup/new/", user);
		}
		[Test]
		[ExpectedException(typeof(WebServiceException))]
		public void SignupUsernameTwiceWithDifferentCasing ()
		{
			var user = getTestUser ();
			user.Username = "someuser";
			client.Post<DTOUser> ("/api/user/signup/new/", user);
			user.Username = "SomEUseR";
			client.Post<DTOUser> ("/api/user/signup/new/", user);
		}

		[Test]
		[ExpectedException(typeof(WebServiceException))]
		public void SignupEmailTwice ()
		{
			var user = getTestUser ();
			client.Post<DTOUser> ("/api/user/signup/new/", user);
			user.Username = "otheruser";
			client.Post<DTOUser> ("/api/user/signup/new/", user);
		}

		[Test]
		public void PendingUserForActivation ()
		{
			var user = getTestUser ();
			client.Post<DTOUser> ("/api/user/signup/new/", user);
			user.Username = "otheruser";
			user.EmailAddress = "other@foo.com";
			client.Post<DTOUser> ("/api/user/signup/new/", user);

			var pending = adminClient.Get<DTOUser[]> ("/api/user/signup/pending/");
//			Assert.AreEqual(2, pending.Length);
			Assert.That (pending.ToList().Where(u => u.Username == "someuser").Count () == 1);
			Assert.That (pending.ToList().Where(u => u.Username == "otheruser").Count () == 1);
		}

		[Test]
		public void PendingUserSuccessfullyActivated ()
		{
			var user = getTestUser ();
			client.Post<DTOUser> ("/api/user/signup/new/", user);
		
			// lookup activation key
			var secret = "";
			using (var db = connFactory.OpenDbConnection ()) {
				var db_user = db.First<DBUser> (u => u.Username == user.Username);
				secret = db_user.VerifySecret;
			}
			client.Get<VerifyUserRequest> ("/api/user/signup/verify/" + secret + "/");

			adminClient.Post<ActivateUserRequest> ("/api/user/signup/activate/" + user.Username + "/", new object());

			using (var db = connFactory.OpenDbConnection ()) {
				var db_user = db.First<DBUser> (u => u.Username == user.Username);
				Assert.IsTrue (db_user.IsActivated);
			}
		}
	}
}

