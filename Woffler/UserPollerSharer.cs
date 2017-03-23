using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Timers;
using Woffler.PollingSources;
using Woffler.Primitives;
using Woffler.ShareDestinations;

namespace Woffler
{
	public class UserPollerSharer
	{
		public UserPollerSharer( User user )
		{
			_user = user;
			_sourcePollers = new Dictionary<Source, Timer>();
			_destinationTrackLists = new Dictionary<Destination, ConcurrentQueue<TrackManifest>>();
			_destinationLocks = new Dictionary<Destination, object>();
		}

		public void Start()
		{
			EventLog.WriteEntry( Constants.EventLogSourceName, $"Polling start for user {_user.Name}", EventLogEntryType.Information );
			foreach ( var source in _user.Sources )
			{
				var poller = new Timer( source.PollInterval * 1000 );
				poller.Elapsed += ( sender, e ) => PollAndShare( sender, e, source );
				poller.Start();
				_sourcePollers.Add( source, poller );
				EventLog.WriteEntry( Constants.EventLogSourceName, $"Poller for {source.Name} created", EventLogEntryType.Information );
			}

			foreach ( var destination in _user.Destinations )
			{
				_destinationTrackLists.Add( destination, new ConcurrentQueue<TrackManifest>() );
				_destinationLocks.Add( destination, new object() );
			}
		}

		public void Stop()
		{
			EventLog.WriteEntry( Constants.EventLogSourceName, $"Polling stop for user {_user.Name}", EventLogEntryType.Information );
			foreach ( var poller in _sourcePollers.Values )
			{
				poller.Stop();
				poller.Dispose();
			}
			_sourcePollers.Clear();
			_destinationTrackLists.Clear();
			_destinationLocks.Clear();
		}

		private void PollAndShare( object sender, ElapsedEventArgs e, Source source )
		{
			var pollingSource = GetPollingSource( source );
			try
			{
				EventLog.WriteEntry( Constants.EventLogSourceName, $"Poll attempt for {source.Name}", EventLogEntryType.Information );
				var trackManifests = pollingSource.Poll( source );
				if ( trackManifests.Any() )
				{
					EventLog.WriteEntry( Constants.EventLogSourceName, $"{trackManifests.ToList().Count} new tracks found", EventLogEntryType.Information );
				}
				source.LastPoll = DateTimeOffset.Now;
				foreach ( var destinationQueue in _destinationTrackLists.Values )
				{
					foreach ( var trackManifest in trackManifests )
					{
						destinationQueue.Enqueue( trackManifest );
					}
				}
			}
			catch ( Exception ex )
			{
				EventLog.WriteEntry( Constants.EventLogSourceName, $"Polling error: {ex.Message}", EventLogEntryType.Error );
			}

			foreach ( var destination in _user.Destinations )
			{
				var trackQueue = _destinationTrackLists[ destination ];
				if ( trackQueue.IsEmpty ) continue;

				lock ( _destinationLocks[ destination ] )
				// while in this block, concurrent queue adds fine, but concurrent dequeues need to wait
				{
					var shareDestination = GetShareDestination( destination );

					ICollection<TrackManifest> tracksToShare;
					if ( trackQueue.Count > destination.TrackLimit )
					{
						tracksToShare = trackQueue.ToList().GetRange( 0, destination.TrackLimit );
					}
					else
					{
						tracksToShare = new List<TrackManifest>( trackQueue );
					}

					try
					{
						EventLog.WriteEntry( Constants.EventLogSourceName, $"Share attempt for {source.Name}", EventLogEntryType.Information );
						shareDestination.Share( tracksToShare, destination );
						foreach ( var track in tracksToShare )
						{
							TrackManifest trackToDiscard;
							trackQueue.TryDequeue( out trackToDiscard );
						}
					}
					catch ( Exception ex )
					{
						EventLog.WriteEntry( Constants.EventLogSourceName, $"Error sharing tracks: {ex.Message}", EventLogEntryType.Error );
					}
				}
			}

		}

		private IPollingSource GetPollingSource( Source source )
		{
			if ( source.Name == "Last.FM" )
			{
				return new LastFmSource();
			}
			return null;
		}

		private IShareDestination GetShareDestination( Destination destination )
		{
			if ( destination.Name == "Slack" )
			{
				return new SlackDestination();
			}
			return null;
		}

		private readonly User _user;
		private readonly Dictionary<Source, System.Timers.Timer> _sourcePollers;
		private readonly Dictionary<Destination, ConcurrentQueue<TrackManifest>> _destinationTrackLists;
		private readonly Dictionary<Destination, object> _destinationLocks;
	}
}
