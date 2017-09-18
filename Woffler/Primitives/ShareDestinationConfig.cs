using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Woffler.Primitives
{
    public class ShareDestinationConfig
    {
		public int Id { get; set; }
		public string Name { get; set; }
		public string ApiKey { get; set; }
		public string ApiUrl { get; set; }
		public string DefaultFormatter { get; set; }
		public int DefaultTrackLimit { get; set; }
		public int ShareDestinationId { get; set; }
		public ShareDestination ShareDestination { get; set; }
    }
}
