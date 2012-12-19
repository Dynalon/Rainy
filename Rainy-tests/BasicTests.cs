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
using ServiceStack.ServiceClient.Web;
using Tomboy.Sync.DTO;

namespace Rainy.Tests
{

	public class BasicTests 
	{
		[Test]
		public void CheckApiRef ()
		{
			var response = GetRootApiRef ();
	
			Assert.AreEqual ("1.0", response.ApiVersion);

			// check the OAuth urls
			Assert.That (response.OAuthAccessTokenUrl.StartsWith (rainyListenUrl));
			Assert.That (response.OAuthAuthorizeUrl.StartsWith (rainyListenUrl));
			Assert.That (response.OAuthRequestTokenUrl.StartsWith (rainyListenUrl));

			Assert.That (Uri.IsWellFormedUriString (response.OAuthAccessTokenUrl, UriKind.Absolute));
			Assert.That (Uri.IsWellFormedUriString (response.OAuthRequestTokenUrl, UriKind.Absolute));
			Assert.That (Uri.IsWellFormedUriString (response.OAuthAuthorizeUrl, UriKind.Absolute));
		}

		// TODO implement way more security tests
		[Test]
		// since the exception name is returned in the webservice result,
		// we can't use [ExpcetedException] here
		public void UnauthenticatedUserAccessFails()
		{
			Exception caught_exception = new Exception ();
			try {
				var apiResponse = GetRootApiRef ();
				var restClient = new JsonServiceClient (baseUri);

				restClient.Get<UserResponse> (apiResponse.UserRef.ApiRef);

				// we are not allowed to reach here
				Assert.Fail ();
			} catch (Exception e) {
				caught_exception = e;
			} finally {
				Assert.AreEqual ("Unauthorized", caught_exception.Message);
			}
		}

		[Test]
		public void GetUser ()
		{

			var user_response = GetUserInfo ();

			Assert.AreEqual (user_response.Username, "johndoe");
			Assert.AreEqual (user_response.LatestSyncRevision, -1);

			Assert.That (Uri.IsWellFormedUriString (user_response.NotesRef.ApiRef, UriKind.Absolute));

		}
	}
}
