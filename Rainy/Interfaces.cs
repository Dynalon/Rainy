using System;
using Tomboy.Sync;
using Rainy.OAuth;

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
		OAuthHandlerBase OAuth { get; }
	}
}
