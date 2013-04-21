using System.Runtime.Serialization;

namespace Rainy.UserManagement
{
	public class DTOUser
	{
		public string Username { get; set; }

		public string Password { get; set; }

		public string EmailAddress { get; set; }
		
		public string AdditionalData { get; set; }
	}
}
