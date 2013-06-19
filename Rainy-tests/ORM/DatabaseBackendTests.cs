using System;
using NUnit.Framework;
using Rainy.Db.Config;
using Rainy.Interfaces;

namespace Rainy.Db
{
	public class DummyAuthenticator : IAuthenticator
	{
		public bool VerifyCredentials (string username, string password)
		{
			return true;
		}
	}
	public class DummyAdminAuthenticator : IAdminAuthenticator
	{
		string Password;
		public DummyAdminAuthenticator ()
		{
		}
		public DummyAdminAuthenticator (string pass)
		{
			Password = pass;
		}
		public bool VerifyAdminPassword (string password)
		{
			if (string.IsNullOrEmpty (Password))
				return true;
			else return Password == password;
		}
	}

	[TestFixture]
	public class DatabaseBackendTests : DbTestsBase
	{

		[Test]
		public void ReadWriteManifest ()
		{
			var conf = new SqliteConfig { File = "/tmp/rainy-test-data/rainy-test.db" };
			var data_backend = new DatabaseBackend (factory, new DummyAuthenticator ());

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
