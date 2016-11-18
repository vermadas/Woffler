using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Woffler.Primitives;

namespace Woffler
{
	public class PollingProcessor
	{
		public ICollection<User> Users { get; set; }

		public PollingProcessor()
		{
			_pollers = new List<UserPollerSharer>();
		}

		public void Start()
		{
			EventLog.WriteEntry( Constants.EventLogSourceName, "PollingProcessor Start", EventLogEntryType.Information );
			foreach ( var user in Users )
			{
				var poller = new UserPollerSharer( user );
				poller.Start();
				_pollers.Add( poller );
			}
		}

		public void Stop()
		{
			EventLog.WriteEntry( Constants.EventLogSourceName, "PollingProcessor stop", EventLogEntryType.Information );
			foreach (var poller in _pollers)
			{
				poller.Stop();
			}
			_pollers.Clear();
		}

		private readonly ICollection<UserPollerSharer> _pollers;
	}
}
