using ServiceStack.OrmLite;
using ServiceStack.ServiceHost;
using Rainy.WebService.OAuth;
using System.Collections.Generic;
using Rainy.Db;
using System.Linq;
using Rainy.OAuth;
using Tomboy.Db;

namespace Rainy.WebService.Management.Admin
{
	public class AccessTokenDto
	{
		public string TokenPart { get; set; }
		public string DeviceName { get; set; }
	}

	[Route("/oauth/tokens/access_token","GET, OPTIONS")]
	public class GetTokenRequest: IReturn<List<AccessTokenDto>>
	{
		public string Username { get; set; }
	}

	[Route("/oauth/tokens/access_token/{TokenPart}","DELETE")]
	public class DeleteTokenRequest: AccessTokenDto, IReturnVoid
	{
		public string Username { get; set; }
	}

	[Route("/oauth/tokens/access_token/","PUT")]
	public class UpdateTokenRequest: AccessTokenDto, IReturnVoid
	{
		public string Username { get; set; }
	}

	public class TokenService : OAuthServiceBase
	{
		public TokenService (IDbConnectionFactory factory) : base (factory)
		{
		}

		public object Get (GetTokenRequest req)
		{
			List<DBAccessToken> db_tokens;
			using (var db = connFactory.OpenDbConnection ()) {
				db_tokens = db.Select<DBAccessToken> (t => t.UserName == this.requestingUser.Username);
			}

			if (db_tokens == null)
				return new List<AccessTokenDto> ();

			return db_tokens.Select (db_token => {
				return new AccessTokenDto {
					TokenPart = db_token.Token,
					DeviceName = db_token.DeviceName
				};
			}).ToList ();
		}
		public object Put (UpdateTokenRequest req)
		{
			string device_name = req.DeviceName;
			var token_part = req.TokenPart;

			using (var db = connFactory.OpenDbConnection ()) {
				db.UpdateOnly (new DBAccessToken () { DeviceName = device_name }, t => new { t.DeviceName },  tk => tk.Token == token_part);
			}
			return null;
		}
		public object Delete (DeleteTokenRequest req)
		{
			using (var db = connFactory.OpenDbConnection ()) {
				db.Delete<DBAccessToken> (t => t.Token == req.TokenPart);
			}
			return null;
		}
	}
	
}