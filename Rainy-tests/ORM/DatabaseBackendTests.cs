using System;
using System.Data;
using NUnit.Framework;
using Tomboy;
using System.Collections.Generic;
using System.Linq;
using Rainy.OAuth;

namespace Rainy.Db
{

	[TestFixture]
	public class DatabaseBackendTests : DbTestsBase
	{
		OAuthAuthenticator auth = (user,pass) => { return true; };

		[Test]
		public void ReadWriteManifest ()
		{
			var data_backend = new DatabaseBackend ("/tmp/rainy-test-data/rainy-test.db", auth, reset: true);

			var server_id = Guid.NewGuid ().ToString ();
			using (var repo = data_backend.GetNoteRepository ("johndoe")) {
				repo.Manifest.LastSyncRevision = 123;
				repo.Manifest.ServerId = server_id;
			}

			// check the manifest got saved
			using (var repo = data_backend.GetNoteRepository ("johndoe")) {
				Assert.AreEqual (123, repo.Manifest.LastSyncRevision);
				Assert.AreEqual (server_id, repo.Manifest.ServerId);
			}
		}
	}
}
