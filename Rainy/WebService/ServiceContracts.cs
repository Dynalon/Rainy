using System.Runtime.Serialization;
using ServiceStack.ServiceHost;
using Rainy.WebService.OAuth;

namespace Rainy.WebService
{

	[Route("/api/1.0/")]
	[Route("/{Username}/{Password}/api/1.0/")]
	// TODO check if we can remove the DataContract attributes
	[DataContract]
	public class ApiRequest : Tomboy.Sync.DTO.ApiRequest, IReturn<Tomboy.Sync.DTO.ApiResponse>
	{
		[DataMember (Name="Username")]
		public string Username { get; set; }
		[DataMember (Name="Password")]
		public string Password { get; set; }
	}

	[Route("/api/1.0/{Username}/")]
	[OAuthRequired]
	[DataContract]
	public class UserRequest : IReturn<Tomboy.Sync.DTO.UserResponse>
	{
		[DataMember (Name="Username")]
		public string Username { get; set; }
	}

	[Route("/api/1.0/{Username}/notes", "GET")]
	[OAuthRequired]
	[DataContract]
	public class GetNotesRequest : IReturn<Tomboy.Sync.DTO.GetNotesResponse>
	{
		[DataMember (Name="Username")]
		public string Username { get; set; }
	}

	[Route("/api/1.0/{Username}/notes", "PUT,POST")]
	[OAuthRequired]
	[DataContract]
	public class PutNotesRequest : Tomboy.Sync.DTO.PutNotesRequest, IReturn<Tomboy.Sync.DTO.GetNotesResponse>
	{
		[DataMember (Name="Username")]
		public string Username { get; set; }
	} 

}