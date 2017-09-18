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
			_sourcePollers = new Dictionary<UserSource, Timer>();
			_destinationTrackLists = new Dictionary<UserShareDestination, ConcurrentQueue<TrackManifest>>();
			_destinationLocks = new Dictionary<UserShareDestination, object>();
		}

		public void Start()
		{
			EventLog.WriteEntry( Constants.EventLogSourceName, $"Polling start for user {_user.Name}", EventLogEntryType.Information );
			foreach ( var userSource in _user.UserSources )
			{
				var poller = new Timer((userSource.PollInterval ?? userSource.SourceConfig.DefaultPollInterval) * 1000 );
				poller.Elapsed += ( sender, e ) => PollAndShare( sender, e, userSource );
				poller.Start();
				_sourcePollers.Add( userSource, poller );
				EventLog.WriteEntry( Constants.EventLogSourceName, $"Poller for {userSource.SourceConfig.Name} created", EventLogEntryType.Information );
			}

			foreach ( var userDestination in _user.UserDestinations )
			{
				_destinationTrackLists.Add(userDestination, new ConcurrentQueue<TrackManifest>() );
				_destinationLocks.Add(userDestination, new object() );
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

		private void PollAndShare( object sender, ElapsedEventArgs e, UserSource userSource )
		{
			var pollingSource = GetPollingSource(userSource.SourceConfig.Source);
			try
			{
				EventLog.WriteEntry( Constants.EventLogSourceName, $"Poll attempt for {userSource.SourceConfig.Name}", EventLogEntryType.Information );
				var trackManifests = pollingSource.Poll(userSource);
				if ( trackManifests.Any() )
				{
					EventLog.WriteEntry( Constants.EventLogSourceName, $"{trackManifests.ToList().Count} new tracks found", EventLogEntryType.Information );
				}
				userSource.LastPoll = DateTimeOffset.Now;
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

			foreach ( var destination in _user.UserDestinations )
			{
				var trackQueue = _destinationTrackLists[ destination ];
				if ( trackQueue.IsEmpty ) continue;

				lock ( _destinationLocks[ destination ] )
				// while in this block, concurrent queue adds fine, but concurrent dequeues need to wait
				{
					var shareDestination = GetShareDestination( destination.ShareDestinationConfig.ShareDestination );

					ICollection<TrackManifest> tracksToShare;
					if ( trackQueue.Count > destination.TrackLimit )
					{
						tracksToShare = trackQueue.ToList().GetRange( 0, ( destination.TrackLimit ?? destination.ShareDestinationConfig.DefaultTrackLimit) );
					}
					else
					{
						tracksToShare = new List<TrackManifest>( trackQueue );
					}

					try
					{
						EventLog.WriteEntry( Constants.EventLogSourceName, $"Share attempt for {destination.ShareDestinationConfig.Name}", EventLogEntryType.Information );
						shareDestination.Share( tracksToShare, destination );
						foreach ( var track in tracksToShare )
						{
							TrackManifest trackToDiscard;
							trackQueue.TryDequeue( out trackToDiscard );
						}
					}
					catch ( Exception ex )
					{
						EventLog.WriteEntry( Constants.EventLogSourceName, $"Error sharing tracks: {ex.ToString()}", EventLogEntryType.Error );
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

		private IShareDestination GetShareDestination( ShareDestination destination )
		{
			if ( destination.Name == "Slack" )
			{
				return new SlackDestination();
			}
			return null;
		}

		private readonly User _user;
		private readonly Dictionary<UserSource, System.Timers.Timer> _sourcePollers;
		private readonly Dictionary<UserShareDestination, ConcurrentQueue<TrackManifest>> _destinationTrackLists;
		private readonly Dictionary<UserShareDestination, object> _destinationLocks;
	}
}
