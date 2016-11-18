using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Woffler.Primitives;

namespace Woffler.ShareDestinations
{
	public interface IShareDestination
	{
		void Share(ICollection<TrackManifest> trackManifests, string userName, string formatString);
	}
}
