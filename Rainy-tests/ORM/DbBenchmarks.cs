using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.OrmLite;
using Rainy.Tests.Db;
using Rainy.Db;
using Tomboy.Db;

namespace Rainy.Tests.Benchmarks
{
	public abstract class DbBenchmarks : DbTestsBase
	{
		private int num_runs = 10;
		private int num_notes = 200;
		private int num_threads = 16;

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
			Run("Insert_SingleThreaded", () => {
				var notes = GetDBSampleNotes (num_notes);
				InsertBenchmark_Worker (notes);
			});
		}
		protected void InsertBenchmark_Worker (List<DBNote> notes)
		{
			// now insert the notes
			using (var conn = connFactory.OpenDbConnection ()) {
				using (var trans = conn.OpenTransaction ()) {
					foreach (var note in notes) {
						conn.Insert<DBNote> (note);
					}
					trans.Commit ();
				}
			}
		}
		
		[Test]
		public void InsertBenchmark_MultiThreaded ()
		{
			Run("Insert_MultiThreaded", () => {
				InsertBenchmark_MultiThreadedWorker (num_threads, num_notes);
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
