using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Microsoft.AspNetCore.Hosting.WindowsServices;

namespace Woffler
{
    public class Program
    {
        public static void Main(string[] args)
        {
			bool isService = true;
			var pathToContentRoot = Directory.GetCurrentDirectory();

			if ( Debugger.IsAttached || args.Contains( "--console" ) )
			{
				isService = false;
			}
			else
			{
				var pathToExe = Process.GetCurrentProcess().MainModule.FileName;
				pathToContentRoot = Path.GetDirectoryName(pathToExe);
			}

			var host = new WebHostBuilder()
				.UseKestrel()
				.UseContentRoot(pathToContentRoot)
				.UseStartup<Startup>()
				.Build();

			if ( isService )
			{
				host.RunAsWofflerService();
			}
			else
			{
				host.Run();
			}
		}
    }
}
