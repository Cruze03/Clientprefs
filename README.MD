# Clientprefs for CSSharp
This plugin exposes some natives for developers to save player data to SQLite / MySQL without actually adding sql code to your plugin giving developers easy access to save player cookie to database.

## Example
```c#
using Clientprefs.API;

private readonly PluginCapability<IClientprefsApi> g_PluginCapability = new("Clientprefs");
private IClientprefsApi? ClientprefsApi;
private int g_iCookieID = 0;
private Dictionary<CCSPlayerController, string> playerCookie = new();

public override void Unload(bool hotReload)
{
    base.Unload(hotReload);

    if (ClientprefsApi == null) return;

    ClientprefsApi.OnDatabaseLoaded -= OnClientprefDatabaseReady;
    ClientprefsApi.OnPlayerCookiesCached -= OnPlayerCookiesCached;
}

public override void OnAllPluginsLoaded(bool hotReload)
{
    ClientprefsApi = g_PluginCapability.Get();

    if (ClientprefsApi == null) return;

    ClientprefsApi.OnDatabaseLoaded += OnClientprefDatabaseReady;
    ClientprefsApi.OnPlayerCookiesCached += OnPlayerCookiesCached;

    if (hotReload)
    {
        if (ClientprefsApi == null || g_iCookieID == -1) return;

        foreach (var player in Utilities.GetPlayers().Where(p => !p.IsBot))
        {
            if (!ClientprefsApi.ArePlayerCookiesCached(player)) continue;
            playerCookie[player] = ClientprefsApi.GetPlayerCookie(player, g_iCookieID);
        }
    }
}

public void OnClientprefDatabaseReady()
{
    if (ClientprefsApi == null) return;
    
    g_iCookieID = ClientprefsApi.RegPlayerCookie("example_cookie", "Example cookie description", CookieAccess.CookieAccess_Public);

    if(g_iCookieID == -1)
    {
        Logger.LogError("[Clientprefs-Example] Failed to register/load cookie 1");
        return;
    }

    Logger.LogInformation($"[Clientprefs-Example] Registered/Loaded cookie with ID: {g_iCookieID}"); // ID: 1
}

public void OnPlayerCookiesCached(CCSPlayerController player)
{
    if (ClientprefsApi == null || g_iCookieID == -1) return;
    
    playerCookie[player] = ClientprefsApi.GetPlayerCookie(player, g_iCookieID);

    Logger.LogInformation($"[Clientprefs-Example] Cookie value: {cookieValue}");
}

[ConsoleCommand("css_clientprefs_example", "Saves example clientprefs cookie value")]
public void OnExampleCommand(CCSPlayerController? caller, CommandInfo _)
{
    if (caller == null || !caller.IsValid || ClientprefsApi == null || g_iCookieID == -1)
    {
        return;
    }

    ClientprefsApi.SetPlayerCookie(caller, g_iCookieID, playerCookie[player]);
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
  "DatabaseSslmode": "",
  "Debug": false,
  "ConfigVersion": 1
}
```

## Things left to implement
[ ] css_settings

## Changelog
- v1.0.3
    - fix: Plugin will no longer throw "Native d01e4eb5 was invoked on a non-main thread" on playerConnect.
    - Devs can now save clientpref for player at PlayerDisconnect(HookMode.Post) too as the plugin now saves in database after 0.5 seconds.
    - Added more error logging and added variable in config to enable debug logs.
- v1.0.2
    - Fixed dictionary errors for players after map change.
- v1.0.1
    - Changed hook method of OnClientprefDatabaseReady & OnPlayerCookiesCached. Please re-refer ClientprefsExample.