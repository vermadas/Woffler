using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Woffler.Primitives;

namespace Woffler.ShareDestinations
{
	public class SlackCeDestination : IShareDestination
	{
		public void Share(ICollection<TrackManifest> trackManifests, string userName, string formatString)
		{
			var formatter = new TrackFormatter(formatString, userName);

			foreach (var manifest in trackManifests)
			{
				using ( var httpClient = new HttpClient() )
				{
					var postContent = formatter.Format(manifest);
					var response = httpClient.PostAsync(SlackApiUrl, new StringContent(postContent, Encoding.UTF8, "application/json")).Result;

					if ( !response.IsSuccessStatusCode )
					{
						throw new Exception( $"Error sharing tracks to Slack: {response.StatusCode}");
					}
				}
			}
		}

		private const string SlackApiUrl = "https://hooks.slack.com/services/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";
	}
}
