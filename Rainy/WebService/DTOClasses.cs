using System;
using System.Runtime.Serialization;
using System.Collections.Generic;
using ServiceStack.ServiceHost;
using Rainy.WebService.OAuth;

namespace Rainy.WebService
{
	[Route("/{Username}/{Password}/api/1.0/")]
	public class ApiRequest : Tomboy.Sync.DTO.ApiRequest, IReturn<Tomboy.Sync.DTO.ApiResponse>
	{
		public string Username { get; set; }
		public string Password { get; set; }
	}

	[Route("/api/1.0/{Username}/")]
	[OAuthRequiredAttribute]
	public class UserRequest : IReturn<Tomboy.Sync.DTO.UserResponse>
	{
		public string Username { get; set; }
	}

	[Route("/api/1.0/{Username}/notes", "GET")]
	[OAuthRequiredAttribute]
	public class GetNotesRequest : IReturn<Tomboy.Sync.DTO.GetNotesResponse>
	{
		public string Username { get; set; }
	}

	[Route("/api/1.0/{Username}/notes", "PUT,POST")]
	[OAuthRequiredAttribute]
	[DataContract]
	public class PutNotesRequest : Tomboy.Sync.DTO.PutNotesRequest, IReturn<Tomboy.Sync.DTO.PutNotesResponse>
	{
		public string Username { get; set; }
	}

}