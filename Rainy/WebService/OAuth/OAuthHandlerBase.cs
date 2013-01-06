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
using Rainy.OAuth.SimpleStore;

namespace Rainy.OAuth
{
	public delegate bool OAuthAuthenticator (string username, string password);

	public abstract class OAuthHandlerBase : IDisposable
	{
		// the data stores required by the OAuth process
		public ITokenRepository<AccessToken> AccessTokens;
		public ITokenRepository<RequestToken> RequestTokens;
		public ITokenStore TokenStore;

		public OAuthAuthenticator Authenticator;
		public OAuthProvider Provider;

		protected INonceStore NonceStore;
		protected IConsumerStore ConsumerStore;

		protected List<IContextInspector> inspectors = new List<IContextInspector> ();

		public OAuthHandlerBase (OAuthAuthenticator auth)
		{
			Authenticator = auth;

			ConsumerStore = new RainyConsumerStore ();
			NonceStore = new TestNonceStore ();
			// initialize those classes that are not persisted
			// TODO request tokens should be persisted in the future
			RequestTokens = new SimpleTokenRepository<RequestToken> ();

		}

		protected void SetupInspectors ()
		{
			inspectors.Add(new NonceStoreInspector (NonceStore));
			inspectors.Add(new OAuth10AInspector (TokenStore));

			// request tokens may only be 1 hour old
			inspectors.Add(new TimestampRangeInspector (new TimeSpan (1, 0, 0)));
				
				// TODO signature validation currently fails
				// don't know if it makes sense to enable this since this 
				// verifies the get request_token step, but our conumser_key and consumer_secret are
				// publically known
				// new SignatureValidationInspector (ConsumerStore),
				
				// will check the consumer_key to be known
				// might be disabled since our consumer_key is public
				// new ConsumerValidationInspector (ConsumerStore)

			Provider = new OAuthProvider (TokenStore, inspectors.ToArray ());
		}

		public abstract void Dispose ();
	}
}