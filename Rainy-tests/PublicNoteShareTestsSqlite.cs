using System;
using System.Linq;
using System.Net;
using NUnit.Framework;
using Rainy.Db;
using Rainy.WebService;
using ServiceStack.ServiceClient.Web;
using Tomboy.Sync.Web;

namespace Rainy.Tests.RestApi
{
	public class PublicNoteShareTestsSqlite : Tomboy.Sync.AbstractSyncManagerTestsBase
	{
		protected RainyTestServer testServer;
		protected DBUser testUser;

		[SetUp]
		public new void SetUp ()
		{
			testServer = new RainyTestServer ();
			testServer.ScenarioSqlite ();
			testServer.Start ();

			syncServer = new WebSyncServer (testServer.BaseUri, testServer.GetAccessToken ());
		}

		[TearDown]
		public new void TearDown ()
		{
			testServer.Stop ();
		}

		[Test]
		public void GetShareableUrlWorks ()
		{
			FirstSyncForBothSides ();
			var client = testServer.GetJsonClient ();

			var first_note = clientEngineOne.GetNotes ().Values.First ();
			var url = testServer.ListenUrl + new GetPublicUrlForNote () { Username = RainyTestServer.TEST_USER }.ToUrl ("GET");

			var resp = client.Get<string> (url);

			// fetch the note from that url via simple, unauthed http request
			var wc = new WebClient ();
			var content = wc.DownloadString (resp);

			Assert.Fail ("Not implemented yet");
		}

		protected override void ClearServer (bool reset)
		{
			return;
		}
	}
	
}
