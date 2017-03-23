using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Woffler.Primitives
{
	public class Destination
	{
		public string Name { get; set; }
		public string ApiKey { get; set; }
		public string ApiUrl { get; set; }
		public string Formatter { get; set; }
		public int TrackLimit { get; set; }
		public string User { get; set; }
		public string TrackUrlProvider { get; set; }
		public string ImageUrlProvider { get; set; }
	}
}
