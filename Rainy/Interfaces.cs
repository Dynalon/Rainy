using System;
using Tomboy.Sync;
using Rainy.OAuth;

namespace Rainy.Interfaces
{
	public interface IAuthenticator
	{
		bool VerifyCredentials (string username, string password);
	}
	public interface IAdminAuthenticator
	{
		bool VerifyAdminPassword (string password);
	}

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
