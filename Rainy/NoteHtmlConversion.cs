using System;
using CsQuery;
using System.Text.RegularExpressions;

namespace Rainy.NoteConversion
{
	public static class NoteConversion
	{
		public static string ToHtml (this string note_body)
		{
			// remove explicity line breaks after list-item that tomboy requires
			var body = Regex.Replace (note_body, "\n</list-item>", "</list-item>");
			body = body.Replace ("\n", "<br>");

			CQ note = body;

			note["bold"].ReplaceOuterWithTag("<b/>");
			note["italic"].ReplaceOuterWithTag ("<i/>");
			note["list"].ReplaceOuterWithTag ("<ul/>");
			note["list-item"].ReplaceOuterWithTag ("<li/>");

			note["size\\:huge"].ReplaceOuterWithTag ("<h1/>");
			note["size\\:large"].ReplaceOuterWithTag ("<h2/>");
			note["size\\:small"].ReplaceOuterWithTag ("<small/>");

			note["strikethrough"].ReplaceOuterWithTag ("<strike/>");
			note["monospace"].ReplaceOuterWithTag ("<pre/>");
			note["highlight"].ReplaceOuterWithTag ("<span class=\"highlight\" />");
			note["link\\:internal"].ReplaceOuterWithTag ("<a class=\"internal\" />");

			note["link\\:url"].ReplaceOuterWithTag ("<a class=\"url\" />");
			// we have to set the href attribtue for those links
			note["a[class='url']"].Each (e => {
				e.SetAttribute ("href", e.InnerText);
			});

			var render = note.Render ();
			return render;
		}
		private static CQ ReplaceOuterWithTag (this CQ element, string tag)
		{
			element.Each (domobj => {
				CQ t = new CQ (tag);
				CQ o = new CQ (domobj);
				o.ReplaceOuterWith (t);
			});
			return element;
		}
		private static CQ ReplaceOuterWith (this CQ element, CQ new_outer)
		{
			element.WrapInner (new_outer);
			return element.ReplaceWith (element.Children ());
		}

		public static string ToTomboyXml (this string html_body)
		{
			var body = html_body.Replace ("<br>", "\n");
			CQ html = body;

			html["b"].ReplaceOuterWithTag("<bold/>");
			html["i"].ReplaceOuterWithTag ("<i/>");
			html["ul"].ReplaceOuterWithTag ("<list/>");
			html["li"].ReplaceOuterWithTag ("<list-item />");
			html["h1"].ReplaceOuterWithTag ("<size:huge/>");
			html["h2"].ReplaceOuterWithTag ("<size:large/>");
			html["small"].ReplaceOuterWithTag ("<size:small/>");
			html["strike"].ReplaceOuterWithTag ("<strikethrough/>");
			html["pre"].ReplaceOuterWithTag ("<monospace/>");
			html["a[class='internal']"].ReplaceOuterWithTag ("<link:internal/>");
			html["a[class='url']"].ReplaceOuterWithTag ("<link:url/>");

			html["a"].ReplaceOuterWithTag ("<link:url/>");

			html["span[class='highlight']"].ReplaceOuterWithTag ("<highlight/>");
			
			// hack replace <div> which get inserted by the wysihtml5
			html["div"].Each (domobj => {
				CQ e = new CQ (domobj);
				var all = new CQ(e.Html());
				e.ReplaceWith(all);
			});


			var render = html.Render ();
			// maybe bug int tomboy - </list-items> need a single \n in from of them
			render = Regex.Replace (render, @"(?!\n)</list-item>", "\n</list-item>", RegexOptions.ExplicitCapture);
			return render;
		}
	}
}
