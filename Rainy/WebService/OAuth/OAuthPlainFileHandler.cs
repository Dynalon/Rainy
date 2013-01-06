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
using DevDefined.OAuth.Storage.Basic;
using System.Threading;
using ServiceStack.Text;
using System.IO;

namespace Rainy.OAuth
{
	/// <summary>
	/// Is responsible for serializing and deserializing the OAuth
	/// data (i.e. AccessTokens). Data integrity is very weak - we will just
	/// periodically rewrite the WHOLE data in JSON serialized formats to a file. If the server
	/// is interrupted between two sync-to-disk processes, the authorization data is lost!
	/// </summary>
	public class OAuthPlainFileHandler : OAuthHandlerBase
	{
		// the paths where we store our data
		protected DirectoryInfo oauthDataPath;
		protected string accessRepoFile;

		// the interval in which we will write the repositories to disk in seconds
		protected uint DiskWriteInterval;
		protected Thread WriteThread;

		public OAuthPlainFileHandler (string data_path, OAuthAuthenticator auth, uint disk_write_interval = 300)
			: base (auth)
		{
			this.DiskWriteInterval = disk_write_interval;

			// initialize the pathes for on-disk storage
			var oauth_data_path = Path.Combine (data_path, "oauth");
			if (!Directory.Exists (oauth_data_path)) {
				// create the path where we store the oauth data
				Directory.CreateDirectory (oauth_data_path);
			}
			oauthDataPath = new DirectoryInfo (oauth_data_path);
			accessRepoFile = Path.Combine (oauthDataPath.FullName, "access_tokens.json");

			// read in persistent data, will initialize AccessToken
			ReadDataFromDisk ();

			TokenStore = new Rainy.OAuth.SimpleStore.SimpleTokenStore (AccessTokens, RequestTokens);

			SetupInspectors ();

			StartIntervallWriteThread ();
		}

		// TODO NonceStore and RequestTokens should be persistent, too.
		protected void ReadDataFromDisk ()
		{
			lock (this) {
				if (File.Exists (accessRepoFile)) {
					string access_repo_serialized = File.ReadAllText (accessRepoFile);
					this.AccessTokens = access_repo_serialized.FromJson<ITokenRepository<AccessToken>> ();
				} else {
					this.AccessTokens = new SimpleTokenRepository<AccessToken> ();
				}
			}
		}
		protected void WriteDataToDisk ()
		{
			lock (this) {
				string access_repo_serialized = this.AccessTokens.ToJson ();
				// TODO create backup of existing file before overwriting
				File.WriteAllText (accessRepoFile, access_repo_serialized);
			}
		}

		protected void StartIntervallWriteThread ()
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
		protected void StopIntervallWriteThread ()
		{
			lock (this) {
				// due to the lock its save to abort the thread at this place
				// which is in the Thread.Sleep phase
				if (WriteThread.IsAlive) {
					WriteThread.Abort ();
				}

				WriteDataToDisk ();
			}
		}
		public override void Dispose ()
		{
			StopIntervallWriteThread ();
		}
	}
}