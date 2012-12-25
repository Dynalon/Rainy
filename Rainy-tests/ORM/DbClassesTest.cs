using System;
using Tomboy.Sync.DTO;
using NUnit.Framework;
using System.Data;
using ServiceStack.OrmLite;
using ServiceStack.Common;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using System.Linq;

namespace Rainy.Db
{
	[TestFixture]
	public class DbTestsBase
	{
		protected string connectionString = "rainy-test.db";
		protected OrmLiteConnectionFactory dbFactory;
		
		[TestFixtureSetUp]
		public void FixtureSetUp ()
		{
			// remove the rainy-test.db file if it exists
			if (File.Exists ("rainy-test.db")) {
				File.Delete ("rainy-test.db");
			}

			dbFactory = new OrmLiteConnectionFactory (connectionString, SqliteDialect.Provider);
			

		}
		
		[SetUp]
		public void SetUp ()
		{
			// Start with empty tables in each test run
			using (var c = dbFactory.OpenDbConnection ()) {
				c.DropAndCreateTable <DBNote> ();
				c.DropAndCreateTable <DBUser> ();
			}
		}

		[TearDown]
		public void TearDown ()
		{
		}
		
		protected DTONote GetDTOSampleNote ()
		{
			return new DTONote () {
				Title = "My sämple title",
				Text = "My sample text",
				CreateDate = DateTime.Now.ToString (Tomboy.Writer.DATE_TIME_FORMAT),
				MetadataChangeDate = DateTime.Now.ToString (Tomboy.Writer.DATE_TIME_FORMAT),
				ChangeDate = DateTime.Now.ToString (Tomboy.Writer.DATE_TIME_FORMAT),
				OpenOnStartup = true,
				Pinned = false,
				Tags = new string[] { "school", "fun", "shopping" },
				Guid = Guid.NewGuid ().ToString ()
			};
		}
		protected DBNote GetDBSampleNote (string username = "test")
		{
			var db_note = GetDTOSampleNote ().ToDBNote ();
			db_note.Username = username;
			return db_note;
		}
		protected List<DTONote> GetDTOSampleNotes (int num, string username = "test")
		{
			var notes = new List<DTONote> ();

			for (int i=0; i < num; i++) {
				notes.Add (GetDTOSampleNote ());
			}
			return notes;
		}
		protected List<DBNote> GetDBSampleNotes (int num, string username = "test")
		{
			var notes = new List<DBNote> ();

			for (int i=0; i < num; i++) {
				notes.Add (GetDBSampleNote (username));
			}

			return notes;
		}
	}

	[TestFixture]
	public class BasicTests : DbTestsBase
	{
		[Test]
		public void StoreAndRetrieveNote ()
		{
			var db_old = GetDBSampleNote ();

			db_old.Username = "test";
			
			using (var conn = dbFactory.OpenDbConnection ()) {
				conn.Insert (db_old);
			}
			DTONote dto_new;
			DBNote db_new;
			using (var conn = dbFactory.OpenDbConnection ()) {
				db_new = conn.Single <DBNote> ("Username = {0}", "test");
			}
			
			dto_new.PopulateWith (db_new);
			
			// check for equalness
			Assert.AreEqual (db_old.Title, db_new.Title);
			Assert.AreEqual (db_old.Text, db_new.Text);
			
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
	}

	[TestFixture]
	public class Benchmarks : DbTestsBase
	{
		private int num_runs = 10;
		private int num_notes = 200;

		[SetUp]
		public new void SetUp ()
		{
		}

		protected void Run (string name, Action bench)
		{
			// helper to measure the benchmarks runs
			long total = 0;
			var sw = new Stopwatch ();

			for (int i=0; i < num_runs; i++) {
				sw.Restart ();
				// call the actual benchmark function
				bench ();
				sw.Stop ();
				total += sw.ElapsedMilliseconds;
				Console.WriteLine ("{0} run {1} took: {2}ms", name, i, sw.ElapsedMilliseconds);
			}
			float avg = (float)total / (float)num_runs;
			Console.WriteLine ("{0} TOTAL average: {1}ms", name, avg);
		}

		[Test]
		public void InsertBenchmark_SingleThreaded ()
		{
			var notes = GetDBSampleNotes (num_notes);
			Run("Insert_SingleThreaded", () => {
				InsertBenchmark_Worker (notes);
			});
		}
		protected void InsertBenchmark_Worker (List<DBNote> notes)
		{
			// now insert the notes
			using (var conn = dbFactory.OpenDbConnection ()) {
				using (var trans = conn.OpenTransaction ()) {
					foreach (var note in notes) {
						conn.Insert (note);
					}
				}
			}
		}
		
		[Test]
		public void InsertBenchmark_MultiThreaded ()
		{
			Run("Insert_MultiThreaded", () => {
				InsertBenchmark_MultiThreadedWorker (16, num_notes);
			});
		}
		
		private void InsertBenchmark_MultiThreadedWorker (int num_threads, int num_notes)
		{
			Task[] tasks = new Task[num_threads];
			
			List<DBNote>[] notes = new List<DBNote>[num_threads];
			for (int j=0; j < num_threads; j++) {
				notes[j] = GetDBSampleNotes (num_notes);
			}
			

			for (int i=0; i< num_threads; i++) {
				var c = i;
				tasks[i] = Task.Factory.StartNew (() => {
					InsertBenchmark_Worker (notes[c]);
				});
			}
			Task.WaitAll (tasks);
		}
		

	}
}

