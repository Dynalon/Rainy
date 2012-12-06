using System;
using System.Linq;
using System.Runtime.Serialization;
using System.Collections.Generic;
using ServiceStack.Common;
using ServiceStack.ServiceHost;
using System.IO;

namespace Rainy.WebService
{
	[Route("/{Username}/{Password}/api/1.0/")]
	public class ApiRequest : IReturn<ApiResponse>
	{
		public string Username { get; set; }
		public string Password { get; set; }
	}

	[DataContract]
	public class ApiResponse
	{
		[DataMember (Name = "user-ref")]
		public ContentRef UserRef { get; set; }

		[DataMember (Name = "oauth_request_token_url")]
		public string OAuthRequestTokenUrl { get; set; }

		[DataMember (Name ="oauth_authorize_url")]
		public string OAuthAuthorizeUrl { get; set; }

		[DataMember (Name ="oauth_access_token_url")]
		public string OAuthAccessTokenUrl { get; set; }

		[DataMember (Name = "api-version")]
		public string ApiVersion { get; set; }

	}

	[DataContract]
	public class ContentRef
	{
		[DataMember (Name = "api-ref")]
		public string ApiRef { get; set; }

		[DataMember (Name = "href")]
		public string Href { get; set; }

	}

	[DataContract]
	public class UserResponse
	{
		[DataMember (Name = "user-name")]
		public string Username { get; set; }

		[DataMember (Name = "first-name")]
		public string Firstname { get; set; }

		[DataMember (Name = "last-name")]
		public string Lastname { get; set; }

		[DataMember (Name = "notes-ref")]
		public ContentRef NotesRef { get; set; }

		[DataMember (Name = "latest-sync-revision")]
		public long LatestSyncRevision { get; set; }

		[DataMember (Name = "current-sync-guid")]
		public string CurrentSyncGuid { get; set; }
	}

	[DataContract]
	public class GetNotesResponse
	{
		[DataMember (Name ="latest-sync-revision")]
		public long LatestSyncRevision { get; set; }

		[DataMember (Name = "notes")]
		public IList<DTONote> Notes { get; set; }

	}

	[Route("/api/1.0/{Username}/notes", "PUT,POST")]
	[DataContract]
	public class PutNotesRequest : IReturn<GetNotesResponse>
	{
		[DataMember (Name = "Username")]
		public string Username { get; set; }

		[DataMember (Name = "latest-sync-revision")]
		public long? LatestSyncRevision { get; set; }

		[DataMember (Name = "note-changes")]
		public IList<DTONote> Notes { get; set; }

	}

	[DataContract]
	public class DTONote
	{
		private Tomboy.Note baseNote { get; set; }
		
		[DataMember (Name = "title")]
		public string Title { get; set; }
		
		[DataMember (Name = "note-content")]
		public string Text { get; set; }
		
		[DataMember (Name = "note-content-version")]
		public double NoteContentVersion {
			get {
				//return double.Parse (Tomboy.Reader.CURRENT_VERSION);
				return 0.3;
			}
		}

		public string Uri { get; set; }

		[DataMember (Name = "guid")]
		public string Guid {
			get {
				if (baseNote != null)
					return Tomboy.Utils.GetNoteFileNameFromURI (baseNote);
				else {
					return Uri.Replace ("note://tomboy/", "").Replace (".note", "");
				}
			}
			set {
				Uri = "note://tomboy/" + value + ".note";
			}
		}

		public DateTime CreateDate { get; set; }
		[DataMember (Name = "create-date")]
		public string CreateDateFormated {
			get {
				return CreateDate.ToString (Tomboy.Writer.DATE_TIME_FORMAT);
			}
			set {
				CreateDate = DateTime.Parse (value);
			}
		}

		public DateTime ChangeDate { get; set; }
		[DataMember (Name = "last-change-date")]
		public string LastChangeDateFormated {
			get {
				return ChangeDate.ToString (Tomboy.Writer.DATE_TIME_FORMAT);
			}
			set {
				ChangeDate = DateTime.Parse (value);
			}
		}

		public DateTime MetadataChangeDate { get; set; }
		[DataMember (Name = "last-metadata-change-date")]
		public string MetadataChangeDateFormated {
			get {
				return MetadataChangeDate.ToString (Tomboy.Writer.DATE_TIME_FORMAT);
			}
			set {
				MetadataChangeDate = DateTime.Parse (value);
			}
		}
	
		// TODO this is not stored via Tomboy-lib, we have to maintain our own Dict of Guid/LastSyncRevs
		[DataMember (Name = "last-sync-revision")]
		public int LastSyncRevision { get; set; }

		[DataMember (Name = "tags")]
		public string[] Tags {
			get {
				return new string[] { };
			}
			set {
			}
			
		}

		// note stored via Tomboy-lib, the command is a pure DTO field that tells us what to do with that note
		[DataMember (Name = "command")]
		public string Command { get; set; }

		// only ctor, a note needs to be passed along
		public DTONote ()
		{
		}
	}
}
