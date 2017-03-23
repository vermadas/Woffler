-- Separate with semicolons so we can run one at a time
CREATE TABLE IF NOT EXISTS Users (
    ID INTEGER PRIMARY KEY,
    Name VARCHAR(32) NOT NULL UNIQUE, 
    Email VARCHAR(64),
    Active TINYINT NOT NULL
);
CREATE TABLE IF NOT EXISTS Sources (
    ID INTEGER PRIMARY KEY,
    Name VARCHAR(32) NOT NULL UNIQUE
);
CREATE TABLE IF NOT EXISTS Source_Configs (
	ID INTEGER PRIMARY KEY,
	Name VARCHAR(32) NOT NULL,
	Source_ID INTEGER NOT NULL,
    API_Key VARCHAR(64),
	API_URL VARCHAR(128),
    Default_Poll_Interval INTEGER NOT NULL,
    Default_Track_Limit INTEGER NOT NULL,
	UNIQUE (Name, Source_ID),
	FOREIGN KEY (Source_ID) REFERENCES Sources (ID)
);
CREATE TABLE IF NOT EXISTS User_Has_Source (
    ID INTEGER PRIMARY KEY,
    User_ID INTEGER NOT NULL,
    Source_Config_ID INTEGER NOT NULL,
    Active TINYINT NOT NULL,
    Source_UserName VARCHAR(32),
    Source_UserPassword VARCHAR(32),
    Poll_Interval INTEGER,
    Track_Limit INTEGER,
    Last_Poll BIGINT,
	FOREIGN KEY (User_ID) REFERENCES Users (ID),
	FOREIGN KEY (Source_Config_ID) REFERENCES Source_Configs (ID)
);
CREATE TABLE IF NOT EXISTS Share_Destinations (
    ID INTEGER PRIMARY KEY,
    Name VARCHAR(32) NOT NULL UNIQUE
);
CREATE TABLE IF NOT EXISTS Share_Destination_Configs (
    ID INTEGER PRIMARY KEY,
	Name VARCHAR(32) NOT NULL,
    Share_Destination_ID INTEGER NOT NULL,
    API_Key VARCHAR(64),
    API_URL VARCHAR(128),
    Default_Formatter TEXT NOT NULL,
    Default_Track_Limit INTEGER NOT NULL,
	UNIQUE (Name, Share_Destination_ID),
	FOREIGN KEY (Share_Destination_ID) REFERENCES Share_Destinations (ID)
);
CREATE TABLE IF NOT EXISTS User_Has_Share_Destination (
    ID INTEGER PRIMARY KEY,
    User_ID INTEGER NOT NULL REFERENCES Users (ID),
    Share_Destination_Config_ID INTEGER NOT NULL REFERENCES Share_Destination_Configs (ID),
    Active TINYINT NOT NULL,
    Share_UserName VARCHAR(32),
    Track_Limit INTEGER,
    Formatter TEXT,
    Track_URL_Provider VARCHAR(32),
    Image_URL_Provider VARCHAR(32),
	FOREIGN KEY (User_ID) REFERENCES Users (ID),
	FOREIGN KEY (Share_Destination_Config_ID) REFERENCES Share_Destination_Configs (ID)
);

CREATE TABLE IF NOT EXISTS Version (
    DB INTEGER NOT NULL);
INSERT INTO Sources (Name) VALUES ('Last.FM');
INSERT INTO Source_Configs (Name, Source_ID, API_Key, API_URL, Default_Poll_Interval, Default_Track_Limit)
	VALUES ('Default', (SELECT ID FROM Sources WHERE name='Last.FM'), '178fe761db2aa99893d9b36b4edd6247', 'http://ws.audioscrobbler.com/2.0/', 60, 5);
INSERT INTO Share_Destinations (Name) VALUES ('Slack');
INSERT INTO Version (DB) VALUES (1);