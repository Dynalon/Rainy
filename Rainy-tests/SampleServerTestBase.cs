using NUnit.Framework;

using System.Linq;
using ServiceStack.ServiceClient.Web;
using Rainy.UserManagement;
using System.Collections.Generic;
using Tomboy.Sync.DTO;
using Tomboy.Sync;
using Tomboy.Sync.Web;
using Tomboy.Sync.Filesystem;
using Tomboy;

namespace Rainy.Tests
{
	/// <summary>
	/// provides a testable server with some users + notes already added
	/// </summary>

	public abstract class SampleServerTestBase : TestBase
	{
		protected List<DTOUser> sampleUser = new List<DTOUser> ();
		protected Dictionary<string, List<DTONote>> sampleNotes = new Dictionary<string, List<DTONote>> ();

		[SetUp]
		public override void SetUp ()
		{
			base.SetUp ();
			DTOUser user;
			List<DTONote> sample_notes;
			JsonServiceClient authed_client, client = GetAdminServiceClient ();

			user = new DTOUser() {
				Username = "johndoe",
				Password = "foobar",
				AdditionalData = "Its just john"
			};
			client.Post<DTOUser> ("/api/admin/user/", user);
			sampleUser.Add (user);

			// add sample notes
			sample_notes = AbstractSyncServerTests.GetSomeSampleNotes ()
				.Select (n => n.ToDTONote ()).ToList ();

			var syncServer = new WebSyncServer (testServer.RainyListenUrl, testServer.GetAccessToken ());

			var storage = new DiskStorage ();
			var tmpPath = "/tmp/sync1";
			storage.SetPath (tmpPath);
			var engine = new Engine (storage);
			var syncClient = new FilesystemSyncClient (engine, new SyncManifest ());

			var syncManager = new Tomboy.Sync.SyncManager (syncClient, syncServer);
			syncManager.DoSync ();

			sampleNotes[user.Username] = sample_notes;


			user = new DTOUser() {
				Username = "janedoe",
				Password = "barfoos",
				AdditionalData = "Jane, Johns wife"
			};
			client.Post<DTOUser> ("/api/admin/user/", user);
			sampleUser.Add (user);
			sampleNotes[user.Username] = AbstractSyncServerTests.GetSomeSampleNotes ()
				.Select (n => n.ToDTONote ()).ToList ();

			// add sample user data

		}
		[TearDown]
		public override void TearDown ()
		{
			base.TearDown ();
		}
	}
	
}
