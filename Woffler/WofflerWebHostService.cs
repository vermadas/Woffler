using Microsoft.AspNetCore.Hosting.WindowsServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using System.Diagnostics;
using System.ServiceProcess;

namespace Woffler
{
	public class WofflerWebHostService : WebHostService
	{
		public WofflerWebHostService(IWebHost host) : base(host)
		{
			_wofflerCore = new WofflerCore();
		}

		protected override void OnStarting(string[] args)
		{
			base.OnStarting(args);
			if (!EventLog.SourceExists(Constants.EventLogSourceName))
			{
				EventLog.CreateEventSource(Constants.EventLogSourceName, "Application");
			}
			_wofflerCore.StartMonitor();
		}

		protected override void OnStarted()
		{
			base.OnStarted();
		}

		protected override void OnStopping()
		{
			base.OnStopping();
			_wofflerCore.StopMonitor();
		}

		private readonly WofflerCore _wofflerCore;
	}

	public static class WebHostServiceExtensions
	{
		public static void RunAsWofflerService( this IWebHost host )
		{
			var webHostService = new WofflerWebHostService(host);
			ServiceBase.Run(webHostService);
		}
	}
}
