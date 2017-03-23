﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Woffler.Primitives;

namespace Woffler.PollingSources
{
	public interface IPollingSource
	{
		ICollection<TrackManifest> Poll( Source source );
	}
}
