using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using Woffler.Primitives;

namespace Woffler.ShareDestinations
{
	public class SlackDestination : IShareDestination
	{
		public void Share( ICollection<TrackManifest> trackManifests, Destination destination )
		{
			var formatter = new TrackFormatter( destination.Formatter, destination.User );

			foreach ( var manifest in trackManifests )
			{
				using ( var httpClient = new HttpClient() )
				{
					var postContent = formatter.Format( manifest );
					var response = httpClient.PostAsync( destination.ApiUrl, new StringContent( postContent, Encoding.UTF8, "application/json" ) ).Result;

					if ( !response.IsSuccessStatusCode )
					{
						throw new Exception( $"Error sharing tracks to Slack: {response.StatusCode}" );
					}
				}
			}
		}
	}
}
