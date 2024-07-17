> [!WARNING]
> This plugin is not heavily tested so if any issue found, please open a ticket and describe the issue as much as you can.

> [!CAUTION]
> Do not use `SetPlayerCookie` at `EventPlayerDisconnect (HookMode.Post)` event as that is when the main plugin saves cookies of a player. (Use HookMode.Pre) or when some command is executed like in example.

# Clientprefs for CSSharp
This plugin exposes some natives for developers to save player data to SQLite / MySQL without actually adding sql code to your plugin giving developers easy access to save player cookie to database.

## Example
```c#
using Clientprefs.API;

private readonly PluginCapability<IClientprefsApi> g_PluginCapability = new("Clientprefs");
private IClientprefsApi? ClientprefsApi;
private int g_iCookieID = 0;

public override void OnAllPluginsLoaded(bool hotReload)
{
    ClientprefsApi = g_PluginCapability.Get();

    if (ClientprefsApi == null) return;

    ClientprefsApi.HookRegisterCookie(OnClientprefDatabaseReady);
    ClientprefsApi.HookPlayerCache(OnPlayerCookiesCached);
}

public override void Unload(bool hotReload)
{
    base.Unload(hotReload);

    if (ClientprefsApi == null) return;

    ClientprefsApi.UnhookRegisterCookie(OnClientprefDatabaseReady);
    ClientprefsApi.UnhookPlayerCache(OnPlayerCookiesCached);
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
    
    var cookieValue = ClientprefsApi.GetPlayerCookie(player, g_iCookieID);

    Logger.LogInformation($"[Clientprefs-Example] Cookie value: {cookieValue}");
}

[ConsoleCommand("css_clientprefs_example", "Saves example clientprefs cookie value")]
public void OnExampleCommand(CCSPlayerController? caller, CommandInfo _)
{
    if (caller == null || !caller.IsValid || ClientprefsApi == null || g_iCookieID == -1)
    {
        return;
    }

    ClientprefsApi.SetPlayerCookie(caller, g_iCookieID, "xyz");
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
  "ConfigVersion": 1
}
```

## Things left to implement
[ ] css_settings

