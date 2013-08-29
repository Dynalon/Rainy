using System;
using System.Linq;
using NUnit.Framework;
using Tomboy;
using Tomboy.Sync.Web.DTO;
using Tomboy.Tags;

namespace Rainy.Tests
{
	[TestFixture]
	public class DtoConversionTests 
	{

		[SetUp]
		public void SetUp ()
		{
		}
		[TearDown]
		public void TearDown ()
		{
		}
		
		[Test]
		public void ConvertUriTests ()
		{
			var tomboy_note = new Note ();
			
			tomboy_note.CreateDate = DateTime.Now;
			tomboy_note.ChangeDate = DateTime.Now;
			tomboy_note.MetadataChangeDate = DateTime.Now;
			
			var dto_note = tomboy_note.ToDTONote ();
			
			Assert.That (!string.IsNullOrEmpty (dto_note.Guid));
			
			Assert.AreEqual (tomboy_note.Guid, dto_note.Guid);
			Assert.That (tomboy_note.Uri.Contains (dto_note.Guid));
			Assert.That (tomboy_note.Uri.Contains (tomboy_note.Guid));
			
			var tomboy_note_2 = dto_note.ToTomboyNote ();
			Assert.AreEqual (tomboy_note.Guid, tomboy_note_2.Guid);
			Assert.AreEqual (tomboy_note.Uri, tomboy_note_2.Uri);
		}
		
		[Test]
		public void ConvertFromTomboyNoteToDTO()
		{
			var tomboy_note = new Note ();
			tomboy_note.Title = "This is a sample note";
			tomboy_note.Text = "This is some sample text";
			
			tomboy_note.ChangeDate = DateTime.Now;
			tomboy_note.CreateDate = DateTime.Now;
			tomboy_note.MetadataChangeDate = DateTime.Now;
			
			var dto_note = tomboy_note.ToDTONote ();
			
			Assert.AreEqual (tomboy_note.Title, dto_note.Title);
			Assert.AreEqual (tomboy_note.Text, dto_note.Text);
			
			Assert.AreEqual (tomboy_note.ChangeDate, DateTime.Parse (dto_note.ChangeDate).ToUniversalTime ());
			Assert.AreEqual (tomboy_note.CreateDate, DateTime.Parse (dto_note.CreateDate).ToUniversalTime ());
			Assert.AreEqual (tomboy_note.MetadataChangeDate, DateTime.Parse (dto_note.MetadataChangeDate).ToUniversalTime ());
			
			Assert.AreEqual (tomboy_note.Guid, dto_note.Guid);
			
			var tag_intersection = dto_note.Tags.Intersect (tomboy_note.Tags.Keys);
			Assert.AreEqual (dto_note.Tags.Count (), tag_intersection.Count ());
		}
		[Test]
		public void ConvertFromDTONoteToTomboyNote()
		{
			var dto_note = new DTONote ();
			dto_note.Title = "This is a sample note";
			dto_note.Text = "This is some sample text";
			
			dto_note.ChangeDate = DateTime.Now.ToString (Tomboy.Writer.DATE_TIME_FORMAT);
			dto_note.MetadataChangeDate = DateTime.Now.ToString (Tomboy.Writer.DATE_TIME_FORMAT);
			dto_note.CreateDate = DateTime.Now.ToString (Tomboy.Writer.DATE_TIME_FORMAT);
			
			var tomboy_note = dto_note.ToTomboyNote ();
			
			Assert.AreEqual (tomboy_note.Title, dto_note.Title);
			Assert.AreEqual (tomboy_note.Text, dto_note.Text);
			
			Assert.AreEqual (tomboy_note.ChangeDate, DateTime.Parse (dto_note.ChangeDate).ToUniversalTime ());
			Assert.AreEqual (tomboy_note.CreateDate, DateTime.Parse (dto_note.CreateDate).ToUniversalTime ());
			Assert.AreEqual (tomboy_note.MetadataChangeDate, DateTime.Parse (dto_note.MetadataChangeDate).ToUniversalTime ());
			
			var tag_intersection = dto_note.Tags.Intersect (tomboy_note.Tags.Keys);
			Assert.AreEqual (dto_note.Tags.Count (), tag_intersection.Count ());
		}
		
		[Test]
		public void ConvertFromDTOWithTags ()
		{
			var dto_note = new DTONote ();
			dto_note.Tags = new string[] { "school", "shopping", "fun" };
			
			var tomboy_note = dto_note.ToTomboyNote ();
			
			foreach (string tag in dto_note.Tags) {
				Assert.Contains (tag, tomboy_note.Tags.Keys);
			}
		}
		[Test]
		public void ConvertToDTOWithTags ()
		{
			var tomboy_note = new Note ();
			tomboy_note.Tags.Add ("school", new Tag ("school"));
			tomboy_note.Tags.Add ("shopping", new Tag ("shopping"));
			tomboy_note.Tags.Add ("fun", new Tag ("fun"));
			
			var dto_note = tomboy_note.ToDTONote ();
			
			foreach (string tag in tomboy_note.Tags.Keys) {
				Assert.Contains (tag, dto_note.Tags);
			}
		}
		
		[Test]
		public void ConvertBackAndForth ()
		{
			var tn1 = new Note () {
				Title = "This is my Title with Umlauts: äöü",
				Text = "This is my note body text.",
				CreateDate = DateTime.Now - new TimeSpan (365, 0, 0, 0),
				MetadataChangeDate = DateTime.Now,
				ChangeDate = DateTime.Now - new TimeSpan (14, 0, 0, 0)
				
				// TODO check why OpenOnStartup is of type string in Tomboy
				//OpenOnStartup = "true"
			};
			
			var dto_note = tn1.ToDTONote ();
			var tn2 = dto_note.ToTomboyNote ();
			
			// notes should be identical
			Assert.AreEqual (tn1.Guid, tn2.Guid);
			Assert.AreEqual (tn1.Uri, tn2.Uri);
			Assert.AreEqual (tn1.Title, tn2.Title);
			Assert.AreEqual (tn1.Text, tn2.Text);
			
			Assert.AreEqual (tn1.ChangeDate, tn2.ChangeDate);
			Assert.AreEqual (tn1.MetadataChangeDate, tn2.MetadataChangeDate);
			Assert.AreEqual (tn1.CreateDate, tn2.CreateDate);
			
			Assert.AreEqual (tn1.OpenOnStartup, tn2.OpenOnStartup);
			
			Assert.AreEqual (tn1.Tags.Keys, tn2.Tags.Keys);
			
		}
	}

}
