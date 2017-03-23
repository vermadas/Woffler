using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Woffler.Primitives
{
	public class Source
	{
		public string Name { get; set; }
		public string ApiKey { get; set; }
		public string ApiUrl { get; set; }
		public int PollInterval { get; set; }
		public int TrackLimit { get; set; }
		public string UserName { get; set; }
		public string UserPassword { get; set; }
		public DateTimeOffset LastPoll { get; set; }
	}
}
