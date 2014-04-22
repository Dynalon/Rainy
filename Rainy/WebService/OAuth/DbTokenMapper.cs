using System.Collections.Generic;
using ServiceStack.Common;
using ServiceStack.OrmLite;
using DevDefined.OAuth.Storage.Basic;
using DevDefined.OAuth.Framework;
using Rainy.Db;
using System;
using DevDefined.OAuth.Storage;
using Tomboy.Db;

namespace Rainy.OAuth
{
	public static class DbTokenMapper
	{
		public static DBAccessToken ToDBAccessToken (this IToken token)
		{
			var db_token = new DBAccessToken ();
			db_token.Token = token.Token;
			db_token.Secret = token.TokenSecret;
			db_token.ConsumerKey = token.ConsumerKey;
			db_token.Realm = token.Realm;
			if (token is AccessToken) {
				db_token.UserName = ((AccessToken)token).UserName;
				db_token.ExpiryDate = ((AccessToken)token).ExpiryDate;
				db_token.Roles = ((AccessToken)token).Roles;
			}
			return db_token;
		}
		public static IToken ToAccessToken (this DBAccessToken token)
		{
			var access_token = new AccessToken();
			access_token.ConsumerKey = token.ConsumerKey;
			access_token.TokenSecret = token.Secret;
			access_token.Token = token.Token;
			access_token.Realm = token.Realm;
			access_token.Roles = token.Roles;
			access_token.UserName = token.UserName;
			access_token.ExpiryDate = token.ExpiryDate;
			return access_token;
		}
		public static DBRequestToken ToDBRequestToken (this IToken token)
		{
			var db_request_token = new DBRequestToken ();
			db_request_token.PopulateWith (token);
			return db_request_token;
		}
		public static IToken ToRequestToken (this DBRequestToken token)
		{
			var request_token = new RequestToken ();
			request_token.PopulateWith (token);
			return request_token;
		}
		public static void SetTokenKey (this AccessToken token, string key)
		{
			if (string.IsNullOrEmpty (key))
				throw new ArgumentNullException ("key");

			token.Roles = new string[] { "token_key:" + key };
		}
		public static string GetTokenKey (this AccessToken token)
		{
			return token.Roles[0].Substring ("token_key:".Length);
		}

		// the token is a crypto key, but we only store a fraction of the key in the database
		// for authentication
		public static string ToShortToken (this string token)
		{
			return token.Substring(0, 24);
		}
	}
}