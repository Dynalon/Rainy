
using System.Collections.Generic;


using System;
using DevDefined.OAuth.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.Common;
using Rainy.Db;
using ServiceStack.OrmLite;
using DevDefined.OAuth.Storage.Basic;

namespace Rainy.OAuth.SimpleStore
{
	/// <summary>
	/// Simple token store. Holds our RequestTokens and AccessTokens.
	/// </summary>
	
	public class SimpleTokenRepository<T> : ITokenRepository<T>
		where T : TokenBase
	{
		public Dictionary<string, T> _tokens { get; set; }
		public SimpleTokenRepository ()
		{
			_tokens = new Dictionary<string, T> ();
		}

		public T GetToken(string token)
		{
			return _tokens[token];
		}
		
		public void SaveToken(T token)
		{
			_tokens[token.Token] = token;
		}
	}

	public class DbAccessTokenRepository<T> : ITokenRepository<T>
		where T: AccessToken
	{
		public T GetToken (string token)
		{
			using (var conn = DbConfig.GetConnection ()) {
				DBAccessToken t;
				t = conn.First<DBAccessToken> ("Token = {0}", token);
				return (T) t.ToAccessToken();
			}
		}
		public void SaveToken (T token)
		{
			using (var conn = DbConfig.GetConnection ()) {
				using (var trans = conn.BeginTransaction ()) {
					// first delete the token
					conn.Delete (token.ToDBAccessToken ());
					// insert a fresh copy
					conn.Insert (token.ToDBAccessToken ());
					trans.Commit ();
				}
			}
		}
	}

	public class DBAccessToken : AccessToken
	{
		[PrimaryKey]
		public new string Token { get; set; }
	}

	public static class DbTokenConverter
	{
		public static DBAccessToken ToDBAccessToken (this IToken token)
		{
			var db_token = new DBAccessToken ();
			db_token.PopulateWith (token);
			return db_token;
		}
		public static IToken ToAccessToken (this DBAccessToken token)
		{
			var access_token = new AccessToken();
			access_token.PopulateWith (token);
			return access_token;
		}
	}
}