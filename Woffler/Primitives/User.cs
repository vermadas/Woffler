using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Woffler.Primitives
{
	public class User
	{
		public User()
		{
			UserSources = new List<UserSource>();
			UserDestinations = new List<UserShareDestination>();
		}
		public int Id { get; set; }
		public string Name { get; set; }
		public string Email { get; set; }
		public bool Active { get; set; }
		public ICollection<UserSource> UserSources { get; set; }
		public ICollection<UserShareDestination> UserDestinations { get; set; }
	}
}
