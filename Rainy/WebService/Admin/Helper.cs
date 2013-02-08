using System;
using System.Linq;

namespace Rainy
{
	public static class Helper
	{
		public static bool IsOnlySafeChars (this string string_sequence)
		{
			if (string.IsNullOrEmpty (string_sequence)) {
				return true;
			}
			char[] safe_chars = new char[] { '_', '-', '.' };
			var arr = string_sequence.ToCharArray ();
			return arr.All (c => char.IsLetter (c) || char.IsNumber (c) || safe_chars.Contains (c));
		}
	}
}
