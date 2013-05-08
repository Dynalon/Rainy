using System;
using NUnit.Framework;
using System.Web.Routing;
using Rainy.UserManagement;
using ServiceStack.ServiceHost;
using Rainy.WebService.Admin;

namespace Rainy.WebService.Management
{
	//-- ADMIN INTERFACE
	[AdminPasswordRequired]
	[Route("/api/admin/user/","POST, PUT")]
	[Route("/api/admin/user/{Username}","GET, DELETE, OPTIONS")]
	public class UserRequest : DTOUser, IReturn<DTOUser>
	{
	}
	
	[AdminPasswordRequired]
	[Route("/api/admin/alluser/","GET")]
	public class AllUserRequest : IReturn<DTOUser[]>
	{
	}

	//-- USER INTERFACE

	// allows user to update hisself user data (especially the password)
	[Route("/api/user/{Username}/", "PUT")]
	public class UpdateUserRequest : DTOUser, IReturn<DTOUser>
	{
	}

	// allows anyone to register a new account
	[Route("/api/user/signup/new/", "POST")]
	public class SignupUserRequest : DTOUser, IReturnVoid
	{
	}

	// email double-opt in verifiycation
	[Route("/api/user/signup/verify/{VerifySecret}/", "GET")]
	public class VerifyUserRequest : IReturnVoid
	{
		public string VerifySecret { get; set; }
	}

	// allows admin to activate users if signup requires moderation
	[AdminPasswordRequired]
	[Route("/api/user/signup/activate/{Username}/", "POST")]
	public class ActivateUserRequest : IReturnVoid
	{
		public string Username { get; set; }
	}

	// list all pending users that await activation/moderation
	[AdminPasswordRequired]
	[Route("/api/user/signup/pending/", "GET")]
	public class PendingActivationsRequest : IReturn<DTOUser[]>
	{
	}

}