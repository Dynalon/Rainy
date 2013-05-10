using System;
using System.Linq;
using ServiceStack.ServiceHost;
using log4net;
using JsonConfig;
using ServiceStack.Common.Web;
using System.Text.RegularExpressions;
using Rainy.ErrorHandling;

namespace Rainy.WebService.Admin
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
		public static int PasswordScore (this string password)
		{
			int score = 0;
			if (password.Length < 8)
				return -1;

			if (password.Length > 12)
				score++;

			if (Regex.IsMatch(password, @"[0-9]+(\.[0-9][0-9]?)?"))
				score++;
			if (Regex.IsMatch(password, @"^(?=.*[a-z])(?=.*[A-Z]).+$"))
				score++;
			if (Regex.IsMatch(password, @"[!,@,#,$,%,^,&,*,?,_,~,-,£,(,),\.,€]"))
				score++;

			return score;
		}
		public static bool IsSafeAsPassword (this string password)
		{
			if (PasswordScore (password) > 2)
				return true;
			else
				return false;
		}
	}

	public class AdminPasswordRequiredAttribute : Attribute, IHasRequestFilter
	{
		protected ILog Logger;
		public AdminPasswordRequiredAttribute ()
		{
			Logger = LogManager.GetLogger (GetType ());
		}
		public IHasRequestFilter Copy ()
		{
			return this;
		}
		public int Priority { get { return 100; } }

		public void RequestFilter (IHttpRequest request, IHttpResponse response, object requestDto)
		{
			var authority_header = request.Headers ["Authority"];
			if (!string.IsNullOrEmpty (authority_header) &&
				authority_header == Config.Global.AdminPassword) {
				// auth worked
				return;
			}
			throw new UnauthorizedException () {ErrorMessage = "Wrong password"};
		}
	}
}
