using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using DevDefined.OAuth.Framework;
using DevDefined.OAuth.Storage.Basic;
using NUnit.Framework;
using Rainy.Interfaces;
using Rainy.OAuth;
using ServiceStack.Common;
using ServiceStack.OrmLite;
using Tomboy.Sync.Web.DTO;
using Rainy.WebService;
using Rainy.Db;
using Tomboy.Db;

namespace Rainy.Tests.Db
{

	[TestFixture]
	public class DbOAuthTests : DbTestsBase
	{
		[Test]
		public void SaveAndReadTokenBase ()
		{
			var token = new TokenBase ();
			token.Token = Guid.NewGuid ().ToString ();
			token.TokenSecret = Guid.NewGuid ().ToString ();

			using (var conn = connFactory.OpenDbConnection ()) {
				conn.Insert (token.ToDBAccessToken ());
			}
			using (var conn = connFactory.OpenDbConnection ()) {
				var dbtoken = conn.Select<DBAccessToken> ().First ();
				Assert.AreEqual (token.Token, dbtoken.Token);
				Assert.AreEqual (token.TokenSecret, dbtoken.Secret);
			}
		}

		[Test]
		public void DbAccessTokenRepository ()
		{
			var repo = new DbAccessTokenRepository<AccessToken> (this.connFactory);

			var token1 = new AccessToken () {
				ConsumerKey = "anyone",
				UserName = "johndoe",
				ExpiryDate = DateTime.Now.AddYears (10),
				Realm = "tomboy",
				Token = Guid.NewGuid ().ToString (),
				TokenSecret = Guid.NewGuid ().ToString (),
			};
			repo.SaveToken (token1);

			var token2 = repo.GetToken (token1.Token);

			Assert.AreEqual (token1.ConsumerKey, token2.ConsumerKey);
			Assert.AreEqual (token1.Realm, token2.Realm);
			Assert.AreEqual (token1.UserName, token2.UserName);
			Assert.AreEqual (token1.ExpiryDate, token2.ExpiryDate);

			// the token is only the first 16 byte = 192 bits - the token is
			// 160 byte = 920 bits long (due to the padding added)
			Assert.AreEqual (token1.Token.Substring (0, 24), token2.Token);
			Assert.AreEqual (token1.TokenSecret, token2.TokenSecret);
		}
	}
}
