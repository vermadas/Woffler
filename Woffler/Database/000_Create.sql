-- Separate with semicolons so we can run one at a time
CREATE TABLE IF NOT EXISTS Users (
    ID INTEGER PRIMARY KEY,
    Name VARCHAR(32) NOT NULL UNIQUE, 
    Email VARCHAR(64),
    Active TINYINT NOT NULL);
CREATE TABLE IF NOT EXISTS Sources (
    ID INTEGER PRIMARY KEY,
    Name VARCHAR(32) NOT NULL UNIQUE,
    API_Key VARCHAR(64),
    Default_Poll_Interval INTEGER NOT NULL,
    Default_Track_Limit INTEGER NOT NULL);
CREATE TABLE IF NOT EXISTS User_Has_Source (
    ID INTEGER PRIMARY KEY,
    User_ID INTEGER NOT NULL REFERENCES User (ID),
    Source_ID INTEGER NOT NULL REFERENCES Source (ID),
    Active TINYINT NOT NULL,
    Source_UserName VARCHAR(32),
    Source_UserPassword VARCHAR(32),
    Poll_Interval INTEGER,
    Track_Limit INTEGER,
    Last_Poll BIGINT);
CREATE TABLE IF NOT EXISTS Share_Destinations (
    ID INTEGER PRIMARY KEY,
    Name VARCHAR(32) NOT NULL UNIQUE,
    API_Key VARCHAR(64),
    Default_Formatter TEXT NOT NULL,
    Default_Track_Limit INTEGER NOT NULL );
CREATE TABLE IF NOT EXISTS User_Has_Share_Destination (
    ID INTEGER PRIMARY KEY,
    User_ID INTEGER NOT NULL REFERENCES User (ID),
    Share_Destination_ID INTEGER NOT NULL REFERENCES Share_Destination (ID),
    Active TINYINT NOT NULL,
    Share_UserName VARCHAR(32),
    Track_Limit INTEGER,
    Formatter TEXT,
    Track_URL_Provider VARCHAR(32),
    Image_URL_Provider VARCHAR(32));
CREATE TABLE IF NOT EXISTS Version (
    DB INTEGER NOT NULL);
INSERT INTO Sources (Name, API_Key, Default_Poll_Interval, Default_Track_Limit)
    VALUES ('Last.FM', '178fe761db2aa99893d9b36b4edd6247', 60, 5);
INSERT INTO Share_Destinations (Name, Default_Formatter, Default_Track_Limit)
    VALUES ('Slack_CE', '{
    "attachments": [
        {
            "fallback": "%N is listening to %A - %T",
            "color": "#36a64f",
            "pretext": "%N is listening to",
            "author_name": "%A",
            "title": "%T",
            "title_link": "%U",
            "text": "%L",
            "thumb_url": "%P"
        }
    ]
}', 5);
INSERT INTO Version (DB) VALUES (1)