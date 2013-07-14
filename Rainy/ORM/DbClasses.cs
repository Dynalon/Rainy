using Tomboy.Sync.Web.DTO;
using ServiceStack.DataAnnotations;
using Tomboy.Sync;
using DevDefined.OAuth.Storage.Basic;
using Rainy.UserManagement;

namespace Rainy.Db
{
	public class DBNote : DTONote 
	{
		[PrimaryKey]
		// Guid is not feasible for primary key as on an account move as user
		// might duplicate notes across different accounts
		public string CompoundPrimaryKey {
			get {
				return Username + "_" + Guid;
			}
		}

		public new string Guid { get; set; }
	
		// to associate a note to a username
		public string Username { get; set; }

		public bool IsEncypted { get; set; }
		public string EncryptedKey { get; set; }
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

		// whether email verifcation has to take place
		public bool IsVerified { get; set; } 
		// the verification key 
		public string VerifySecret { get; set; }

		public bool IsActivated { get; set; }

		public DBUser ()
		{
			Manifest = new SyncManifest ();
		}
	}

	public class DBAccessToken : AccessToken
	{
		[PrimaryKey]
		public new string Token { get; set; }

		// a short name for the device, i.e. "my laptop", or "my phone"
		// will be set by the user 
		public string DeviceName { get; set; }

		// the TokenKey encrypts the master_key; the so encrypted master_key
		// is sent back as access token to the user
		public string TokenKey { get; set; }
	}

	public class DBRequestToken : RequestToken
	{
		[PrimaryKey]
		public new string Token { get; set; }
	}

	public static class DbClassConverter
	{
		public static DBNote ToDBNote (this DTONote dto, DBUser user)
		{
			// ServiceStack's .PopulateWith is for some reasons
			// ORDERS of magnitudes slower than manually copying
			// TODO evaluate PopulateWith performance / bottleneck
			// or other mappers like ValueInjecter

			var db = new DBNote ();

			db.Guid = dto.Guid;
			db.Title = dto.Title;
			db.Text = dto.Text;
			db.Tags = dto.Tags;

			// dates
			db.ChangeDate = dto.ChangeDate;
			db.MetadataChangeDate = dto.MetadataChangeDate;
			db.CreateDate = dto.CreateDate;

			db.OpenOnStartup = dto.OpenOnStartup;
			db.Pinned = dto.Pinned;

			db.Username = user.Username;

			return db;
		}

		public static DTONote ToDTONote (this DBNote db)
		{
			var dto = new DTONote ();
			
			dto.Guid = db.Guid;
			dto.Title = db.Title;
			dto.Text = db.Text;
			dto.Tags = db.Tags;
			
			// dates
			dto.ChangeDate = db.ChangeDate;
			dto.MetadataChangeDate = db.MetadataChangeDate;
			dto.CreateDate = db.CreateDate;
			
			dto.OpenOnStartup = db.OpenOnStartup;
			dto.Pinned = db.Pinned;

			return dto;
		}
	}
}
