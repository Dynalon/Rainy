using NUnit.Framework;

using Rainy.UserManagement;
using Rainy.WebService.Admin;
using ServiceStack.ServiceClient.Web;
using Rainy.Tests;


namespace Rainy.Tests
{

	[TestFixture]
	public class AdminServiceTests : SampleServerTestBase
	{
		[Test]
		public void AddUser ()
		{
			var user = new DTOUser ();
			user.Username = "michael";
			user.EmailAddress = "michael@knight.com";
			user.AdditionalData = "Some more info about Michael";

			var client = GetServiceClient ();
			client.Post<UserRequest> ("/api/admin/user/", user);

			var resp = client.Get<DTOUser> ("/api/admin/user/michael");

			Assert.AreEqual (user.Username, resp.Username);
			Assert.AreEqual (user.EmailAddress, resp.EmailAddress);
			Assert.AreEqual (user.AdditionalData, resp.AdditionalData);

		}

		[Test]
		public void GetUserList ()
		{
			var client = GetServiceClient ();
			var resp = client.Get<DTOUser> ("/api/admin/alluser/");
		}
	}
}

