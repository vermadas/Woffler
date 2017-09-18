using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Woffler.Primitives
{
    public class SourceConfig
    {
		public int Id { get; set; }
		public string Name { get; set; }
		public string ApiKey { get; set; }
		public string ApiUrl { get; set; }
		public int DefaultPollInterval { get; set; }
		public int DefaultTrackLimit { get; set; }
		public int SourceId { get; set; }
		public Source Source { get; set; }
    }
}
