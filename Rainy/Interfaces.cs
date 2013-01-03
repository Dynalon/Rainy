using System;
using Tomboy.Sync;

namespace Rainy
{
	public interface INoteRepository : IDisposable
	{
		Tomboy.Engine Engine { get; }
		SyncManifest Manifest { get; }
	}

	public interface IDataBackend 
	{
		INoteRepository GetNoteRepository (string username);
	}
}
