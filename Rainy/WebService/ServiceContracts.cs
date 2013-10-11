using System.Runtime.Serialization;
using ServiceStack.ServiceHost;
using Rainy.WebService.OAuth;
using DTO = Tomboy.Sync.Web.DTO;
using Tomboy.Sync.Web.DTO;
using System.Collections.Generic;

namespace Rainy.WebService
{

	[Api ("Sync API")]
	[Route("/api/1.0/", "GET",
	       Summary = "Root API entrance. Supplies all information for further requests",
	       Notes = "see the REST specification at https://wiki.gnome.org/Apps/Tomboy/Synchronization/REST/1.0")]
	[Route("/{Username}/{Password}/api/1.0/", "GET")]
	// TODO check if we can remove the DataContract attributes
	[DataContract]
	public class ApiRequest : DTO.ApiRequest, IReturn<DTO.ApiResponse>
	{
		[DataMember (Name="Username")]
		public string Username { get; set; }
		[DataMember (Name="Password")]
		public string Password { get; set; }
	}

	[Api ("Sync API")]
	[Route("/api/1.0/{Username}/", "GET",
	       Summary = "Get user details for sync",
	       Notes = "Requires OAuth authentication beforehand. See the REST specification at https://wiki.gnome.org/Apps/Tomboy/Synchronization/REST/1.0")]
	[OAuthRequired]
	[DataContract]
	public class UserRequest : IReturn<DTO.UserResponse>
	{
		[DataMember (Name="Username")]
		[ApiMember (Description="The username to get the notes from. ")]
		public string Username { get; set; }
	}

	[Route("/api/1.0/{Username}/notes", "GET",
	       Summary = "Get notes of a user",
	       Notes = "Requires OAuth authentication beforehand. See the REST specification at https://wiki.gnome.org/Apps/Tomboy/Synchronization/REST/1.0")]
	[OAuthRequired]
	[DataContract]
	public class GetNotesRequest : IReturn<DTO.GetNotesResponse>
	{
		[DataMember (Name="Username")]
		[ApiMember (Description="The username to get the notes from. Requires OAuth authentication beforehand.")]
		public string Username { get; set; }
	}

	[Route("/api/1.0/{Username}/notes/{Guid}", "GET", Summary = "Get a single note frm a a user")]
	[OAuthRequired]
	public class GetSingleNoteRequest : IReturn<DTO.GetSingleNoteResponse>
	{
		[DataMember (Name="Username")]
		public string Username { get; set; }
		[DataMember (Name="Guid")]
		public string Guid { get; set; }
	}


	[Route("/api/1.0/{Username}/notes", "PUT,POST",
	       Summary = "Update notes of a user",
	       Notes = "Requires OAuth authentication beforehand. See the REST specification at https://wiki.gnome.org/Apps/Tomboy/Synchronization/REST/1.0")]
	[OAuthRequired]
	[DataContract]
	public class PutNotesRequest : DTO.PutNotesRequest, IReturn<DTO.GetNotesResponse>
	{
		[DataMember (Name="Username")]
		public string Username { get; set; }
	}


	// NOTE HISTORY API
	[DataContract]
	public class NoteHistory
	{
		[DataMember (Name="revision")]
		public long Revision { get; set; }

		[DataMember (Name="note")]
		public DTONote Note { get; set; }
	}

	public class NoteHistoryResponse
	{
		[DataMember (Name="current-revision")]
		public long CurrentRevision { get; set; }

		[DataMember (Name="versions")]
		public NoteHistory[] Versions { get; set; }
	}


	[Route("/api/1.0/{Username}/notes/archive/{Guid}/", "GET",
	       Summary = "Retrieves the list of archived notes with full note data fields, but empty note text.")]
	[DataContract]
	public class GetNoteHistoryRequest : IReturn<NoteHistoryResponse>
	{
		[DataMember (Name="Username")]
		public string Username { get; set; }

		[DataMember (Name="Guid")]
		public string Guid { get; set; }
	}

	[Route("/api/1.0/{Username}/notes/archive/{Guid}/{Revision}/", "GET",
	       Summary = "Retrieves a specific version of a note including the note text.")]
	[DataContract]
	public class GetArchivedNoteRequest : IReturn<DTONote>
	{
		[DataMember (Name="Username")]
		public string Username { get; set; }

		[DataMember (Name="Guid")]
		public string Guid { get; set; }

		[DataMember (Name="Revision")]
		public long Revision { get; set; }
	}

	// lists all notes we have a history (current and deleted ones), and a list of revisions we know
	// and the number of and are not anymore in the latest sync
	// (so the note is likely to have gotten deleted)

	[DataContract]
	public class NoteArchiveResponse
	{
		[DataMember (Name="guids")]
		public string[] Guids { get; set; }
		[DataMember (Name="versions")]
		public DTONote[] Notes { get; set; }
	}

	[Route("/api/1.0/{Username}/notes/archive/all",
	       Summary = "Retrieves a list of all known Guids, including notes that have been deleted previously")]
	[DataContract]
	public class GetNoteArchiveRequest : IReturn<NoteArchiveResponse>
	{
		[DataMember (Name="Username")]
		public string Username { get; set; }
	}


	// NOTE SHARING API
	//

	// note that this url is NOT the url that should be passed along, but rather the url that the service returns should.
	[Route("/api/1.0/{Username}/notes/public/{Guid}/",
	       Summary = "Retrieves a public, shareable url to the note with the embedded encryption key as a parameter (if required).")]
	[DataContract]
	public class GetPublicUrlForNote : IReturn<string>
	{
		[DataMember (Name="Username")]
		public string Username { get; set; }

		[DataMember (Name="Guid")]
		public string Guid { get; set; }
	}
}