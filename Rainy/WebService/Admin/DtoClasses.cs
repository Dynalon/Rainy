using System.Runtime.Serialization;

namespace Rainy.UserManagement
{
	public class DTOUser
	{
		public virtual string Username { get; set; }

		public virtual string Password { get; set; }

		public string EmailAddress { get; set; }

		public string FirstName { get; set; }

		public string LastName { get; set; }
		
		public string AdditionalData { get; set; }
	}
}
