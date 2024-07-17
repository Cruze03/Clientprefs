using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Core.Attributes;

using Clientprefs.API;
using CounterStrikeSharp.API.Core.Capabilities;
using Microsoft.Extensions.Logging;

namespace ClientPrefsExample;

[MinimumApiVersion(215)]
public class ClientPrefsExample : BasePlugin
{
    public override string ModuleName => "Example plugin";
    public override string ModuleDescription => "Example plugin Description";
    public override string ModuleAuthor => "Cruze";
    public override string ModuleVersion => "1.0.0";

    private readonly PluginCapability<IClientprefsApi> g_PluginCapability = new("Clientprefs");

    private IClientprefsApi? ClientprefsApi;

    private int g_iCookieID = -1, g_iCookieID2 = -1, g_iCookieID3 = -1;

    public override void Load(bool hotReload)
    {
        base.Load(hotReload);
    }

    public override void Unload(bool hotReload)
    {
        base.Unload(hotReload);

        if (ClientprefsApi == null) return;

        ClientprefsApi.UnhookRegisterCookie(OnClientprefDatabaseReady);
        ClientprefsApi.UnhookPlayerCache(OnPlayerCookiesCached);
    }

    public override void OnAllPluginsLoaded(bool hotReload)
    {
        ClientprefsApi = g_PluginCapability.Get();

        if (ClientprefsApi == null) return;

        ClientprefsApi.HookRegisterCookie(OnClientprefDatabaseReady);
        ClientprefsApi.HookPlayerCache(OnPlayerCookiesCached);
    }

    public void OnClientprefDatabaseReady()
    {
        if (ClientprefsApi == null) return;
        
        g_iCookieID = ClientprefsApi.RegPlayerCookie("example_cookie", "Example cookie description", CookieAccess.CookieAccess_Public);
        g_iCookieID2 = ClientprefsApi.RegPlayerCookie("example_cookie2", "Example cookie description", CookieAccess.CookieAccess_Public);
        g_iCookieID3 = ClientprefsApi.RegPlayerCookie("example_cookie", "Example cookie description", CookieAccess.CookieAccess_Public);

        if(g_iCookieID == -1)
        {
            Logger.LogError("[Clientprefs-Example] Failed to register/load cookie 1");
            return;
        }

        if(g_iCookieID2 == -1)
        {
            Logger.LogError("[Clientprefs-Example] Failed to register/load cookie 2");
            return;
        }

        Logger.LogInformation($"[Clientprefs-Example] Registered/Loaded cookie with ID: {g_iCookieID}"); // ID: 1
        Logger.LogInformation($"[Clientprefs-Example] Registered/Loaded cookie with ID: {g_iCookieID2}"); // ID: 2
        Logger.LogInformation($"[Clientprefs-Example] Registered/Loaded cookie with ID: {g_iCookieID3}"); // ID: 1
    }

    public void OnPlayerCookiesCached(CCSPlayerController player)
    {
        if (ClientprefsApi == null || g_iCookieID == -1 || g_iCookieID2 == -1) return;
        
        var cookieValue = ClientprefsApi.GetPlayerCookie(player, g_iCookieID);
        var cookieValue2 = ClientprefsApi.GetPlayerCookie(player, g_iCookieID2);

        Logger.LogInformation($"[Clientprefs-Example] Cookie value: {cookieValue}");
        Logger.LogInformation($"[Clientprefs-Example] Cookie value 2: {cookieValue2}");
    }

    [ConsoleCommand("css_clientprefs_example", "Saves example clientprefs cookie value")]
    public void OnExampleCommand(CCSPlayerController? caller, CommandInfo _)
    {
        if (caller == null || !caller.IsValid || ClientprefsApi == null || g_iCookieID == -1)
        {
            return;
        }

        ClientprefsApi.SetPlayerCookie(caller, g_iCookieID, "xyz");
        ClientprefsApi.SetPlayerCookie(caller, g_iCookieID2, "abc");
        ClientprefsApi.SetPlayerCookie(caller, g_iCookieID3, "xyz");
    }
}