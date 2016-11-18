using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Woffler.Primitives;

namespace Woffler.Database
{
	public class DatabaseHandler: IDisposable
	{
		public DatabaseHandler()
		{
			_connection = new SQLiteConnection("Data Source=" + _sqliteDatabaseFile + ";Version=" + SqliteDatabaseVersion + ";");
			Initialize();
		}
		public void Dispose()
		{
			_connection.Close();
		}

		public ICollection<User> BuildUserList()
		{
/*			// For debugging now

			var user = new User
			{
				Name = "Adam G",
				Email = "adam.graves@careevolution.com"
			};
			var now = DateTimeOffset.Now;
			var source = new Source
			{
				ApiKey = "178fe761db2aa99893d9b36b4edd6247",
				LastPoll = now.AddMinutes(-240),
				PollInterval = 60,
				Name = "Last.FM",
				TrackLimit = 5,
				UserName = "vermadas"
			};

			var destination = new Destination
			{
				Name = "Slack_CE",
				TrackLimit = 10,
				User = "Adam Graves",
				Formatter = @"{
    ""attachments"": [
		{
			""fallback"": ""%N is listening to %A - %T"",
            ""color"": ""#36a64f"",
            ""pretext"": ""%N is listening to"",
            ""author_name"": ""%A"",
            ""title"": "" %T"",
            ""title_link"": ""%U"",
            ""text"": ""%L"",
            ""thumb_url"": ""%P""
		}
    ]
}"
			};

			user.Sources = new List<Source>() { source };
			user.Destinations = new List<Destination>() { destination };
			return new List<User>() { user }; */
			_connection.Open();
			var users = QueryUsers();
			foreach (var user in users)
			{
				QuerySourcesForUser( user );
				QueryDestinationsForUser( user );
			}
			_connection.Close();
			return users;
		}
		private void Initialize()
		{
			try
			{
				_connection.Open();

				CreateAndUpgradeTables();
			}
			catch (Exception)
			{

			}
			finally
			{
				_connection.Close();
			}
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

		private ICollection<User> QueryUsers()
		{
			var users = new List<User>();

			using (var command = new SQLiteCommand("SELECT Name,Email FROM Users WHERE Active = 1", _connection))
			{
				var reader = command.ExecuteReader();
				while (reader.Read())
				{
					var user = new User()
					{
						Name = (string) reader["Name"],
						Email = (string) reader["Email"]
					};
					users.Add(user);
				}
			}

			return users;
		}

		private void QuerySourcesForUser(User user)
		{
			var sourcesSql = $@"SELECT uhs.Source_UserName,
	uhs.Source_UserPassword,
	uhs.Poll_Interval,
	uhs.Track_Limit,
	s.Name,
	s.API_Key,
	s.Default_Poll_Interval,
	s.Default_Track_Limit
FROM Users u
JOIN User_Has_Source uhs ON u.ID = uhs.User_ID
JOIN Sources s ON uhs.Source_ID = s.ID
WHERE u.Name = '{user.Name}' AND uhs.Active = 1";

			using (var command = new SQLiteCommand(sourcesSql, _connection))
			{
				var reader = command.ExecuteReader();
				while (reader.Read())
				{
					var interval = reader["Poll_Interval"].GetType() != typeof(DBNull)
						? (long) reader["Poll_Interval"]
						: (long) reader["Default_Poll_Interval"];
					var trackLimit = reader[ "Track_Limit" ].GetType() != typeof( DBNull )
						? (long) reader[ "Track_Limit" ]
						: (long) reader[ "Default_Track_Limit" ];
					// Not worrying about reading last poll from the data until it can be persisted
					var lastPoll = DateTimeOffset.Now.AddMinutes(DefaultMinuteOffsetForLastPoll);
					var source = new Source()
					{
						Name = (string) reader["Name"],
						ApiKey = reader["API_Key"].GetType() != typeof( DBNull ) ? (string) reader["API_Key"] : null,
						LastPoll = lastPoll,
						PollInterval = Convert.ToInt32(interval),
						TrackLimit = Convert.ToInt32(trackLimit),
						UserName = reader["Source_UserName"].GetType() != typeof( DBNull ) ? (string) reader["Source_UserName"] : null,
						UserPassword = reader[ "Source_UserPassword" ].GetType() != typeof( DBNull ) ? (string) reader[ "Source_UserPassword" ] : null
					};
					user.Sources.Add(source);
				}
			}
		}

		private void QueryDestinationsForUser(User user)
		{
			var destinationsSql = $@"SELECT uhd.Share_UserName,
	uhd.Track_Limit,
	uhd.Formatter,
	uhd.Track_URL_Provider,
	uhd.Image_URL_Provider,
	d.Name,
	d.API_Key,
	d.Default_Formatter,
	d.Default_Track_Limit
FROM Users u
JOIN User_Has_Share_Destination uhd ON u.ID = uhd.User_ID
JOIN Share_Destinations d ON uhd.Share_Destination_ID = d.ID
WHERE u.Name = '{user.Name}' AND uhd.Active = 1";

			using ( var command = new SQLiteCommand( destinationsSql, _connection ) )
			{
				var reader = command.ExecuteReader();
				while ( reader.Read() )
				{
					var formatter = reader[ "Formatter" ].GetType() != typeof( DBNull )
						? (string)reader[ "Formatter" ]
						: (string)reader[ "Default_Formatter" ];
					var trackLimit = reader[ "Track_Limit" ].GetType() != typeof( DBNull )
						? (long)reader[ "Track_Limit" ]
						: (long)reader[ "Default_Track_Limit" ];

					var destination = new Destination()
					{
						Name = (string) reader[ "Name" ],
						ApiKey = reader[ "API_Key" ].GetType() != typeof( DBNull ) ? (string)reader[ "API_Key" ] : null,
						Formatter = formatter,
						TrackLimit = Convert.ToInt32(trackLimit),
						User = reader[ "Share_UserName" ].GetType() != typeof( DBNull ) ? (string)reader[ "Share_UserName" ] : null,
						TrackUrlProvider = reader[ "Track_URL_Provider" ].GetType() != typeof( DBNull ) ? (string)reader[ "Track_URL_Provider" ] : null,
						ImageUrlProvider = reader[ "Image_URL_Provider" ].GetType() != typeof( DBNull ) ? (string)reader[ "Image_URL_Provider" ] : null
					};
					user.Destinations.Add( destination );
				}
			}
		}
		private SQLiteConnection _connection;

		private readonly string _sqliteDatabaseFile = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\WofflerDatabase.sqlite";
		private const string SqliteDatabaseVersion = "3";
		private const int DefaultMinuteOffsetForLastPoll = -10;
	}
}
