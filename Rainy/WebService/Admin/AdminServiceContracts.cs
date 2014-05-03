using Rainy.UserManagement;
using Rainy.WebService.Admin;
using ServiceStack.ServiceHost;
using System.Collections.Generic;

namespace Rainy.WebService.Management
{
	//-- ADMIN INTERFACE
	[AdminPasswordRequired]
	[Route("/api/admin/user/","POST, PUT",
	       Summary = "Creates/Updates a user's details. Admin authentication is required",
	       Notes = "Note that passwords can't be changed due to encryption, thus the password field will be ignored.")]
	[Route("/api/admin/user/{Username}","GET, DELETE, OPTIONS",
	       Summary = "Gets user details or delete a user")]
	public class UserRequest : DTOUser, IReturn<DTOUser>
	{
	}

	[AdminPasswordRequired]
	[Route("/api/admin/alluser/","GET",
	       Summary = "Gets a list of all users registered and their user details")]
	public class AllUserRequest : IReturn<List<DTOUser>>
	{
	}
	
	// allows admin to activate users if signup requires moderation
	[AdminPasswordRequired]
	[Route("/api/user/signup/activate/{Username}/", "POST",
	       Summary = "Activates a user if moderation is required. Requires admin authentication.")]
	public class ActivateUserRequest : IReturnVoid
	{
		public string Username { get; set; }
	}

	// list all pending users that await activation/moderation
	[AdminPasswordRequired]
	[Route("/api/user/signup/pending/", "GET",
	       Summary = "Get a list of all pending users that await moderation.")]
	public class PendingActivationsRequest : IReturn<DTOUser[]>
	{
	}

	//-- USER INTERFACE

	// allows user to update hisself user data (especially the password)
	[Route("/api/user/{Username}/", "PUT",
	       Summary = "Update a users information. can only be called by the user himself (credentials required)")]
	public class UpdateUserRequest : DTOUser, IReturn<DTOUser>
	{
	}

	// check if a username is available
	public class CheckUsernameResponse
	{
		public bool Available { get; set; }
		public string Username { get; set; }
	}
	[Route("/api/user/signup/check_username/{Username}", "GET",
	       Summary = "Simple request that checks if a username is already taken or not. Taken usernames can not be used for signup.")]
	public class CheckUsernameRequest : IReturn<CheckUsernameResponse>
	{
		public string Username { get; set; }
	}

	// allows anyone to register a new account
	[Route("/api/user/signup/new/", "POST",
	       Summary = "Signup a new user. Everyone can use this signup as long as it is enabled by the server config")]
	public class SignupUserRequest : DTOUser, IReturnVoid
	{
	}

	// email double-opt in verifiycation
	[Api]
	[Route("/api/user/signup/verify/{VerifySecret}/", "GET",
	       Summary = "Verify a secret that is send to the user by mail for email verification.")]
	public class VerifyUserRequest : IReturnVoid
	{
		[ApiMember(Description="The verification key that is sent by email")]
		public string VerifySecret { get; set; }
	}


}