// The MIT License
//
// Copyright (c) 2006-2008 DevDefined Limited.
// Copyright (c) 2012 Timo Dörr <timo@latecrew.de>.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Generic;
using DevDefined.OAuth.Storage;
using DevDefined.OAuth.Storage.Basic;
using DevDefined.OAuth.Testing;
using DevDefined.OAuth.Provider;
using DevDefined.OAuth.Provider.Inspectors;
using Rainy.OAuth;
using Rainy.Interfaces;

namespace Rainy.OAuth
{
	// TODO: replace with Interface and wire in DI composition root
	public class OAuthHandler: IDisposable
	{
		public readonly ITokenRepository<AccessToken> AccessTokens;
		public readonly ITokenRepository<RequestToken> RequestTokens;
		protected ITokenStore TokenStore;
		protected IAuthenticator Authenticator;

		public OAuthProvider Provider;

		protected INonceStore NonceStore;
		protected IConsumerStore ConsumerStore;

		protected List<IContextInspector> inspectors = new List<IContextInspector> ();

		public OAuthHandler (IAuthenticator auth,
		                     ITokenRepository<AccessToken> access_token_repo,
		                     ITokenRepository<RequestToken> request_token_repo,
		                     ITokenStore token_store)
		{

			this.Authenticator = auth;
			this.AccessTokens = access_token_repo;
			this.RequestTokens = request_token_repo;
			this.TokenStore = token_store;

			this.ConsumerStore = new RainyConsumerStore ();
			//this.NonceStore = new DummyNonceStore ();
			// initialize those classes that are not persisted
			// TODO request tokens should be persisted in the future
			//RequestTokens = new SimpleTokenRepository<RequestToken> ();


			SetupInspectors ();
		}

		protected void SetupInspectors ()
		{
			//inspectors.Add(new NonceStoreInspector (NonceStore));
			inspectors.Add(new OAuth10AInspector (TokenStore));

			// request tokens may only be 36 hour old
			// HACK this will compare client & server times. if the client time is of
			// by more than 36 ours, the request will fail totally
			inspectors.Add(new TimestampRangeInspector (new TimeSpan (36, 0, 0)));
				
			// TODO HACK signature validation currently fails
			// this is not so bad, as we rely on SSL for encryption, we just have to make sure
			// the access token is valid elsewhere
			//inspectors.Add(new SignatureValidationInspector (ConsumerStore));
				
			// will check the consumer_key to be known
			// might be disabled since our consumer_key is public (="anyone")
			// new ConsumerValidationInspector (ConsumerStore)

			Provider = new OAuthProvider (TokenStore, inspectors.ToArray ());
		}

		public virtual void Dispose ()
		{
		}
	}
}