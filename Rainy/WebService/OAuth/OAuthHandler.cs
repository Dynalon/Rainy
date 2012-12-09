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
using DevDefined.OAuth.Storage;
using DevDefined.OAuth.Storage.Basic;
using System.Collections.Generic;
using System.Threading;
using ServiceStack.Text;
using System.IO;
using DevDefined.OAuth.Testing;
using DevDefined.OAuth.Provider;
using Rainy.OAuth.SimpleStore;
using DevDefined.OAuth.Provider.Inspectors;
using System;
using DevDefined.OAuth.Framework;
using DevDefined.OAuth.Utility;

namespace Rainy.OAuth
{

	public delegate bool OAuthAuthenticator (string username, string password);

	/// <summary>
	/// Data store manager. Is also responsible for serializing and deserializing the OAuth
	/// data (i.e. AccessTokens). Data integrity is very weak - we will just
	/// periodically rewrite the WHOLE data in JSON serialized formats to a file. If the server
	/// is interrupted between two sync-to-disk processes, the authorization data is lost!
	/// </summary>
	public class OAuthHandler
	{
		// the data stores required by the OAuth process
		public ITokenRepository<AccessToken> AccessTokens;
		public ITokenRepository<RequestToken> RequestTokens;
		public ITokenStore TokenStore;
		public INonceStore NonceStore;
		public IConsumerStore ConsumerStore;
		public OAuthProvider Provider;
		public OAuthAuthenticator Authenticator;

		// the paths where we store our data
		protected DirectoryInfo OauthDataPath;
		protected string AccessRepoFile;

		// the interval in which we will write the repositories to disk in seconds
		protected uint DiskWriteInterval;
		protected Thread WriteThread;

		public OAuthHandler (string oauth_data_path, OAuthAuthenticator auth, uint disk_write_interval = 300)
		{
			this.DiskWriteInterval = disk_write_interval;
			this.Authenticator = auth;

			// initialize the pathes for on-disk storage
			OauthDataPath = new DirectoryInfo (oauth_data_path);
			if (!Directory.Exists (oauth_data_path)) {
				// create the path where we store the oauth data
				Directory.CreateDirectory (oauth_data_path);
			}
			AccessRepoFile = Path.Combine (OauthDataPath.FullName, "access_tokens.json");

			// read in persistent data
			ReadDataFromDisk ();

			// initialize those classes that are not persisted
			RequestTokens = new SimpleTokenRepository<RequestToken> ();
			TokenStore = new Rainy.OAuth.SimpleStore.SimpleTokenStore (AccessTokens, RequestTokens);
			ConsumerStore = new RainyConsumerStore ();
			NonceStore = new TestNonceStore ();
		
			Provider = new OAuthProvider (TokenStore, new IContextInspector[] {
				new NonceStoreInspector (NonceStore),
				new TimestampRangeInspector (new TimeSpan (1, 0, 0)),
				new OAuth10AInspector (TokenStore)
					
				// TODO signature validation currently fails
				// don't know if it makes sense to enable this since this 
				// verifies the get request_token step, but our conumser_key and consumer_secret are
				// publically known
				// new SignatureValidationInspector (ConsumerStore),
	
				// will check the consumer_key to be known
				// might be disabled since our consumer_key is public
				//new ConsumerValidationInspector (ConsumerStore)
			});

		}
		// TODO NonceStore and RequestTokens should be persistent, too.
		protected void ReadDataFromDisk ()
		{
			lock (this) {
				if (File.Exists (AccessRepoFile)) {
					string access_repo_serialized = File.ReadAllText (AccessRepoFile);
					this.AccessTokens = access_repo_serialized.FromJson<ITokenRepository<AccessToken>> ();
				} else {
					this.AccessTokens = new SimpleTokenRepository<AccessToken> ();
				}
			}
		}
		protected void WriteDataToDisk ()
		{
			// TODO create a lock on the data while we serialize
			lock (this) {
				string access_repo_serialized = this.AccessTokens.ToJson ();
				// TODO create backup of existing file before overwriting
				File.WriteAllText (AccessRepoFile, access_repo_serialized);
			}
		}

		public void StartIntervallWriteThread ()
		{
			WriteThread = new Thread (() => {
				while (true) {
					Thread.Sleep ((int) DiskWriteInterval * 1000);

					lock (this) {
						WriteDataToDisk ();
					}
				}
			});
			WriteThread.Start ();
		}
		public void StopIntervallWriteThread ()
		{
			lock (this) {
				// due to the lock its save to abort the thread at this place
				if (WriteThread.IsAlive) {
					WriteThread.Abort ();
				}

				WriteDataToDisk ();
			}
		}
	}
}