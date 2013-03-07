using System;
using System.Linq;
using ServiceStack.ServiceHost;
using log4net;
using JsonConfig;
using ServiceStack.Common.Web;

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
			var authFailedResponse = new HttpResult () {
				StatusCode = System.Net.HttpStatusCode.Unauthorized
			};
			// jQuery & Co. do not send the Authority header for options preflight

			// so we need to accept OPTIONS requests without password
			if (request.HttpMethod == "OPTIONS")
				return;

			try {
				if (request.Headers ["Authority"] != Config.Global.AdminPassword) {
					response.StatusCode = 401;
					response.StatusDescription = "Unauthorized.";
					response.Close ();
				}
			} catch (Exception e) {
				Logger.Warn ("Admin authentication failed");
					response.StatusCode = 401;
					response.StatusDescription = "Unauthorized.";
					response.Close ();
			}

			// auth worked
			return;
		}
	}
}
