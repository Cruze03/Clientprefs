using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Dapper;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using Microsoft.Data.Sqlite;

using Clientprefs.API;

namespace Clientprefs;
public partial class Clientprefs
{
    private string SQLiteDatasource = "";

    private void Database_OnPluginLoad()
    {
        SQLiteDatasource = $"Data Source={Path.Join(ModuleDirectory, "clientprefs.db")}";
    }
    
    private MySqlConnection CreateConnection()
    {
        MySqlConnectionStringBuilder builder = new MySqlConnectionStringBuilder
        {
            Server = Config.DatabaseHost,
            UserID = Config.DatabaseUsername,
            Password = Config.DatabasePassword,
            Database = Config.DatabaseName,
            Port = (uint)Config.DatabasePort,
            SslMode = Enum.Parse<MySqlSslMode>(Config.DatabaseSslmode, true),
            AllowUserVariables=true,
        };
        return new MySqlConnection(builder.ToString());
    }

    private async Task<bool> ConnectDatabaseTable()
    {
        try
        {
            g_ClientPrefs.Clear();
            g_iLatestClientprefID = 0;
            
            if(Config.DatabaseType == "mysql")
            {
                if(Config.DatabaseHost == "" || Config.DatabaseName == "" || Config.DatabaseUsername == "" || Config.DatabasePassword == "")
                {
                    Logger.LogError($"{LogPrefix} Database connection information is missing. Please fill in the information in the config file.");
                    return false;
                }
                
                using (var connection = CreateConnection())
                {
                    await connection.OpenAsync();
                    
                    string query = @$"CREATE TABLE IF NOT EXISTS {Config.TableName}
                    (
                        id INTEGER unsigned NOT NULL auto_increment,
                        name varchar({IClientprefsApi.COOKIE_MAX_NAME_LENGTH}) NOT NULL UNIQUE,
                        description varchar({IClientprefsApi.COOKIE_MAX_DESCRIPTION_LENGTH}),
                        access INTEGER,
                        PRIMARY KEY (id)
                    )";

                    string query2 = @$"CREATE TABLE IF NOT EXISTS {Config.TableNamePlayerData}
                    (
                        steamid varchar(32) NOT NULL,
                        cookie_id int(10) NOT NULL,
                        value varchar({IClientprefsApi.COOKIE_MAX_VALUE_LENGTH}),
                        timestamp int NOT NULL,
                        PRIMARY KEY (steamid, cookie_id)
                    )";
                    
                    using (var transaction = await connection.BeginTransactionAsync())
                    {
                        await connection.ExecuteAsync(query, transaction: transaction);
                        await connection.ExecuteAsync(query2, transaction: transaction);

                        await transaction.CommitAsync();

                        query = @$"SELECT * FROM {Config.TableName}";

                        var rows = await connection.QueryAsync(query);

                        foreach (var row in rows)
                        {
                            if(g_ClientPrefs.Any(p => p.Id == (int)row.id || p.Name == row.name)) continue;
                            
                            if((int)row.id > g_iLatestClientprefID)
                            {
                                g_iLatestClientprefID = (int)row.id;
                            }
                            
                            g_ClientPrefs.Add(
                                new ClientPrefs
                                {
                                    Id = (int)row.id,
                                    Name = row.name,
                                    Description = row.description,
                                    Access = (CookieAccess)row.access,
                                }
                            );
                        }

                        g_bDatabaseLoaded = true;

                        if(g_iLatestClientprefID > 0)
                            g_iLatestClientprefID++;

                        Server.NextWorldUpdate(()=>
                        {
                            foreach(var p in Utilities.GetPlayers())
                            {
                                if(p == null || !p.IsValidPlayer())
                                {
                                    continue;
                                }
                                
                                var steamId = p.SteamID.ToString();
                                g_PlayerSettings[steamId] = new();
                                g_PlayerClientPrefs.Remove(steamId);
                                GetPlayerCookies(p, steamId);
                            }

                            // Timer so that other plugin can catch this event else this is called before AllPluginsLoaded
                            AddTimer(2.0f, ClientprefsApi.CallOnDatabaseLoaded);

                            Logger.LogInformation($"{LogPrefix} Database connection established.");
                        });
                    }
                    return true;
                }
            }
            else
            {
                string query = @$"CREATE TABLE IF NOT EXISTS {Config.TableName}
                (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name varchar({IClientprefsApi.COOKIE_MAX_NAME_LENGTH}) NOT NULL UNIQUE,
                    description varchar({IClientprefsApi.COOKIE_MAX_DESCRIPTION_LENGTH}),
                    access INTEGER
                )";

                string query2 = @$"CREATE TABLE IF NOT EXISTS {Config.TableNamePlayerData}
                (
                    steamid varchar(65) NOT NULL,
                    cookie_id int(10) NOT NULL,
                    value varchar({IClientprefsApi.COOKIE_MAX_VALUE_LENGTH}),
                    timestamp int,
                    PRIMARY KEY (steamid, cookie_id)
                )";

                using (var connection = new SqliteConnection(SQLiteDatasource))
                {
                    await connection.OpenAsync();

                    using(var transaction = await connection.BeginTransactionAsync())
                    {
                        /*
                        using (var command = new SqliteCommand(query, connection, transaction))
                        {
                            await command.ExecuteNonQueryAsync();
                        }

                        using (var command = new SqliteCommand(query2, connection, transaction))
                        {
                            await command.ExecuteNonQueryAsync();
                        }
                        */

                        await connection.ExecuteAsync(query, connection, transaction: transaction);
                        await connection.ExecuteAsync(query2, connection, transaction: transaction);

                        await transaction.CommitAsync();

                        query = @$"SELECT * FROM {Config.TableName}";

                        var rows = await connection.QueryAsync(query);

                        g_iLatestClientprefID = 0;

                        foreach (var row in rows)
                        {
                            if(g_ClientPrefs.Any(p => p.Id == (int)row.id || p.Name == row.name)) continue;
                            
                            if((int)row.id > g_iLatestClientprefID)
                            {
                                g_iLatestClientprefID = (int)row.id;
                            }
                            
                            g_ClientPrefs.Add(
                                new ClientPrefs
                                {
                                    Id = (int)row.id,
                                    Name = row.name,
                                    Description = row.description,
                                    Access = (CookieAccess)row.access,
                                }
                            );
                        }
                        
                        g_bDatabaseLoaded = true;

                        if(g_iLatestClientprefID > 0)
                            g_iLatestClientprefID++;
                        
                        Server.NextWorldUpdate(()=>
                        {
                            foreach(var p in Utilities.GetPlayers())
                            {
                                if(p == null || !p.IsValidPlayer())
                                {
                                    continue;
                                }
                                
                                var steamId = p.SteamID.ToString();
                                g_PlayerSettings[steamId] = new();
                                g_PlayerClientPrefs.Remove(steamId);
                                GetPlayerCookies(p, steamId);
                            }

                            // Timer so that other plugin can catch this event else this is called before AllPluginsLoaded
                            AddTimer(2.0f, ClientprefsApi.CallOnDatabaseLoaded);

                            Logger.LogInformation($"{LogPrefix} Database connection established.");
                        });
                    }
                }

                return true;
            }
        }
        catch (Exception ex)
        {
            Server.NextWorldUpdate(() => Logger.LogError($"{LogPrefix} Unable to connect to database: {ex.Message}"));
            throw;
        }
    }

    private void GetPlayerCookies(CCSPlayerController player, string steamId)
    {
        if(!g_bDatabaseLoaded)
        {
            return;
        }

        g_PlayerClientPrefs.Add(steamId, new List<PlayerClientPrefs>());

        if(Config.DatabaseType == "mysql")
        {
            Task.Run(async () =>
            {
                try
                {   
                    using (var connection = CreateConnection())
                    {
                        await connection.OpenAsync();
                        
                        string query = $"SELECT * FROM {Config.TableNamePlayerData} WHERE steamid = @steam";

                        var parameters = new DynamicParameters();
                        parameters.Add("@steam", steamId);
                        
                        var rows = await connection.QueryAsync(query, parameters);
                        
                        foreach (var row in rows)
                        {
                            g_PlayerClientPrefs[steamId].Add(
                                new PlayerClientPrefs
                                {
                                    Id = (int)row.cookie_id,
                                    OldValue = row.value,
                                    NewValue = row.value,
                                }
                            );
                        }

                        g_PlayerSettings[steamId].Loaded = true;

                        Server.NextWorldUpdate(()=> ClientprefsApi.CallOnPlayerCookiesCached(player));
                    }
                }
                catch (Exception ex)
                {
                    Server.NextWorldUpdate(() => Logger.LogError($"{LogPrefix} An error occurred while fetching player preferences: {ex.Message}"));
                    throw;
                }
            });
        }
        else
        {
            Task.Run(async () =>
            {
                try
                {    
                    using (var connection = new SqliteConnection(SQLiteDatasource))
                    {
                        await connection.OpenAsync();
                        
                        string query = $"SELECT * FROM {Config.TableNamePlayerData} WHERE steamid = @steam";
                        
                        var rows = await connection.QueryAsync(query, new { steam = steamId });
                        
                        foreach (var row in rows)
                        {
                            g_PlayerClientPrefs[steamId].Add(
                                new PlayerClientPrefs
                                {
                                    Id = (int)row.cookie_id,
                                    OldValue = row.value,
                                    NewValue = row.value,
                                }
                            );
                        }

                        g_PlayerSettings[steamId].Loaded = true;

                        Server.NextWorldUpdate(()=> ClientprefsApi.CallOnPlayerCookiesCached(player));
                    }
                }
                catch (Exception ex)
                {
                    Server.NextWorldUpdate(() => Logger.LogError($"{LogPrefix} An error occurred while fetching player preferences: {ex.Message}"));
                    throw;
                }
            });
        }
    }

    public bool CreatePlayerCookie(string name, string description, CookieAccess access)
    {
        if(!g_bDatabaseLoaded)
        {
            Logger.LogError($"{LogPrefix}[CreatePlayerCookie] Database is not loaded yet.");
            return false;
        }
        
        string query = @$"REPLACE INTO `{Config.TableName}`
            (name, description, access)
            VALUES (@name, @description, @access);";

        bool returnVal = true;
  
        if(Config.DatabaseType == "mysql")
        {
            Task.Run(async () =>
            {
                try
                {
                    using (var connection = CreateConnection())
                    {
                        await connection.OpenAsync();

                        var parameters = new DynamicParameters();
                        
                        parameters.Add("@name", name);
                        parameters.Add("@value", description);
                        parameters.Add("@access", (int)access);

                        await connection.QueryAsync(query, parameters);
                    }
                }
                catch (Exception ex)
                {
                    Server.NextWorldUpdate(() => Logger.LogError($"{LogPrefix} An error occurred while creating a player cookie: {ex.Message}"));
                    returnVal = false;
                    throw;
                }
            });
        }
        else
        {
            Task.Run(async () =>
            {
                try
                {
                    using (var connection = new SqliteConnection(SQLiteDatasource))
                    {
                        await connection.OpenAsync();

                        await connection.ExecuteAsync(query,
                            new { name, description, access = (int)access });
                    }
                }
                catch (Exception ex)
                {
                    Server.NextWorldUpdate(() => Logger.LogError($"{LogPrefix} An error occurred while creating a player cookie: {ex.Message}"));
                    returnVal = false;
                    throw;
                }
            });
        }
        return returnVal;
    }

    private void SavePlayerCookies(CCSPlayerController? player = null)
    {
        if(!g_bDatabaseLoaded)
        {
            return;
        }

        List<string> aPlayers = new();

        if(player != null)
        {
            var steamId = player.SteamID.ToString();
            
            if(!g_PlayerSettings.ContainsKey(steamId))
            {
                return;
            }

            if(!g_PlayerSettings[steamId].Loaded)
            {
                return;
            }
            
            if(!g_PlayerClientPrefs.ContainsKey(steamId))
            {
                return;
            }
            
            aPlayers.Add(steamId);
        }
        else
        {
            foreach(var p in Utilities.GetPlayers())
            {
                if(p == null || !p.IsValidPlayer())
                {
                    continue;
                }
                
                var steamId = p.SteamID.ToString();
                
                if(!g_PlayerSettings.ContainsKey(steamId))
                {
                    continue;
                }

                if(!g_PlayerSettings[steamId].Loaded)
                {
                    continue;
                }
                
                if(!g_PlayerClientPrefs.ContainsKey(steamId))
                {
                    continue;
                }
                
                aPlayers.Add(steamId);
            }
        }

        int time = GetEpochTime();
            
        if(Config.DatabaseType == "mysql")
        {
            Task.Run(async () =>
            {
                try
                {
                    using (var connection = CreateConnection())
                    {
                        await connection.OpenAsync();
                        
                        string query;

                        var parameters = new DynamicParameters();
                        
                        using (var transaction = await connection.BeginTransactionAsync())
                        {
                            foreach (var steamId in aPlayers)
                            {
                                foreach (var pref in g_PlayerClientPrefs[steamId])
                                {
                                    if(pref.OldValue == pref.NewValue) continue;

                                    var p = g_ClientPrefs.Where(p => p.Id == pref.Id).FirstOrDefault();
                                    if(p == null) continue;

                                    query = @$"REPLACE INTO `{Config.TableNamePlayerData}`
                                    (steamid, cookie_id, value, timestamp)
                                    VALUES (@steam, @id, @value, @timestamp);";

                                    parameters = new DynamicParameters();
                                    parameters.Add("@steam", steamId);
                                    parameters.Add("@id", pref.Id);
                                    parameters.Add("@value", pref.NewValue);
                                    parameters.Add("@timestamp", time);

                                    await connection.ExecuteAsync(query, parameters, transaction: transaction);
                                }

                                Server.NextWorldUpdate(() => 
                                {
                                    Logger.LogInformation($"{LogPrefix} Player data saved.");
                                    g_PlayerClientPrefs.Remove(steamId);
                                    g_PlayerSettings.Remove(steamId);
                                });
                            }
                            await transaction.CommitAsync();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Server.NextWorldUpdate(() => Logger.LogError($"{LogPrefix} An error occurred while saving player(s) data: {ex.Message}"));
                    throw;
                }
            });
        }
        else
        {
            Task.Run(async () =>
            {
                try
                {
                    using (var connection = new SqliteConnection(SQLiteDatasource))
                    {
                        await connection.OpenAsync();
                        
                        string query;
                        
                        using (var transaction = await connection.BeginTransactionAsync())
                        {
                            foreach (var steamId in aPlayers)
                            {
                                foreach (var pref in g_PlayerClientPrefs[steamId])
                                {
                                    if(pref.OldValue == pref.NewValue) continue;

                                    var p = g_ClientPrefs.Where(p => p.Id == pref.Id).FirstOrDefault();
                                    if(p == null) continue;

                                    query = @$"REPLACE INTO `{Config.TableNamePlayerData}`
                                    (steamid, cookie_id, value, timestamp)
                                    VALUES (@steam, @id, @value, @timestamp);";

                                    await connection.ExecuteAsync(query,
                                    new { steam=steamId, id=pref.Id, value=pref.NewValue, timestamp=time },
                                    transaction: transaction);
                                }

                                Server.NextWorldUpdate(() => 
                                {
                                    Logger.LogInformation($"{LogPrefix} Player data saved.");
                                    g_PlayerClientPrefs.Remove(steamId);
                                    g_PlayerSettings.Remove(steamId);
                                });
                            }
                            await transaction.CommitAsync();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Server.NextWorldUpdate(() => Logger.LogError($"{LogPrefix} An error occurred while saving player(s) data: {ex.Message}"));
                    throw;
                }
            });
        }
    }
}