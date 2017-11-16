using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Woffler
{
	public sealed class Constants
	{
		public const string EventLogSourceName = "WofflerService";
	}

	public sealed class DbTables
	{
		public const string Users = "Users";
		public const string Sources = "Sources";
		public const string SourceConfigs = "Source_Configs";
		public const string UserHasSource = "User_Has_Source";
		public const string ShareDestinations = "Share_Destinations";
		public const string ShareDestinationConfigs = "Share_Destination_Configs";
		public const string UserHasShareDestination = "User_Has_Share_Destination";
		public const string Version = "Version";
	}
}
