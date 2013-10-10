using Tomboy.Sync.Web.DTO;
using ServiceStack.DataAnnotations;
using Tomboy.Sync;
using DevDefined.OAuth.Storage.Basic;
using Rainy.UserManagement;
using Tomboy.Db;

namespace Rainy.Db
{

	public class DBArchivedNote : DBNote
	{
		[PrimaryKey]
		public override string CompoundPrimaryKey {
			get {
				return Username + "_" + Guid + "_" + LastSyncRevision;
			}
		}
	}

	public class DBUser : DTOUser
	{
		[PrimaryKey]
		public override string Username { get; set; }

		public override string Password {
			get { return ""; }
			set { return; }
		}

		public SyncManifest Manifest { get; set; }

		public string PasswordSalt { get; set; }
		public string PasswordHash { get; set; }

		public string MasterKeySalt { get; set; }
		public string EncryptedMasterKey { get; set; }

		// the verification key 
		public string VerifySecret { get; set; }

		public DBUser ()
		{
			Manifest = new SyncManifest ();
		}
	}

	public class DBRequestToken : RequestToken
	{
		[PrimaryKey]
		public new string Token { get; set; }
	}


}
