using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Woffler.Primitives
{
    public class UserShareDestination
    {
		public int Id { get; set; }
		public bool Active { get; set; }
		public string ShareUserName { get; set; }
		public int? TrackLimit { get; set; }
		public string Formatter { get; set; }
		public string TrackUrlProvider { get; set; }
		public string ImageUrlProvider { get; set; }
		public int UserId { get; set; }
		public int ShareDestinationConfigId { get; set; }
		public ShareDestinationConfig ShareDestinationConfig { get; set; }
    }
}
