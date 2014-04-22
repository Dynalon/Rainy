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
	/// <summary>
	/// Simple token store. Can hold RequestTokens and AccessTokens.
	/// </summary>
	
	/*public class SimpleTokenRepository<T> : ITokenRepository<T>
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
	}*/

	// we rely on SSL to avoid replay attacks, so do not keep track of nonces
	public class DummyNonceStore : INonceStore
	{
		public bool RecordNonceAndCheckIsUnique (IConsumer consumer, string nonce)
		{
			return true;
		}
	}

	public class DbRequestTokenRepository<T> : DbAccessObject, ITokenRepository<T>
		where T: RequestToken
	{
		public DbRequestTokenRepository (IDbConnectionFactory factory) : base(factory)
		{
		}
		public T GetToken (string token)
		{
			using (var conn = connFactory.OpenDbConnection ())
			{
				DBRequestToken request_token;
				request_token = conn.First<DBRequestToken> (t => t.Token == token);
				return (T) request_token.ToRequestToken ();
			}
		}

		public void SaveToken (T token)
		{
			using (var conn = connFactory.OpenDbConnection ()) {
				using (var trans = conn.BeginTransaction ()) {
					// first delete the token
					conn.Delete<DBRequestToken> (token.ToDBRequestToken ());
					// insert a fresh copy
					conn.Insert<DBRequestToken> (token.ToDBRequestToken ());
					trans.Commit ();
				}
			}
		}
	}

	public class DbAccessTokenRepository<T> : DbAccessObject, ITokenRepository<T>
		where T: AccessToken
	{
		public DbAccessTokenRepository (IDbConnectionFactory factory) : base (factory)
		{
		}
		public T GetToken (string token)
		{
			var short_token = token.ToShortToken ();
			using (var conn = connFactory.OpenDbConnection ()) {
				DBAccessToken t;
				t = conn.First<DBAccessToken> (tkn => tkn.Token == short_token);
				return (T) t.ToAccessToken();
			}
		}
		public void SaveToken (T token)
		{
			// we only store a part of the token - the remainder is part of the encryption key
			// which we do not want to store
			var db_token = token.ToDBAccessToken ();
			db_token.Token = db_token.Token.ToShortToken ();
			using (var conn = connFactory.OpenDbConnection ()) {
				using (var trans = conn.BeginTransaction ()) {
					// first delete the token
					conn.Delete<DBAccessToken> (t => t.Token == db_token.Token);
					// insert a fresh copy
					conn.Insert<DBAccessToken> (db_token);
					trans.Commit ();
				}
			}
		}
	}

}