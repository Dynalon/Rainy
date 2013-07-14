using ServiceStack.OrmLite;
using ServiceStack.ServiceHost;
using Rainy.WebService.OAuth;
using System.Collections.Generic;
using Rainy.Db;
using System.Linq;

namespace Rainy.WebService.Management.Admin
{
	public class AccessTokenDto
	{
		public string TokenPart { get; set; }
		public string DeviceName { get; set; }
	}

	[Route("/api/{Username}/access_token","GET, OPTIONS")]
	[OAuthRequired]
	public class GetTokenRequest: IReturn<List<AccessTokenDto>>
	{
		public string Username { get; set; }
	}

	[Route("/api/{Username}/access_token/{TokenPart}","DELETE")]
	[OAuthRequired]
	public class DeleteTokenRequest: AccessTokenDto, IReturnVoid
	{
		public string Username { get; set; }
	}

	[Route("/api/{Username}/access_token/","PUT")]
	[OAuthRequired]
	public class UpdateTokenRequest: AccessTokenDto, IReturnVoid
	{
		public string Username { get; set; }
	}

	public class TokenService : OAuthServiceBase
	{
		private IDbConnectionFactory connFactory;
		public TokenService (IDbConnectionFactory factory) : base ()
		{
			connFactory = factory;
		}

		public object Get (GetTokenRequest req)
		{
			List<DBAccessToken> db_tokens;
			using (var db = connFactory.OpenDbConnection ()) {
				db_tokens = db.Select<DBAccessToken> (t => t.UserName == this.requestingUser.Username);
			}

			if (db_tokens == null)
				return new List<AccessTokenDto> ();

			else
				return db_tokens.Select (db_token => {
					return new AccessTokenDto {
						TokenPart = db_token.Token,
						DeviceName = "Unknown Device"
					};
				}).ToList ();
		}
		public object Put (UpdateTokenRequest req)
		{
			string device_name = req.DeviceName;

			using (var db = connFactory.OpenDbConnection ()) {
				db.UpdateOnly (new DBAccessToken () { DeviceName = device_name }, t => new { t.DeviceName },  tk => tk.Token == requestingUser.AuthToken);
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