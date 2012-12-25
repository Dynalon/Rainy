using Tomboy.Sync.DTO;
using ServiceStack.DataAnnotations;

namespace Rainy.Db
{
	public class DBNote : DTONote 
	{
		[PrimaryKey]
		public new string Guid { get; set; }
	
		// to associate a note to a username
		public string Username { get; set; }
	}
	
	public class DBUser
	{
		[PrimaryKey]
		public string Id { get; set; }

		public string Username { get; set; }
	}

	public static class DbClassConverter
	{
		public static DBNote ToDBNote (this DTONote dto)
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
