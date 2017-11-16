using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Woffler.Primitives;

namespace Woffler.Database
{
	public class DatabaseHandler: IDisposable
	{
		public DatabaseHandler()
		{
			_connection = new SQLiteConnection("Data Source=" + _sqliteDatabaseFile + ";foreign keys=true;Version=" + SqliteDatabaseVersion + ";");
			_dbLocks = new ConcurrentDictionary<string, object>();
			Initialize();
		}
		public void Dispose()
		{
			_connection.Close();
		}

		private void Initialize()
		{
			_connection.Open();

			CreateAndUpgradeTables();
		}

		private void CreateAndUpgradeTables()
		{
			// Do we have a blank database?

			using ( var command = new SQLiteCommand( "SELECT name FROM sqlite_master WHERE type='table' AND name='Version'", _connection ) )
			{
				var reader = command.ExecuteReader();
				if ( reader.Read() ) return;
			}

			// Upgrading not necessary yet

			var createSqlFile = Path.Combine( Path.GetDirectoryName( Assembly.GetExecutingAssembly().Location ), @"Database\000_Create.sql" );
			var createSqlStatements = File.ReadAllText(createSqlFile);

			foreach (var singleSqlStatement in createSqlStatements.Split(';'))
			{
				using ( var populateDatabaseCommand = new SQLiteCommand( singleSqlStatement, _connection ) )
				{
					populateDatabaseCommand.ExecuteNonQuery();
				}
			}

		}

		public ICollection<User> QueryUsers( string name = null, bool activeOnly = true, bool loadChildren = true )
		{
			var users = new List<User>();

			var sql = new StringBuilder();
			sql.Append( $"SELECT ID, Name, Email, Active FROM {DbTables.Users}" );
			if ( !string.IsNullOrEmpty( name ) || activeOnly )
			{
				sql.Append( " WHERE" );

				if ( !string.IsNullOrEmpty( name ) )
				{
					sql.Append( $" Name = '{name}'" );

					if ( activeOnly )
					{
						sql.Append( " AND" );
					}
				}
				if ( activeOnly )
				{
					sql.Append( " Active = 1" );
				}
			}
			
			using (var command = new SQLiteCommand( sql.ToString(), _connection))
			{
				var reader = command.ExecuteReader();
				while (reader.Read())
				{
					var user = new User()
					{
						Id = GetColumnValue( reader, "ID", Convert.ToInt32),
						Name = GetColumnValue<string>(reader, "Name"),
						Email = GetColumnValue<string>(reader, "Email"),
						Active = GetColumnValue(reader, "Active", Convert.ToBoolean)
					};

					if ( loadChildren )
					{
						user.UserSources = QueryUserSources(user.Id, activeOnly);
						user.UserDestinations = QueryUserShareDestinations(user.Id, activeOnly);
					}

					users.Add(user);
				}
			}

			return users;
		}

		public IList<UserSource> QueryUserSources( int UserId, bool activeOnly = true, bool loadChildren = true )
		{
			var userSourcesSql = $@"SELECT ID,
	User_ID,
	Source_Config_ID,
	Active,
	Source_UserName,
	Source_UserPassword,
	Poll_Interval,
	Track_Limit,
	Last_Poll
FROM {DbTables.UserHasSource}
WHERE User_ID = {UserId}";

			if (activeOnly)
			{
				userSourcesSql += " AND Active = 1";
			}

			var userSources = new List<UserSource>();

			using (var command = new SQLiteCommand(userSourcesSql, _connection))
			{
				var reader = command.ExecuteReader();
				while ( reader.Read() )
				{
					// Not worrying about reading last poll from the data until it can be persisted
					var lastPoll = DateTimeOffset.Now.AddMinutes(DefaultMinuteOffsetForLastPoll);
					var userSource = new UserSource()
					{
						Id = GetColumnValue(reader, "ID", Convert.ToInt32),
						UserId = GetColumnValue(reader, "User_ID", Convert.ToInt32),
						SourceConfigId = GetColumnValue(reader, "Source_Config_ID", Convert.ToInt32),
						Active = GetColumnValue(reader, "Active", Convert.ToBoolean),
						SourceUserName = GetColumnValue<string>(reader, "Source_UserName"),
						SourceUserPassword = GetColumnValue<string>(reader, "Source_UserPassword"),
						PollInterval = GetColumnValue(reader, "Poll_Interval", NullableIntConverter),
						TrackLimit = GetColumnValue(reader, "Track_Limit", NullableIntConverter),
						LastPoll = lastPoll
					};

					if ( loadChildren )
					{
						userSource.SourceConfig = QuerySourceConfigs(userSource.SourceConfigId).First();
					}

					userSources.Add(userSource);
				}
			}

			return userSources;
		}

		public IList<SourceConfig> QuerySourceConfigs(int? sourceConfigId = null, bool loadChildren = true)
		{
			var sourceConfigSql = $@"SELECT ID,
	Name,
	Source_ID,
	API_Key,
	API_URL,
	Default_Poll_Interval,
	Default_Track_Limit
FROM {DbTables.SourceConfigs}";

			if (sourceConfigId != null)
			{
				sourceConfigSql += $" WHERE ID = {sourceConfigId}";
			}

			var sourceConfigs = new List<SourceConfig>();
			using (var command = new SQLiteCommand(sourceConfigSql, _connection))
			{
				var reader = command.ExecuteReader();
				while ( reader.Read() )
				{
					var sourceConfig = new SourceConfig()
					{
						Id = GetColumnValue(reader, "ID", Convert.ToInt32),
						Name = GetColumnValue<string>(reader, "Name"),
						SourceId = GetColumnValue(reader, "Source_ID", Convert.ToInt32),
						ApiKey = GetColumnValue<string>(reader, "API_Key"),
						ApiUrl = GetColumnValue<string>(reader, "API_URL"),
						DefaultPollInterval = GetColumnValue(reader, "Default_Poll_Interval", Convert.ToInt32),
						DefaultTrackLimit = GetColumnValue(reader, "Default_Track_Limit", Convert.ToInt32)
					};

					if ( loadChildren )
					{
						sourceConfig.Source = QuerySources(sourceConfig.SourceId).First();
					}

					sourceConfigs.Add(sourceConfig);
				}
			}

			return sourceConfigs;
		}

		public IList<Source> QuerySources( int? sourceId )
		{
			var sourcesSql = $@"SELECT ID,
Name
FROM {DbTables.Sources}";

			if ( sourceId != null )
			{
				sourcesSql += $" WHERE ID = {sourceId}";
			}

			var sources = new List<Source>();

			using (var command = new SQLiteCommand(sourcesSql, _connection))
			{
				var reader = command.ExecuteReader();

				while ( reader.Read() )
				{
					var source = new Source()
					{
						Id = GetColumnValue(reader, "ID", Convert.ToInt32),
						Name = GetColumnValue<string>(reader, "Name")
					};

					sources.Add(source);
				}
			}

			return sources;
		}

		public IList<UserShareDestination> QueryUserShareDestinations(int UserId, bool activeOnly = true, bool loadChildren = true)
		{
			var userDestinationsSql = $@"SELECT ID,
	User_ID,
	Share_Destination_Config_ID,
	Active,
	Share_UserName,
	Track_Limit,
	Formatter,
	Track_URL_Provider,
	Image_URL_Provider
FROM {DbTables.UserHasShareDestination}
WHERE User_ID = {UserId}";

			if (activeOnly)
			{
				userDestinationsSql += " AND Active = 1";
			}

			var userDestinations = new List<UserShareDestination>();

			using (var command = new SQLiteCommand(userDestinationsSql, _connection))
			{
				var reader = command.ExecuteReader();
				while (reader.Read())
				{
					var userDestination = new UserShareDestination()
					{
						Id = GetColumnValue(reader, "ID", Convert.ToInt32),
						UserId = GetColumnValue(reader, "User_ID", Convert.ToInt32),
						ShareDestinationConfigId = GetColumnValue(reader, "Share_Destination_Config_ID", Convert.ToInt32),
						Active = GetColumnValue(reader, "Active", Convert.ToBoolean),
						ShareUserName = GetColumnValue<string>(reader, "Share_UserName"),
						TrackLimit = GetColumnValue(reader, "Track_Limit", NullableIntConverter),
						Formatter = GetColumnValue<string>(reader, "Formatter"),
						TrackUrlProvider = GetColumnValue<string>(reader, "Track_URL_Provider"),
						ImageUrlProvider = GetColumnValue<string>(reader, "Image_URL_Provider")
					};

					if (loadChildren)
					{
						userDestination.ShareDestinationConfig = QueryShareDestinationConfigs(userDestination.ShareDestinationConfigId).First();
					}

					userDestinations.Add(userDestination);
				}
			}

			return userDestinations;
		}

		public IList<ShareDestinationConfig> QueryShareDestinationConfigs(int? destinationConfigId = null, bool loadChildren = true)
		{
			var destinationConfigSql = $@"SELECT ID,
	Name,
	Share_Destination_ID,
	API_Key,
	API_URL,
	Default_Formatter,
	Default_Track_Limit
FROM {DbTables.ShareDestinationConfigs}";

			if (destinationConfigId != null)
			{
				destinationConfigSql += $" WHERE ID = {destinationConfigId}";
			}

			var destinationConfigs = new List<ShareDestinationConfig>();
			using (var command = new SQLiteCommand(destinationConfigSql, _connection))
			{
				var reader = command.ExecuteReader();
				while (reader.Read())
				{
					var destinationConfig = new ShareDestinationConfig()
					{
						Id = GetColumnValue(reader, "ID", Convert.ToInt32),
						Name = GetColumnValue<string>(reader, "Name"),
						ShareDestinationId = GetColumnValue(reader, "Share_Destination_ID", Convert.ToInt32),
						ApiKey = GetColumnValue<string>(reader, "API_Key"),
						ApiUrl = GetColumnValue<string>(reader, "API_URL"),
						DefaultFormatter = GetColumnValue<string>(reader, "Default_Formatter"),
						DefaultTrackLimit = GetColumnValue(reader, "Default_Track_Limit", Convert.ToInt32)
					};

					if (loadChildren)
					{
						destinationConfig.ShareDestination = QueryShareDestinations(destinationConfig.ShareDestinationId).First();
					}

					destinationConfigs.Add(destinationConfig);
				}
			}

			return destinationConfigs;
		}

		public IList<ShareDestination> QueryShareDestinations(int? destinationId)
		{
			var destinationsSql = $@"SELECT ID,
Name
FROM {DbTables.ShareDestinations}";

			if (destinationId != null)
			{
				destinationsSql += $" WHERE ID = {destinationId}";
			}

			var destinations = new List<ShareDestination>();

			using (var command = new SQLiteCommand(destinationsSql, _connection))
			{
				var reader = command.ExecuteReader();

				while (reader.Read())
				{
					var destination = new ShareDestination()
					{
						Id = GetColumnValue(reader, "ID", Convert.ToInt32),
						Name = GetColumnValue<string>(reader, "Name")
					};

					destinations.Add(destination);
				}
			}

			return destinations;
		}

		public void PersistUser( User user )
		{
			if ( user.Id == 0 ) // INSERT
			{
				var insertLine = new StringBuilder();
				var valuesLine = new StringBuilder();
				insertLine.Append($"INSERT INTO {DbTables.Users} (Name, Active");
				valuesLine.Append($" VALUES ('{user.Name}', {Convert.ToInt32(user.Active)}");
				if ( user.Email != null )
				{
					insertLine.Append(", Email");
					valuesLine.Append($", '{user.Email}'");
				}
				insertLine.Append(")");
				valuesLine.Append(")");
				var persistSql = insertLine.ToString() + valuesLine.ToString();
				var newKey = ExecuteInsertWithTableLock(DbTables.Users, persistSql);
				user.Id = newKey;
			}
			else // UPDATE
			{
				var updateLine = new StringBuilder();
				updateLine.Append($"UPDATE {DbTables.Users} SET Name = '{user.Name}'");
				updateLine.Append($", Active = {Convert.ToInt32(user.Active)}");
				var emailValue = user.Email != null ? $"'{user.Email}'" : "null";
				updateLine.Append($", Email = {emailValue}");
				updateLine.Append($" WHERE ID = {user.Id}");
				var persistSql = updateLine.ToString();
				using (var command = new SQLiteCommand(persistSql, _connection))
				{
					command.ExecuteNonQuery();
				}
			}

			foreach ( var userSource in user.UserSources )
			{
				PersistUserSource(userSource);
			}
			foreach ( var shareDestination in user.UserDestinations )
			{
				PersistUserShareDestination(shareDestination);
			}

		}

		public void PersistSourceConfig( SourceConfig sourceConfig )
		{
			if ( sourceConfig.Id == 0 )
			{
				var insertLine = new StringBuilder();
				var valuesLine = new StringBuilder();
				insertLine.Append($"INSERT INTO {DbTables.SourceConfigs} (Name, Source_ID, Default_Poll_interval, Default_Track_Limit");
				valuesLine.Append($" VALUES ('{sourceConfig.Name}', {sourceConfig.SourceId}");
				valuesLine.Append($", {sourceConfig.DefaultPollInterval}, {sourceConfig.DefaultTrackLimit}");
				if ( sourceConfig.ApiKey != null )
				{
					insertLine.Append(", API_Key");
					valuesLine.Append($", '{sourceConfig.ApiKey}'");
				}
				if ( sourceConfig.ApiUrl != null )
				{
					insertLine.Append(", API_URL");
					valuesLine.Append($", '{sourceConfig.ApiUrl}'");
				}
				insertLine.Append(")");
				valuesLine.Append(")");		
				var persistSql = insertLine.ToString() + valuesLine.ToString();
				var newKey = ExecuteInsertWithTableLock(DbTables.SourceConfigs, persistSql);
				sourceConfig.Id = newKey;
			}
			else
			{
				var updateLine = new StringBuilder();
				updateLine.Append($"UPDATE {DbTables.SourceConfigs} SET Name = '{sourceConfig.Name}'");
				updateLine.Append($", Source_ID = {sourceConfig.SourceId}");
				updateLine.Append($", Default_Poll_Interval = {sourceConfig.DefaultPollInterval}");
				updateLine.Append($", Default_Track_Limit = {sourceConfig.DefaultTrackLimit}");
				var apiKeyValue = sourceConfig.ApiKey != null ? $"'{sourceConfig.ApiKey}'" : "null";
				updateLine.Append($", {apiKeyValue}");
				var apiKeyUrl = sourceConfig.ApiUrl != null ? $"'{sourceConfig.ApiUrl}'" : "null";
				updateLine.Append($", {apiKeyUrl}");
				updateLine.Append($" WHERE ID = {sourceConfig.Id}");
				var persistSql = updateLine.ToString();
				using (var command = new SQLiteCommand(persistSql, _connection))
				{
					command.ExecuteNonQuery();
				}
			}
		}

		public void PersistUserSource( UserSource userSource )
		{
			if ( userSource.Id == 0 )
			{
				var insertLine = new StringBuilder();
				var valuesLine = new StringBuilder();
				insertLine.Append($"INSERT INTO {DbTables.UserHasSource} (User_ID, Source_Config_ID, Active");
				valuesLine.Append($" VALUES ({userSource.UserId}, {userSource.SourceConfigId}, {Convert.ToInt32(userSource.Active)}");
				if (userSource.SourceUserName != null)
				{
					insertLine.Append(", Source_UserName");
					valuesLine.Append($", '{userSource.SourceUserName}'");
				}
				if (userSource.SourceUserPassword != null)
				{
					insertLine.Append(", Source_UserPassword");
					valuesLine.Append($", '{userSource.SourceUserPassword}'");
				}
				if (userSource.PollInterval != null)
				{
					insertLine.Append(", Poll_Interval");
					insertLine.Append($", {userSource.PollInterval}");
				}
				if (userSource.TrackLimit != null)
				{
					insertLine.Append(", Track_Limit");
					insertLine.Append($", {userSource.TrackLimit}");
				}
				if (userSource.LastPoll != null)
				{
					insertLine.Append(", Last_Poll");
					insertLine.Append($", {userSource.LastPoll.ToUnixTimeSeconds()}");
				}
				var persistSql = insertLine.ToString() + valuesLine.ToString();
				var newKey = ExecuteInsertWithTableLock(DbTables.UserHasSource, persistSql);
				userSource.Id = newKey;
			}
			else
			{
				var updateLine = new StringBuilder();
				updateLine.Append($"UPDATE {DbTables.UserHasSource} SET User_ID = {userSource.UserId}");
				updateLine.Append($", Source_Config_ID = {userSource.SourceConfigId}");
				updateLine.Append($", Active = {Convert.ToInt32(userSource.Active)}");
				var sourceUserNameValue = userSource.SourceUserName != null ? $"'{userSource.SourceUserName}'" : "null";
				updateLine.Append($", Source_UserName = {sourceUserNameValue}");
				var sourceUserPasswordValue = userSource.SourceUserPassword != null ? $"'{userSource.SourceUserPassword}'" : "null";
				updateLine.Append($", Source_UserPassword = {sourceUserPasswordValue}");
				var pollIntervalValue = userSource.PollInterval != null ? $"'{userSource.PollInterval}'" : "null";
				updateLine.Append($", Poll_Interval = {pollIntervalValue}");
				var trackLimitValue = userSource.TrackLimit != null ? $"'{userSource.TrackLimit}'" : "null";
				updateLine.Append($", Track_Limit = {trackLimitValue}");
				var lastPollValue = userSource.LastPoll != null ? $"'{userSource.LastPoll}'" : "null";
				updateLine.Append($", Last_Poll = {lastPollValue}");
				updateLine.Append($" WHERE ID = {userSource.Id}");
				var persistSql = updateLine.ToString();
				using (var command = new SQLiteCommand(persistSql, _connection))
				{
					command.ExecuteNonQuery();
				}
			}
		}

		public void PersistShareDestinationConfig( ShareDestinationConfig shareDestinationConfig )
		{
			if ( shareDestinationConfig.Id == 0)
			{
				var insertLine = new StringBuilder();
				var valuesLine = new StringBuilder();
				insertLine.Append($"INSERT INTO {DbTables.ShareDestinationConfigs} (Name, Share_Destination_ID");
				insertLine.Append(", Default_Formatter, Default_Track_Limit");
				valuesLine.Append($" VALUES ('{shareDestinationConfig.Name}', {shareDestinationConfig.ShareDestinationId}");
				valuesLine.Append($", {shareDestinationConfig.DefaultFormatter}, {shareDestinationConfig.DefaultTrackLimit}");
				if (shareDestinationConfig.ApiKey != null)
				{
					insertLine.Append(", API_Key");
					valuesLine.Append($", '{shareDestinationConfig.ApiKey}'");
				}
				if (shareDestinationConfig.ApiUrl != null)
				{
					insertLine.Append(", API_URL");
					valuesLine.Append($", '{shareDestinationConfig.ApiUrl}'");
				}
				insertLine.Append(")");
				valuesLine.Append(")");
				var persistSql = insertLine.ToString() + valuesLine.ToString();
				var newKey = ExecuteInsertWithTableLock(DbTables.ShareDestinationConfigs, persistSql);
				shareDestinationConfig.Id = newKey;
			}
			else
			{
				var updateLine = new StringBuilder();
				updateLine.Append($"UPDATE {DbTables.ShareDestinationConfigs} SET Name = '{shareDestinationConfig.Name}'");
				updateLine.Append($", Share_Destination_ID = {shareDestinationConfig.ShareDestinationId}");
				updateLine.Append($", Default_Formatter = {shareDestinationConfig.DefaultFormatter}");
				updateLine.Append($", Default_Track_Limit = {shareDestinationConfig.DefaultTrackLimit}");
				var apiKeyValue = shareDestinationConfig.ApiKey != null ? $"'{shareDestinationConfig.ApiKey}'" : "null";
				updateLine.Append($", {apiKeyValue}");
				var apiKeyUrl = shareDestinationConfig.ApiUrl != null ? $"'{shareDestinationConfig.ApiUrl}'" : "null";
				updateLine.Append($", {apiKeyUrl}");
				updateLine.Append($" WHERE ID = {shareDestinationConfig.Id}");
				var persistSql = updateLine.ToString();
				using (var command = new SQLiteCommand(persistSql, _connection))
				{
					command.ExecuteNonQuery();
				}
			}
		}

		public void PersistUserShareDestination( UserShareDestination userShareDestination )
		{
			if ( userShareDestination.Id == 0 )
			{
				var insertLine = new StringBuilder();
				var valuesLine = new StringBuilder();
				insertLine.Append($"INSERT INTO {DbTables.UserHasShareDestination} (User_ID, Source_Destination_Config_ID, Active");
				valuesLine.Append($" VALUES ({userShareDestination.UserId}, {userShareDestination.ShareDestinationConfigId}, {Convert.ToInt32(userShareDestination.Active)}");
				if (userShareDestination.ShareUserName != null)
				{
					insertLine.Append(", Share_UserName");
					valuesLine.Append($", '{userShareDestination.ShareUserName}'");
				}
				if (userShareDestination.TrackLimit != null)
				{
					insertLine.Append(", Track_Limit");
					insertLine.Append($", {userShareDestination.TrackLimit}");
				}
				if (userShareDestination.Formatter != null)
				{
					insertLine.Append(", Formatter");
					insertLine.Append($", {userShareDestination.Formatter}");
				}
				if (userShareDestination.TrackUrlProvider != null)
				{
					insertLine.Append(", Track_URL_Provider");
					insertLine.Append($", {userShareDestination.TrackUrlProvider}");
				}
				if (userShareDestination.ImageUrlProvider != null)
				{
					insertLine.Append(", Image_URL_Provider");
					insertLine.Append($", {userShareDestination.ImageUrlProvider}");
				}
				var persistSql = insertLine.ToString() + valuesLine.ToString();
				var newKey = ExecuteInsertWithTableLock(DbTables.UserHasShareDestination, persistSql);
				userShareDestination.Id = newKey;
			}
			else
			{
				var updateLine = new StringBuilder();
				updateLine.Append($"UPDATE {DbTables.UserHasShareDestination} SET User_ID = {userShareDestination.UserId}");
				updateLine.Append($", Share_Destination_Config_ID = {userShareDestination.ShareDestinationConfig}");
				updateLine.Append($", Active = {Convert.ToInt32(userShareDestination.Active)}");
				var shareUserNameValue = userShareDestination.ShareUserName != null ? $"'{userShareDestination.ShareUserName}'" : "null";
				updateLine.Append($", Source_UserName = {shareUserNameValue}");
				var formatterValue = userShareDestination.Formatter != null ? $"'{userShareDestination.Formatter}'" : "null";
				updateLine.Append($", Formatter = {formatterValue}");
				var trackLimitValue = userShareDestination.TrackLimit != null ? $"'{userShareDestination.TrackLimit}'" : "null";
				updateLine.Append($", Track_Limit = {trackLimitValue}");
				var trackUrlProviderValue = userShareDestination.TrackUrlProvider != null ? $"'{userShareDestination.TrackUrlProvider}'" : "null";
				updateLine.Append($", Track_URL_Provider = {trackUrlProviderValue}");
				var imageUrlProviderValue = userShareDestination.ImageUrlProvider != null ? $"'{userShareDestination.ImageUrlProvider}'" : "null";
				updateLine.Append($", Image_URL_Provider = {imageUrlProviderValue}");
				updateLine.Append($" WHERE ID = {userShareDestination.Id}");
				var persistSql = updateLine.ToString();
				using (var command = new SQLiteCommand(persistSql, _connection))
				{
					command.ExecuteNonQuery();
				}
			}
		}
		private T GetColumnValue<T>( SQLiteDataReader reader, string columnName, Func<object,T> converter = null )
		{
			if ( reader [ columnName ].GetType() == typeof( DBNull ) )
			{
				return default(T);
			}
			if ( converter != null )
			{
				return converter( reader[ columnName ] );
			}
			return (T)reader[columnName];
		}

		private static int? NullableIntConverter( object value )
		{
			if (value == null) return null;

			return Convert.ToInt32(value);
		}
		private static DateTimeOffset DateTimeOffsetConverter( object value )
		{
			var secondsFromEpoch = (long)value;

			return DateTimeOffset.FromUnixTimeSeconds(secondsFromEpoch);
		}
		private int ExecuteInsertWithTableLock( string dbTable, string insertSql )
		{
			if ( !_dbLocks.ContainsKey( dbTable ) )
			{
				_dbLocks.Add(dbTable, new object());
			}

			int newKey = 0;
			using (var command = new SQLiteCommand( insertSql, _connection) )
			{
				lock ( _dbLocks[ dbTable ] )
				{
					command.ExecuteNonQuery();
					command.CommandText = $"select seq from sqlite_sequence where name = '{dbTable}'";
					newKey = (int) command.ExecuteScalar();
				}
			}

			return newKey;
		}
		private readonly SQLiteConnection _connection;
		private readonly IDictionary<string, object> _dbLocks;

		private readonly string _sqliteDatabaseFile = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\WofflerDatabase.sqlite";
		private const string SqliteDatabaseVersion = "3";
		private const int DefaultMinuteOffsetForLastPoll = -10;
	}
}
