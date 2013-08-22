using System;
using NUnit.Framework;
using Rainy.NoteConversion;

namespace Rainytests
{
	[TestFixture()]
	public class NoteConversionTests
	{
		[Test()]
		public void Basic ()
		{
			string note_body = "<italic>Italic.</italic><bold>This is bold.</bold>";
			var result = note_body.ToHtml ();
			Console.WriteLine (result);
			Assert.AreEqual ("<i>Italic.</i><b>This is bold.</b>", result);

			
		}
	}
}

