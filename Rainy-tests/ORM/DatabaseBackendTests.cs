using System;
using NUnit.Framework;
using Rainy.Db.Config;

namespace Rainy.Db
{

	[TestFixture]
	public class DatabaseBackendTests : DbTestsBase
	{
		Rainy.Interfaces.CredentialsVerifier auth = (user,pass) => { return true; };

		[Test]
		public void ReadWriteManifest ()
		{
			var conf = new SqliteConfig { File = "/tmp/rainy-test-data/rainy-test.db" };
			var data_backend = new DatabaseBackend (auth);

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
