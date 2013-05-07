using System;
using NUnit.Framework;

namespace Rainy.Db
{

	[TestFixture]
	public class DatabaseBackendTests : DbTestsBase
	{
		Rainy.Interfaces.CredentialsVerifier auth = (user,pass) => { return true; };

		[Test]
		public void ReadWriteManifest ()
		{
			var data_backend = new DatabaseBackend ("/tmp/rainy-test-data/rainy-test.db", auth);

			var server_id = Guid.NewGuid ().ToString ();
			using (var repo = data_backend.GetNoteRepository (testUser.Username)) {
				repo.Manifest.LastSyncRevision = 123;
				repo.Manifest.ServerId = server_id;
			}

			// check the manifest got saved
			using (var repo = data_backend.GetNoteRepository (testUser.Username)) {
				Assert.AreEqual (123, repo.Manifest.LastSyncRevision);
				Assert.AreEqual (server_id, repo.Manifest.ServerId);
			}
		}
	}
}
