using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace Woffler
{
	public partial class WofflerService : ServiceBase
	{
		public WofflerService()
		{
			InitializeComponent();
			_wofflerCore = new WofflerCore();
		}

		protected override void OnStart( string[] args )
		{
			if (!EventLog.SourceExists(Constants.EventLogSourceName))
			{
				EventLog.CreateEventSource(Constants.EventLogSourceName, "Application");
			}
			_wofflerCore.StartMonitor();
		}

		protected override void OnStop()
		{
			_wofflerCore.StopMonitor();
		}

		private readonly WofflerCore _wofflerCore;
	}
}
