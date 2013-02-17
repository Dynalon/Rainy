using DevDefined.OAuth.Storage.Basic;
using Rainy.Interfaces;

namespace Rainy.OAuth
{
	public class OAuthDatabaseHandler : OAuthHandlerBase
	{
		public OAuthDatabaseHandler (CredentialsVerifier auth) : base (auth)
		{
			AccessTokens = new DbAccessTokenRepository<AccessToken> ();
			TokenStore = new Rainy.OAuth.SimpleStore.SimpleTokenStore (AccessTokens, RequestTokens);

			SetupInspectors ();
		}
		public override void Dispose ()
		{

		}
	}
}