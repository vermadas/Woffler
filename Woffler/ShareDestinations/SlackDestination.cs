using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using Woffler.Primitives;

namespace Woffler.ShareDestinations
{
	public class SlackDestination : IShareDestination
	{
		public void Share( ICollection<TrackManifest> trackManifests, UserShareDestination userDestination )
		{
			var formatter = new TrackFormatter((userDestination.Formatter ?? userDestination.ShareDestinationConfig.DefaultFormatter), userDestination.ShareUserName );

			foreach ( var manifest in trackManifests )
			{
				using ( var httpClient = new HttpClient() )
				{
					var postContent = formatter.Format( manifest );
					var response = httpClient.PostAsync( userDestination.ShareDestinationConfig.ApiUrl, new StringContent( postContent, Encoding.UTF8, "application/json" ) ).Result;

					if ( !response.IsSuccessStatusCode )
					{
						throw new Exception( $"Error sharing tracks to Slack: {response.StatusCode}" );
					}
				}
			}
		}
	}
}
