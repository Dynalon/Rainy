using DevDefined.OAuth.Storage.Basic;
using Rainy.Interfaces;
using ServiceStack.OrmLite;

namespace Rainy.OAuth
{
	public class OAuthDatabaseHandler : OAuthHandlerBase
	{
		public OAuthDatabaseHandler (IDbConnectionFactory factory, IAuthenticator auth) : base (auth)
		{
			AccessTokens = new DbAccessTokenRepository<AccessToken> (factory);
			TokenStore = new Rainy.OAuth.SimpleStore.SimpleTokenStore (AccessTokens, RequestTokens);

			SetupInspectors ();
		}
		public override void Dispose ()
		{

		}
	}
}