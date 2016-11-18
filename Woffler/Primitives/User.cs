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
			Sources = new List<Source>();
			Destinations = new List<Destination>();
		}

		public string Name { get; set; }
		public string Email { get; set; }
		public ICollection<Source> Sources { get; set; }
		public ICollection<Destination> Destinations { get; set; }
	}
}
