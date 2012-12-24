using System;

namespace Rainy
{
	public interface INoteRepository : IDisposable
	{
		Tomboy.Engine Engine { get; }
		NoteManifest Manifest { get; }
		//Dictionary<string, int> NoteRevisions { get; set; }
	}

	public interface IDataBackend 
	{
		INoteRepository GetNoteRepository (string username);
	}
}
