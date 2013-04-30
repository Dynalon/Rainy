using System.Runtime.Serialization;
using ServiceStack.ServiceHost;
using Rainy.WebService.OAuth;
using DTO = Tomboy.Sync.Web.DTO;

namespace Rainy.WebService
{

	[Route("/api/1.0/", "GET")]
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

	[Route("/api/1.0/{Username}/", "GET")]
	[OAuthRequired]
	[DataContract]
	public class UserRequest : IReturn<DTO.UserResponse>
	{
		[DataMember (Name="Username")]
		public string Username { get; set; }
	}

	[Route("/api/1.0/{Username}/notes", "GET")]
	[OAuthRequired]
	[DataContract]
	public class GetNotesRequest : IReturn<DTO.GetNotesResponse>
	{
		[DataMember (Name="Username")]
		public string Username { get; set; }
	}

	[Route("/api/1.0/{Username}/notes", "PUT,POST")]
	[OAuthRequired]
	[DataContract]
	public class PutNotesRequest : DTO.PutNotesRequest, IReturn<DTO.GetNotesResponse>
	{
		[DataMember (Name="Username")]
		public string Username { get; set; }
	} 

}