using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Woffler.Primitives
{
	public class UserSource
	{
		public int Id { get; set; }
		public bool Active { get; set; }
		public string SourceUserName { get; set; }
		public string SourceUserPassword { get; set; }
		public int? PollInterval { get; set; }
		public int? TrackLimit { get; set; }
		public DateTimeOffset LastPoll { get; set; }
		public int UserId { get; set; }
		public int SourceConfigId { get; set; }
		public SourceConfig SourceConfig { get; set; }
    }
}
