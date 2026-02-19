using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Core.Attributes;

using Clientprefs.API;
using CounterStrikeSharp.API.Core.Capabilities;
using Microsoft.Extensions.Logging;

namespace ClientPrefsExample;

[MinimumApiVersion(361)]
public class ClientPrefsExample : BasePlugin
{
    public override string ModuleName => "Example plugin";
    public override string ModuleDescription => "Example plugin Description";
    public override string ModuleAuthor => "Cruze";
    public override string ModuleVersion => "1.0.0";

    private readonly PluginCapability<IClientprefsApi> PluginCapability = new("Clientprefs");

    private IClientprefsApi? ClientprefsApi;

    private int CookieID = -1, CookieID2 = -1, CookieID3 = -1;

    public override void Load(bool hotReload)
    {
        base.Load(hotReload);
    }

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
    }

    public void OnClientprefDatabaseReady()
    {
        if (ClientprefsApi == null) return;

        Task.Run(async () =>
        {
            CookieID = await ClientprefsApi.RegPlayerCookieAsync("example_cookie", "Example cookie description", CookieAccess.CookieAccess_Public);
            CookieID2 = await ClientprefsApi.RegPlayerCookieAsync("example_cookie2", "Example cookie description", CookieAccess.CookieAccess_Public);
            CookieID3 = await ClientprefsApi.RegPlayerCookieAsync("example_cookie", "Example cookie description", CookieAccess.CookieAccess_Public);

            if (CookieID == -1)
            {
                Logger.LogError("[Clientprefs-Example] Failed to register/load cookie 1");
                return;
            }

            if (CookieID2 == -1)
            {
                Logger.LogError("[Clientprefs-Example] Failed to register/load cookie 2");
                return;
            }

            Logger.LogInformation($"[Clientprefs-Example] Registered/Loaded cookie with ID: {CookieID}"); // ID: 1
            Logger.LogInformation($"[Clientprefs-Example] Registered/Loaded cookie with ID: {CookieID2}"); // ID: 2
            Logger.LogInformation($"[Clientprefs-Example] Registered/Loaded cookie with ID: {CookieID3}"); // ID: 1
        });
    }

    public void OnPlayerCookiesCached(CCSPlayerController player)
    {
        if (ClientprefsApi == null || CookieID == -1 || CookieID2 == -1) return;

        var cookieValue = ClientprefsApi.GetPlayerCookie(player, CookieID);
        var cookieValue2 = ClientprefsApi.GetPlayerCookie(player, CookieID2);

        Logger.LogInformation($"[Clientprefs-Example] Cookie value: {cookieValue}");
        Logger.LogInformation($"[Clientprefs-Example] Cookie value 2: {cookieValue2}");
    }

    [ConsoleCommand("css_clientprefs_example", "Saves example clientprefs cookie value")]
    public void OnExampleCommand(CCSPlayerController? caller, CommandInfo _)
    {
        if (caller == null || !caller.IsValid || ClientprefsApi == null || CookieID == -1)
        {
            return;
        }

        ClientprefsApi.SetPlayerCookie(caller, CookieID, "xyz");
        ClientprefsApi.SetPlayerCookie(caller, CookieID2, "abc");
        ClientprefsApi.SetPlayerCookie(caller, CookieID3, "xyz");
    }
}