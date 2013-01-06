using DevDefined.OAuth.Storage.Basic;

namespace Rainy.OAuth
{
	public class OAuthDatabaseHandler : OAuthHandlerBase
	{
		public OAuthDatabaseHandler (OAuthAuthenticator auth) : base (auth)
		{
			AccessTokens = new DbAccessTokenRepository<AccessToken> ();
			TokenStore = new Rainy.OAuth.SimpleStore.SimpleTokenStore (AccessTokens, RequestTokens);

			SetupInspectors ();
		}
	}

	/// <summary>
	/// Is responsible for serializing and deserializing the OAuth
	/// data (i.e. AccessTokens). Data integrity is very weak - we will just
	/// periodically rewrite the WHOLE data in JSON serialized formats to a file. If the server
	/// is interrupted between two sync-to-disk processes, the authorization data is lost!
	/// </summary>
	
}