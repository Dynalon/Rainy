using System;
using NUnit.Framework;
using Rainy.NoteConversion;

namespace Rainy.Tests.XmlNoteConversion
{
	[TestFixture]
	public class NoteToHtmlConversionTests
	{
		[Test]
		public void BoldNextToBold()
		{
			string note_body = "<bold>Bold1</bold><bold>Bold2</bold>";
			var result = note_body.ToHtml ();
			Assert.AreEqual ("<b>Bold1</b><b>Bold2</b>", result);
		}
		[Test]
		public void BoldNextToItalic()
		{
			string note_body = "<italic>Italic.</italic><bold>This is bold.</bold>";
			var result = note_body.ToHtml ();
			Assert.AreEqual ("<i>Italic.</i><b>This is bold.</b>", result);

		}
		[Test]
		public void BoldAndItalic()
		{
			string note_body = "<italic><bold>Bold and Italic.</bold></italic>";
			var result = note_body.ToHtml ();
			Assert.AreEqual ("<i><b>Bold and Italic.</b></i>", result);

		}
		[Test]
		public void MultilineItalic ()
		{
			string note_body = "<italic>Multiline\nItalic</italic>";
			var result = note_body.ToHtml ();
			Assert.AreEqual ("<i>Multiline<br>Italic</i>", result);
		}

		[Test]
		public void UnorderedLists ()
		{
			string note_body = "<list><list-item dir=\"ltr\" class>Foo</list-item><list-item>Bar</list-item></list>";
			var result = note_body.ToHtml ();
			Assert.AreEqual ("<ul><li>Foo</li><li>Bar</li></ul>", result);
		}
		
		[Test]
		public void SizeTags ()
		{
			string html_body = "<size:small>Foobar</size:small>";
			var result = html_body.ToHtml ();
			string expected = "<h3>Foobar</h3>";
			Assert.AreEqual (expected, result);
		}
	}

	[TestFixture]
	public class HtmlToTomboyNote
	{
		[Test]
		public void BoldNextToBold()
		{
			string note_body = "<b>Bold1</b><b>Bold2</b>";
			var result = note_body.ToTomboyXml ();
			Assert.AreEqual ("<bold>Bold1</bold><bold>Bold2</bold>", result);
		}
		[Test]
		public void UnorderedLists ()
		{
			string html_body = "<ul><li>Foo</li><li>Bar</li></ul>";
			var result = html_body.ToTomboyXml ();
			string expected = "<list><list-item>Foo</list-item><list-item>Bar</list-item></list>";
			Assert.AreEqual (expected, result);
		}

		[Test]
		public void LineBreaks()
		{
			string html_body = "<b>Bold1</b><b>Bold2<br></b>";
			var result = html_body.ToTomboyXml ();
			string expected = "<bold>Bold1</bold><bold>Bold2\n</bold>";
			Assert.AreEqual (expected, result);
		}

		[Test]
		public void SizeTags ()
		{
			string html_body = "<h3>Foobar</h3>";
			var result = html_body.ToTomboyXml ();
			string expected = "<size:small>Foobar</size:small>";
			Assert.AreEqual (expected, result);
		}
	}
}

