using System;
using System.Collections.Generic;
using System.Net;
using NUnit.Framework;
using Rainy.Interfaces;
using ServiceStack.ServiceClient.Web;
using Tomboy;

namespace Rainy.Tests
{
	public class DummyAuthenticator : IAuthenticator
	{
		public bool VerifyCredentials (string username, string password)
		{
			return true;
		}
	}
	public class DummyAdminAuthenticator : IAdminAuthenticator
	{
		string Password;
		public DummyAdminAuthenticator ()
		{
		}
		public DummyAdminAuthenticator (string pass)
		{
			Password = pass;
		}
		public bool VerifyAdminPassword (string password)
		{
			if (string.IsNullOrEmpty (Password))
				return true;
			else return Password == password;
		}
	}

	public abstract class TestBase
	{
		protected RainyTestServer testServer;
		protected string adminPass;
		protected string listenUrl;

		public TestBase ()
		{
			ServicePointManager.CertificatePolicy = new DummyCertificateManager ();
		}

		[SetUp]
		public virtual void SetUp ()
		{
			testServer = new RainyTestServer ();
			adminPass = RainyTestServer.ADMIN_TEST_PASS;
			listenUrl = testServer.ListenUrl;
		}
		[TearDown]
		public virtual void TearDown ()
		{
			testServer.Stop ();
		}

		protected JsonServiceClient GetAdminServiceClient ()
		{
			var client = new JsonServiceClient (listenUrl);
			client.LocalHttpWebRequestFilter += (request) => {
					request.Headers.Add ("Authority", adminPass);
			};

			return client;
		}
		protected JsonServiceClient GetServiceClient ()
		{
			return new JsonServiceClient (testServer.ListenUrl);
		}

		public static List<Note> GetSampleNotes ()
		{
			var sample_notes = new List<Note> ();

			// TODO: add tags to the notes!

			sample_notes.Add (new Note () {
				Title = "Sämplé title 1!",
				Text = "** This is the text of Sämple Note 1**",
				CreateDate = DateTime.Now,
				MetadataChangeDate = DateTime.Now,
				ChangeDate = DateTime.Now
			});

			sample_notes.Add (new Note () {
				Title = "2nd Example",
				Text = "This is the text of the second sample note",
				CreateDate = new DateTime (1984, 04, 14, 4, 32, 0, DateTimeKind.Utc),
				ChangeDate = new DateTime (2012, 04, 14, 4, 32, 0, DateTimeKind.Utc),
				MetadataChangeDate = new DateTime (2012, 12, 12, 12, 12, 12, DateTimeKind.Utc),
			});

			// note that DateTime.MinValue is not an allowed timestamp for notes!
			sample_notes.Add (new Note () {
				Title = "3rd exampel title",
				Text = "Another example note",
				CreateDate = DateTime.MinValue + new TimeSpan (1, 0, 0, 0, 0),
				ChangeDate = DateTime.MinValue + new TimeSpan (1, 0, 0, 0, 0),
				MetadataChangeDate = DateTime.MinValue + new TimeSpan (1, 0, 0, 0, 0)
			});

			return sample_notes;
		}
	}
}
