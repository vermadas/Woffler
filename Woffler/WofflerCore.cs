using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Woffler.Database;

namespace Woffler
{
	public class WofflerCore
	{
		public WofflerCore()
		{
			// Empty
		}

		public void StartMonitor()
		{
			EventLog.WriteEntry(Constants.EventLogSourceName, "Monitor start", EventLogEntryType.Information);
			try
			{
				_databaseHandler = new DatabaseHandler();
				var users = _databaseHandler.QueryUsers();
				_pollingProcessor = new PollingProcessor { Users = users };
				_pollingProcessor.Start();
			}
			catch ( Exception e )
			{
				EventLog.WriteEntry(Constants.EventLogSourceName, e.ToString(), EventLogEntryType.Error);
			}
		}

		public void StopMonitor()
		{
			EventLog.WriteEntry( Constants.EventLogSourceName, "Monitor stop", EventLogEntryType.Information );
			_pollingProcessor.Stop();
			_databaseHandler.Dispose();
		}

		private DatabaseHandler _databaseHandler;
		private PollingProcessor _pollingProcessor;
	}
}
