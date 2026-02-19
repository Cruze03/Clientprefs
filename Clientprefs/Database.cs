using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

using Dapper;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using System.Data.Common;
using Microsoft.Data.Sqlite;


using Clientprefs.API;

namespace Clientprefs;

public partial class Clientprefs
{
	private bool IsMySQL()
	{
		return Config.DatabaseType.Equals("mysql", StringComparison.OrdinalIgnoreCase);
	}

	private DbConnection CreateConnection()
	{
		if (IsMySQL())
		{
			MySqlConnectionStringBuilder builder = new MySqlConnectionStringBuilder
			{
				Server = Config.DatabaseHost,
				UserID = Config.DatabaseUsername,
				Password = Config.DatabasePassword,
				Database = Config.DatabaseName,
				Port = (uint)Config.DatabasePort,
				SslMode = Enum.Parse<MySqlSslMode>(Config.DatabaseSslmode, true),
				AllowUserVariables = true,
			};
			return new MySqlConnection(builder.ToString());
		}
		return new SqliteConnection($"Data Source={Path.Join(ModuleDirectory, "clientprefs.db")}");
	}

	private async Task<bool> ConnectDatabaseTable()
	{
		try
		{
			Cookies.Clear();
			LatestClientprefID = 0;

			string cookiequery = @$"CREATE TABLE IF NOT EXISTS {Config.TableName}
            (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name varchar({IClientprefsApi.COOKIE_MAX_NAME_LENGTH}) NOT NULL UNIQUE,
                description varchar({IClientprefsApi.COOKIE_MAX_DESCRIPTION_LENGTH}),
                access INTEGER
            )";
			string playerquery = @$"CREATE TABLE IF NOT EXISTS {Config.TableNamePlayerData}
            (
                steamid varchar(65) NOT NULL,
                cookie_id int(10) NOT NULL,
                value varchar({IClientprefsApi.COOKIE_MAX_VALUE_LENGTH}),
                timestamp int,
                PRIMARY KEY (steamid, cookie_id)
            )";

			if (IsMySQL())
			{
				if (Config.DatabaseHost == "" || Config.DatabaseName == "" || Config.DatabaseUsername == "" || Config.DatabasePassword == "")
				{
					Logger.LogError($"Database connection information is missing. Please fill in the information in the config file.");
					return false;
				}

				cookiequery = @$"CREATE TABLE IF NOT EXISTS {Config.TableName}
                (
                    id INTEGER unsigned NOT NULL auto_increment,
                    name varchar({IClientprefsApi.COOKIE_MAX_NAME_LENGTH}) NOT NULL UNIQUE,
                    description varchar({IClientprefsApi.COOKIE_MAX_DESCRIPTION_LENGTH}),
                    access INTEGER,
                    PRIMARY KEY (id)
                )";

				playerquery = @$"CREATE TABLE IF NOT EXISTS {Config.TableNamePlayerData}
                (
                    steamid varchar(32) NOT NULL,
                    cookie_id int(10) NOT NULL,
                    value varchar({IClientprefsApi.COOKIE_MAX_VALUE_LENGTH}),
                    timestamp int NOT NULL,
                    PRIMARY KEY (steamid, cookie_id)
                )";
			}

			using (var connection = CreateConnection())
			{
				await connection.OpenAsync();

				using (var transaction = await connection.BeginTransactionAsync())
				{
					await connection.ExecuteAsync(cookiequery, transaction: transaction);
					await connection.ExecuteAsync(playerquery, transaction: transaction);

					await transaction.CommitAsync();

					cookiequery = @$"SELECT * FROM {Config.TableName}";

					var rows = await connection.QueryAsync(cookiequery);

					foreach (var row in rows)
					{
						if (Cookies.Any(p => p.Id == (int)row.id || p.Name == row.name)) continue;

						if ((int)row.id > LatestClientprefID)
						{
							LatestClientprefID = (int)row.id;
						}

						Cookies.Add(
							new Cookie
							{
								Id = (int)row.id,
								Name = row.name,
								Description = row.description,
								Access = (CookieAccess)row.access,
							}
						);
					}

					_databaseLoaded = true;

					if (LatestClientprefID > 0)
						LatestClientprefID++;

					Server.NextWorldUpdate(() =>
					{
						foreach (var p in Utilities.GetPlayers())
						{
							if (p == null || !p.IsValidPlayer())
							{
								continue;
							}

							var steamId = p.SteamID.ToString();
							GetPlayerCookies(p, steamId);
						}

						// Timer so that other plugin can catch this event else this is called before AllPluginsLoaded
						AddTimer(2.0f, ClientprefsApi.CallOnDatabaseLoaded);

						DebugLog("Database connection established.");
					});
				}
				return true;
			}
		}
		catch (Exception ex)
		{
			Server.NextWorldUpdate(() => Logger.LogError($"Unable to connect to database: {ex.Message}"));
			return false;
		}
	}

	private void GetPlayerCookies(CCSPlayerController player, string steamId)
	{
		if (!_databaseLoaded)
		{
			Logger.LogError($"GetPlayerCookies called when Database is not loaded yet.");
			return;
		}

		if (!PlayerSettings.ContainsKey(steamId))
		{
			PlayerSettings.Add(steamId, new());
		}

		Task.Run(async () =>
		{
			try
			{
				using (var connection = CreateConnection())
				{
					await connection.OpenAsync();

					string query = $"SELECT * FROM {Config.TableNamePlayerData} WHERE steamid = @steam";

					var rows = await connection.QueryAsync(query, new { steam = steamId });

					foreach (var row in rows)
					{
						PlayerSettings[steamId].ModifyCookie((int)row.cookie_id, row.value, row.value);
					}

					PlayerSettings[steamId].Loaded = true;

					DebugLog($"Cookies for {steamId} loaded: {PlayerSettings[steamId].CookieCount}");

					Server.NextWorldUpdate(() =>
					{
						if (!player.IsValidPlayer()) return;

						ClientprefsApi.CallOnPlayerCookiesCached(player);
					});
				}
			}
			catch (Exception ex)
			{
				Server.NextWorldUpdate(() => Logger.LogError($"An error occurred while fetching player preferences: {ex.Message}"));
			}
		});
	}

	public bool CreatePlayerCookie(string name, string description, CookieAccess access)
	{
		if (!_databaseLoaded)
		{
			Logger.LogError($"CreatePlayerCookie called when Database is not loaded yet.");
			return false;
		}

		string query = @$"REPLACE INTO `{Config.TableName}`
            (name, description, access)
            VALUES (@name, @description, @access);";

		Task.Run(async () =>
		{
			try
			{
				using (var connection = CreateConnection())
				{
					await connection.OpenAsync();

					await connection.QueryAsync(query, new { name, description, access = (int)access });

					DebugLog($"Created playercookie with name {name}");
				}
			}
			catch (Exception ex)
			{
				Server.NextWorldUpdate(() => Logger.LogError($"An error occurred while creating a player cookie: {ex.Message}"));
				return;
			}
		});
		return true;
	}

	public async Task<bool> CreatePlayerCookieNew(string name, string description, CookieAccess access)
	{
		if (!_databaseLoaded)
		{
			Logger.LogError($"CreatePlayerCookieNew called when Database is not loaded yet.");
			return false;
		}

		string query = @$"REPLACE INTO `{Config.TableName}`
            (name, description, access)
            VALUES (@name, @description, @access);";

		try
		{
			using (var connection = CreateConnection())
			{
				await connection.OpenAsync();

				await connection.QueryAsync(query, new { name, description, access = (int)access });

				DebugLog($"Created playercookienew with name {name}");
			}
		}
		catch (Exception ex)
		{
			Server.NextWorldUpdate(() => Logger.LogError($"An error occurred while creating a player cookie new: {ex.Message}"));
			return false;
		}
		return true;
	}

	private void SavePlayerCookies(string steamId64 = "")
	{
		if (!_databaseLoaded)
		{
			Logger.LogError($"SavePlayerCookies called when Database is not loaded yet.");
			return;
		}

		var aPlayers = new Dictionary<string, List<ClientCookie>>();

		if (!string.IsNullOrEmpty(steamId64))
		{
			if (!PlayerSettings.ContainsKey(steamId64))
			{
				return;
			}

			if (!PlayerSettings[steamId64].Loaded)
			{
				return;
			}

			aPlayers.Add(steamId64, [.. PlayerSettings[steamId64].Cookies]);
			PlayerSettings.Remove(steamId64);
		}
		else
		{
			foreach (var pair in PlayerSettings)
			{
				var steamId = pair.Key;
				var value = pair.Value;

				if (!value.Loaded)
				{
					continue;
				}

				aPlayers.Add(steamId, [.. value.Cookies]);
				PlayerSettings.Remove(steamId);
			}
		}

		if (aPlayers.Count == 0) return;

		int time = GetEpochTime();

		Task.Run(async () =>
		{
			try
			{
				using (var connection = CreateConnection())
				{
					await connection.OpenAsync();

					string query;

					using (var transaction = await connection.BeginTransactionAsync())
					{
						foreach (var pair in aPlayers)
						{
							var steamId = pair.Key;
							var cookies = pair.Value;

							foreach (var pref in cookies)
							{
								var isNewValueNull = string.IsNullOrWhiteSpace(pref.NewValue);

								if (pref.OldValue == pref.NewValue && !isNewValueNull) continue;

								var p = Cookies.FirstOrDefault(p => p.Id == pref.Id);
								if (p == null) continue;

								if (isNewValueNull)
								{
									query = @$"DELETE FROM `{Config.TableNamePlayerData}`
									WHERE steamid = @steam AND cookie_id = @id;";

									await connection.ExecuteAsync(query,
									new { steam = steamId, id = pref.Id },
									transaction: transaction);
									DebugLog($"Saving cookies {pref.Id} for {steamId}");
									continue;
								}

								query = @$"REPLACE INTO `{Config.TableNamePlayerData}`
                                (steamid, cookie_id, value, timestamp)
                                VALUES (@steam, @id, @value, @timestamp);";

								await connection.ExecuteAsync(query,
								new { steam = steamId, id = pref.Id, value = pref.NewValue, timestamp = time },
								transaction: transaction);
								DebugLog($"Saving cookies {pref.Id} for {steamId}");
							}
						}
						await transaction.CommitAsync();
						DebugLog($"Saved cookies");
					}
				}
			}
			catch (Exception ex)
			{
				Server.NextWorldUpdate(() => Logger.LogError($"An error occurred while saving player(s) data: {ex.Message}"));
				throw;
			}
		});
	}
}