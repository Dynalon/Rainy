//
//  Author:
//       Timo Dörr <timo@latecrew.de>
//
//  Copyright (c) 2012 Timo Dörr
//
//  This library is free software; you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as
//  published by the Free Software Foundation; either version 2.1 of the
//  License, or (at your option) any later version.
//
//  This library is distributed in the hope that it will be useful, but
//  WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
//  Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public
//  License along with this library; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
using System;
using NUnit.Framework;
using DevDefined.OAuth.Framework;
using DevDefined.OAuth.Storage.Basic;
using Tomboy.Sync.Web;
using System.Linq;
using Tomboy.Sync;

namespace Tomboy
{
	[TestFixture]

	// UbuntuOne currently broken
	// although GetAllNotes () works, DeleteNotes() does not
	// and we need this to reset U1 notes to zero before each test
	[Ignore]
	public class UbuntuOneWebSyncServerTests : AbstractSyncServerTests
	{
		protected IToken GetAccessToken ()
		{
			// access tokens can be retrieved with gconf once tomboy is setup for syncing
			// use those paths:
			// /apps/tomboy/sync/tomboyweb/oauth_token
			// /apps/tomboy/sync/tomboyweb/oauth_token_secret

			IToken access_token = new AccessToken ();
			access_token.ConsumerKey = "anyone";
			access_token.Token = "zqkX2sJ0DN2xS2wp7Vjb";
			access_token.TokenSecret = "zjhRkTWWFSJCQdZgr61thWD7qDz7z3t7LT3F9mQ7Hxk0cDV0hqF11xcRR38dLVJxX1Qb3lxCcRN5nwXt";

			return access_token;
		}

		[SetUp]
		public void SetUp ()
		{

			var uri = "https://one.ubuntu.com/notes/";

			this.syncServer = new WebSyncServer (uri, GetAccessToken ());

			// delete all notes on the server before every test
			syncServer.BeginSyncTransaction ();
			var notes = syncServer.GetAllNotes (false);
			syncServer.DeleteNotes (notes.Select (n => n.Guid).ToList ());

			notes = syncServer.GetAllNotes (false);
			Assert.AreEqual (0, notes.Count);
		}
	}
}
