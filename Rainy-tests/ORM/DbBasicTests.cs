using System;
using Tomboy.Sync.Web.DTO;
using NUnit.Framework;
using System.Data;
using ServiceStack.OrmLite;
using ServiceStack.Common;
using System.Collections.Generic;
using System.Linq;
using DevDefined.OAuth.Framework;
using Rainy.OAuth.SimpleStore;
using DevDefined.OAuth.Storage.Basic;
using Rainy.OAuth;

namespace Rainy.Db
{

	[TestFixture]
	public class DbBasicTests : DbTestsBase
	{
		[Test]
		public void StoreAndRetrieveNote ()
		{
			var db_old = GetDBSampleNote ();

			db_old.Username = "test";
			
			using (var conn = dbFactory.OpenDbConnection ()) {
				conn.Insert (db_old);
			}
			DTONote dto_new = null;
			DBNote db_new;
			using (var conn = dbFactory.OpenDbConnection ()) {
				db_new = conn.Single <DBNote> ("Username = {0}", "test");
			}
			
			dto_new.PopulateWith (db_new);
			
			// check for equalness
			Assert.AreEqual (db_old.Title, db_new.Title);
			Assert.AreEqual (db_old.Text, db_new.Text);
			Assert.AreEqual (db_old.Tags, db_new.Tags);
			
			Assert.AreEqual (db_old.ChangeDate, db_new.ChangeDate);
			
		}
		[Test]
		public void StoreOverlongText ()
		{
			// our DB schema in SQLite is created with VARCHAR(8000) by
			// ORMLite, but actually SQLite uses TEXT as internal datatype
			// so we can store arbitrary note length. Make sure that it is
			// like that.
			
			var overlong_string = "";
			for (int i=0; i< 20000; i++) {
				overlong_string += "a";
			}
			
			var sample_note = GetDBSampleNote ();
			sample_note.Text = overlong_string;
			sample_note.Title = overlong_string;
			sample_note.Username = "overlong";
			
			using (var conn = dbFactory.OpenDbConnection ()) {
				conn.Insert (sample_note);
			}
			
			using (var conn = dbFactory.OpenDbConnection ()) {
				var note = conn.Single<DBNote> ("Username = {0}", "overlong");
				
				Assert.AreEqual (20000, note.Text.Length);
				Assert.AreEqual (sample_note.Text, note.Text);
				Assert.AreEqual (sample_note.Title, note.Title);
			}
		}

		[Test]
		public void StoreLargeInsertTransaction ()
		{
			int num_samples = 250;
			var notes = GetDBSampleNotes (num_samples);

			using (var conn = dbFactory.OpenDbConnection ()) {
				using (var trans = conn.OpenTransaction ()) {
					conn.InsertAll (notes);
					trans.Commit ();
				}

				var db_notes = conn.Select<DBNote> ();
				Assert.AreEqual (num_samples, db_notes.Count);
			}
		}

		[Test]
		public void UpdateNote ()
		{
			var sample_note = GetDBSampleNote ();

			using (var conn = dbFactory.OpenDbConnection ()) {
				conn.Insert (sample_note);
				sample_note.Title = "changed title";
				conn.Update (sample_note);
			}
			using (var conn = dbFactory.OpenDbConnection ()) {
				var db_notes = conn.Select<DBNote> ();
				Assert.AreEqual (1, db_notes.Count);
				var db_note = db_notes.First ();

				Assert.AreEqual (sample_note.Guid, db_note.Guid);
				Assert.AreEqual (sample_note.Title, db_note.Title);
			}
		}

		[Test]
		public void DeleteNote ()
		{
			int num_samples = 100;
			var sample_notes = GetDBSampleNotes (num_samples);
			var delete_note1 = sample_notes.First ();
			var delete_note2 = sample_notes.Last ();

			using (var conn = dbFactory.OpenDbConnection ()) {
				conn.InsertAll (sample_notes);

				conn.Delete (delete_note1);
				var stored_notes = conn.Select<DBNote> ();
				// there should be now one note less in the db
				Assert.AreEqual (num_samples - 1, stored_notes.Count);

				// and the deleted note should not be there
				var result = conn.Select<DBNote> ("Guid = {0}", delete_note1.Guid);
				Assert.AreEqual (0, result.Count);

				conn.Delete<DBNote> (n => n.Guid == delete_note2.Guid);
				stored_notes = conn.Select<DBNote> ();
				Assert.AreEqual (num_samples - 2, stored_notes.Count);

				result = conn.Select<DBNote> ("Guid = {0}", delete_note1.Guid);
				Assert.AreEqual (0, result.Count);
			}
		}

		[Test]
		public void PendingTransaction ()
		{
			// make sure that inserted notes not yet commited can be
			// retrieved via .Select

			var sample_note = GetDBSampleNote (username: "user");

			using (var conn = dbFactory.OpenDbConnection ()) {
				using (var trans = conn.BeginTransaction ()) {
					conn.Insert (sample_note);
					// get the note before it was commited
					var db_note = conn.Single <DBNote> ("Username = {0}", "user");
					Assert.That (!ReferenceEquals (db_note, sample_note));
					Assert.AreEqual (sample_note.Guid, db_note.Guid);
			
					trans.Rollback ();
				}
			}
		}

		[Test]
		public void UpdateNonExistingNoteDoesNotWork ()
		{
			// test if we can update a non-existing note
			// (we assume we can't)

			var sample_note = GetDBSampleNote (username: "test");

			using (var conn = dbFactory.OpenDbConnection ()) {
				conn.Update (sample_note);
			}

			using (var conn = dbFactory.OpenDbConnection ()) {
				var result = conn.Select<DBNote> ("Username = {0}", "test");
				Assert.AreEqual (0, result.Count);
			}
		}

		[Test]
		public void DeleteNonExistingNote ()
		{
			// test if it is ok to delete a note that does not exist

			var sample_note = GetDBSampleNote (username: "test");
			
			using (var conn = dbFactory.OpenDbConnection ()) {
				conn.Delete (sample_note);
			}
			
			using (var conn = dbFactory.OpenDbConnection ()) {
				var result = conn.Select<DBNote> ("Username = {0}", "test");
				Assert.AreEqual (0, result.Count);
			}
		}

		[Test]
		public void DeleteNonExistingNoteDoesNotCancelTransaction ()
		{
			var sample_note = GetDBSampleNote (username: "test");
			
			using (var conn = dbFactory.OpenDbConnection ()) {
				using (var trans = conn.BeginTransaction ()) {
					conn.Delete (sample_note);
					conn.Insert (sample_note);

					trans.Commit ();
				}
			}
			
			using (var conn = dbFactory.OpenDbConnection ()) {
				var result = conn.Select<DBNote> ("Username = {0}", "test");
				Assert.AreEqual (1, result.Count);
			}
		}

		[Test]
		public void SaveAndReadUser ()
		{

			var user = new DBUser ();
			using (var conn = dbFactory.OpenDbConnection ()) {
				user.Username = "test";
				user.Manifest.ServerId = Guid.NewGuid ().ToString ();
				user.Manifest.LastSyncRevision = 123;
				user.Manifest.NoteRevisions.Add (Guid.NewGuid ().ToString (), 666);

				conn.Save (user);
			}
			using (var conn = dbFactory.OpenDbConnection ()) {
				var db_user = conn.First<DBUser> ("Username = {0}", "test");

				Assert.AreEqual (user.Manifest.ServerId, db_user.Manifest.ServerId);
				Assert.AreEqual (user.Manifest.NoteRevisions.First (), db_user.Manifest.NoteRevisions.First ());
				Assert.AreEqual (user.Manifest.LastSyncRevision, db_user.Manifest.LastSyncRevision);

			}
		}
	}

	[TestFixture]
	public class DbOauthTests : DbTestsBase
	{
		[Test]
		public void SaveAndReadTokenBase ()
		{
			var token = new TokenBase ();
			token.Token = Guid.NewGuid ().ToString ();
			token.TokenSecret = Guid.NewGuid ().ToString ();

			using (var conn = dbFactory.OpenDbConnection ()) {
				conn.Insert (token.ToDBAccessToken ());
			}
			using (var conn = dbFactory.OpenDbConnection ()) {
				var dbtoken = conn.Select<DBAccessToken> ().First ();
				Assert.AreEqual (token.Token, dbtoken.Token);
				Assert.AreEqual (token.TokenSecret, dbtoken.TokenSecret);
			}
		}

		[Test]
		public void DbTokenRepository ()
		{
			var repo = new DbAccessTokenRepository<AccessToken> ();

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
			Assert.AreEqual (token1.Token, token2.Token);
			Assert.AreEqual (token1.TokenSecret, token2.TokenSecret);
		}
	}
}
