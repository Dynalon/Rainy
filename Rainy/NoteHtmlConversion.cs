using System;
using CsQuery;

namespace Rainy.NoteConversion
{
	public static class NoteConversion
	{
		public static string ToHtml (this string note_body)
		{
			CQ note = note_body;

			foreach (var o in note["bold"]) {
				var html = o.InnerHTML;
				var elem = o.ChildElements;
				CQ b = "<b />";
				b.Append (elem);
				o.AppendChild ((IDomElement) b);
			}
			return note.RenderSelection ();
		}
	}
}
