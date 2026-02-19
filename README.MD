# Clientprefs for CSSharp
This plugin exposes some natives for developers to save player data to SQLite / MySQL without actually adding sql code to your plugin giving developers easy access to save player cookie to database.

## Example
```c#
using Clientprefs.API;

private readonly PluginCapability<IClientprefsApi> PluginCapability = new("Clientprefs");
private IClientprefsApi? ClientprefsApi;
private int CookieID = 0;
private Dictionary<CCSPlayerController, string> PlayerCookie = new();

public override void Unload(bool hotReload)
{
    base.Unload(hotReload);

    if (ClientprefsApi == null) return;

    ClientprefsApi.OnDatabaseLoaded -= OnClientprefDatabaseReady;
    ClientprefsApi.OnPlayerCookiesCached -= OnPlayerCookiesCached;
}

public override void OnAllPluginsLoaded(bool hotReload)
{
    ClientprefsApi = PluginCapability.Get();

    if (ClientprefsApi == null) return;

    ClientprefsApi.OnDatabaseLoaded += OnClientprefDatabaseReady;
    ClientprefsApi.OnPlayerCookiesCached += OnPlayerCookiesCached;

    if (hotReload)
    {
        if (ClientprefsApi == null || CookieID == -1) return;

        foreach (var player in Utilities.GetPlayers().Where(p => !p.IsBot))
        {
            if (!ClientprefsApi.ArePlayerCookiesCached(player)) continue;
            PlayerCookie[player] = ClientprefsApi.GetPlayerCookie(player, CookieID);
        }
    }
}

public void OnClientprefDatabaseReady()
{
    if (ClientprefsApi == null) return;

    Task.Run(async () =>
    {
        CookieID = ClientprefsApi.RegPlayerCookieAsync("example_cookie", "Example cookie description", CookieAccess.CookieAccess_Public);

        if(CookieID == -1)
        {
            Logger.LogError("[Clientprefs-Example] Failed to register/load cookie 1");
            return;
        }

        Logger.LogInformation($"[Clientprefs-Example] Registered/Loaded cookie with ID: {CookieID}"); // ID: 1
    });
}

public void OnPlayerCookiesCached(CCSPlayerController player)
{
    if (ClientprefsApi == null || CookieID == -1) return;

    PlayerCookie[player] = ClientprefsApi.GetPlayerCookie(player, CookieID);

    Logger.LogInformation($"[Clientprefs-Example] Cookie value: {cookieValue}");
}

[ConsoleCommand("css_clientprefs_example", "Saves example clientprefs cookie value")]
public void OnExampleCommand(CCSPlayerController? caller, CommandInfo _)
{
    if (caller == null || !caller.IsValid || ClientprefsApi == null || CookieID == -1)
    {
        return;
    }

    ClientprefsApi.SetPlayerCookie(caller, CookieID, PlayerCookie[player]);
}
```

## Config
```json
{
  "TableName": "css_cookies",
  "TableNamePlayerData": "css_cookies_playerdata",
  "DatabaseType": "sqlite",
  "DatabaseHost": "",
  "DatabaseName": "",
  "DatabaseUsername": "",
  "DatabasePassword": "",
  "DatabasePort": 3306,
  "DatabaseSslmode": "none",
  "Debug": false,
  "ConfigVersion": 1
}
```

## Things left to implement
[ ] css_settings

## Changelog
- v1.0.7
    - refactor: whole codebase.
    - fix: cases where player cookies were not getting saved.

- v1.0.6
    - fix: Exceptions on player cached & disconnect.
- v1.0.4
    - fix: CreatePlayerCookie native using wrong parameter name.
    - fix: SavePlayerCookie missing not operator.
    - fix: Player preferences not getting saved at map change.
- v1.0.3
    - fix: Plugin will no longer throw "Native d01e4eb5 was invoked on a non-main thread" on playerConnect.
    - Devs can now save clientpref for player at PlayerDisconnect(HookMode.Post) too as the plugin now saves in database after 0.5 seconds.
    - Added more error logging and added variable in config to enable debug logs.
- v1.0.2
    - Fixed dictionary errors for players after map change.
- v1.0.1
    - Changed hook method of OnClientprefDatabaseReady & OnPlayerCookiesCached. Please re-refer ClientprefsExample.