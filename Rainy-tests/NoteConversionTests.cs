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
		public void UnorderedListsLinebreakRemove ()
		{
			string note_body = "<list><list-item dir=\"ltr\" class>Foo\n</list-item><list-item>Bar\n</list-item></list>";
			var result = note_body.ToHtml ();
			Assert.AreEqual ("<ul><li>Foo</li><li>Bar</li></ul>", result);
		}
		
		[Test]
		public void SizeTags ()
		{
			string note_body = "<size:small>Foobar</size:small>";
			var result = note_body.ToHtml ();
			string expected = "<small>Foobar</small>";
			Assert.AreEqual (expected, result);
		}
		[Test]
		public void HighlightTag ()
		{
			string note_body = "<highlight>Foobar</highlight>";
			var result = note_body.ToHtml ();
			string expected = "<span class=\"highlight\">Foobar</span>";
			Assert.AreEqual (expected, result);
		}
		[Test]
		public void InternalLink ()
		{
			string note_body = "<link:internal>Foobar</link:internal>";
			var result = note_body.ToHtml ();
			string expected = "<a class=\"internal\">Foobar</a>";
			Assert.AreEqual (expected, result);
		}
		[Test]
		public void UrlLink ()
		{
			string note_body = "<link:url>http://www.example.com/index.php?foo=bar</link:url>";
			var result = note_body.ToHtml ();
			string expected = "<a class=\"url\" href=\"http://www.example.com/index.php?foo=bar\">http://www.example.com/index.php?foo=bar</a>";
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
		public void UnorderedListsHaveLinebreaks ()
		{
			string html_body = "<ul><li>Foo</li><li>Bar</li></ul>";
			var result = html_body.ToTomboyXml ();
			// watch for the \n - it is a bug in tomboy, list-items have to end with newline
			string expected = "<list><list-item>Foo\n</list-item><list-item>Bar\n</list-item></list>";
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
		public void SizeTagsSmall ()
		{
			string html_body = "<small>Foobar</small>";
			var result = html_body.ToTomboyXml ();
			string expected = "<size:small>Foobar</size:small>";
			Assert.AreEqual (expected, result);
		}

		[Test]
		public void HighlightTags ()
		{
			string html_body = "<span class=\"highlight\">Foobar</span>";
			var result = html_body.ToTomboyXml ();
			string expected = "<highlight>Foobar</highlight>";
			Assert.AreEqual (expected, result);
		}

		[Test]
		public void InternalLink ()
		{
			string html_body = "<a class=\"internal\">Foobar</a>";
			var result = html_body.ToTomboyXml ();
			string expected = "<link:internal>Foobar</link:internal>";
			Assert.AreEqual (expected, result);
		}

		[Test]
		public void UrlLink ()
		{
			string html_body = "<a class=\"url\" href=\"http://www.example.com/index.php?foo=bar\">http://www.example.com/index.php?foo=bar</a>";
			var result = html_body.ToTomboyXml ();
			string expected = "<link:url>http://www.example.com/index.php?foo=bar</link:url>";
			Assert.AreEqual (expected, result);
		}

		[Test]
		public void DivsAreRemoved ()
		{
			string html_body = "<div><b>Bla</b>foobar</div>";
			var result = html_body.ToTomboyXml ();
			string expected = "<bold>Bla</bold>foobar";
			Assert.AreEqual (expected, result);
		}
	}
}

