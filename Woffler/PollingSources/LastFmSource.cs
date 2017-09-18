using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Woffler.Primitives;

namespace Woffler.PollingSources
{
	public class LastFmSource : IPollingSource
	{
		public ICollection<TrackManifest> Poll( UserSource userSource )
		{
			var parameters = "?method=user.getrecenttracks" +
							  "&user=" + userSource.SourceUserName +
							  "&api_key=" + userSource.SourceConfig.ApiKey +
							  "&from=" + ConvertToUnixTime(userSource.LastPoll ) +
							  "&limit=" + ( userSource.TrackLimit ?? userSource.SourceConfig.DefaultTrackLimit );
			var xmlDoc = new XmlDocument();

			using ( var httpClient = new HttpClient() )
			{
				httpClient.BaseAddress = new Uri( userSource.SourceConfig.ApiUrl );
				httpClient.DefaultRequestHeaders.Accept.Add( new MediaTypeWithQualityHeaderValue( "application/json" ) );

				var response = httpClient.GetAsync( parameters ).Result;

				if ( response.IsSuccessStatusCode )
				{
					var responseStr = response.Content.ReadAsStringAsync().Result;
					xmlDoc.LoadXml( responseStr );
				}
				else
				{
					throw new Exception( $"Error getting scrobbled tracks from Last.FM: {response.StatusCode}" );
				}
			}

			return ParseLastFmXmlResponse( xmlDoc );
		}

		private long ConvertToUnixTime( DateTimeOffset dateTimeOffset )
		{
			return dateTimeOffset.ToUnixTimeSeconds();
		}

		private ICollection<TrackManifest> ParseLastFmXmlResponse( XmlDocument xmlDoc )
		{
			var trackManifests = new List<TrackManifest>();

			foreach ( XmlNode trackNode in xmlDoc.SelectNodes( "/lfm/recenttracks/track" ) )
			{
				var utcTimeAttribute = trackNode.SelectSingleNode( "date/@uts" )?.Value;
				DateTime? listenTime = null;
				if ( utcTimeAttribute != null )
				{
					var utcTimeInt = long.Parse( utcTimeAttribute );
					listenTime = DateTimeOffset.FromUnixTimeSeconds( utcTimeInt ).LocalDateTime;
				}
				trackManifests.Add( new TrackManifest()
				{
					Album = trackNode.SelectSingleNode( "album" )?.InnerText,
					Name = trackNode.SelectSingleNode( "name" )?.InnerText,
					Artist = trackNode.SelectSingleNode( "artist" )?.InnerText,
					Url = trackNode.SelectSingleNode( "url" )?.InnerText,
					AlbumArtUrl = trackNode.SelectSingleNode( "image[@size='large']" )?.InnerText,
					ListenTime = listenTime
				} );
			}

			return trackManifests;
		}
	}
}
