using NUnit.Framework;
using System;
using Rainy.Tests;
using ServiceStack.ServiceClient.Web;
using System.Collections.Generic;
using Rainy.WebService.Management;
using Rainy.UserManagement;

namespace Rainy.Tests
{
	[TestFixture()]
	public class DemoServerAccess : TestBase
	{
		[SetUp]
		public override void SetUp ()
		{
			base.SetUp ();
			// uncomment to populate demoserver (do not commit password into master!)
//			this.listenUrl = "http://rainy-demoserver.latecrew.de";
//			this.adminPass = "";

			testServer.Start ();
		}

		[Test]
		public void CreateDemoAccounts ()
		{
			var users = @"testuser    testpass
					aiden    QSmCmH
					alexander    fcOYGZ
					alexis    XwG4Hy
					allison    Fm84Pz
					alyssa    msS0yK
					amelia    MmFTkh
					andrew    dhFHJu
					anna    jMmkjo
					anthony    sbck8m
					ashley    NkPu9U
					aubrey    Q0JkFr
					audrey    WNmaru
					ava    vxpGuz
					avery    fQZPjm
					benjamin    QlRHFr
					brandon    9EQUYz
					brayden    TERA4w
					brianna    480eZe
					brooklyn    bl3cqZ
					caleb    b9IIS3
					camila    jb4QR5
					carter    Og5630
					charlotte    SM9yUr
					chloe    xy0gfH
					christian    JFpfFr
					christopher    gXEuhD
					claire    Tks9GN
					daniel    7djYGV
					david    uT4kWZ
					dylan    lPinW0
					elijah    yW9YQY
					elizabeth    VNquj0
					ella    vPMMfj
					emily    Y5LLgf
					emma    oH7Lda
					ethan    UdGHfc
					evan    rnKrac
					evelyn    DTjkV1
					gabriel    3qRnkp
					gabriella    IujWTS
					gavin    bFDyb5
					grace    zEHd9O
					hailey    jgOwtp
					hannah    SN5OPs
					isaac    WS71tv
					isabella    ghrF6b
					isaiah    9cUEET
					jack    ZINEES
					jackson    x7kslI
					jacob    hhx9q0";

			var userdata = users.Split (new char[] { '\n' });
			var userlist = new Dictionary<string, string> ();
			foreach (var user in userdata) {
				var credentials = user.Split (new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
				userlist.Add (credentials [0], credentials [1]);
				Console.WriteLine ("{0}\t{1}", credentials[0], credentials[1]);
			}

			var adminClient = this.GetAdminServiceClient ();

			foreach (var kvp in userlist) {
				var username = kvp.Key;
				var password = kvp.Value;
		
				var user = new DTOUser { Username = username, Password = password };
				user.EmailAddress = username + "@example.com";
				user.IsActivated = true;
				user.IsVerified = true;

				var user_url = new UserRequest ().ToUrl ("POST");
				adminClient.Post<UserRequest> (user_url, user);

				// get the user and verify
				var user_get_url = new UserRequest () { Username = username }.ToUrl("GET");
				var resp = adminClient.Get<DTOUser[]> (user_get_url);
				Assert.AreEqual (username, resp[0].Username);
			}
		}
	}
}
