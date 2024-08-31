using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;

using Clientprefs.API;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Commands;
using Microsoft.Extensions.Logging;

namespace Clientprefs;

[MinimumApiVersion(215)]
public partial class Clientprefs : BasePlugin, IPluginConfig<ClientprefsConfig>
{
    public override string ModuleName => "Clientprefs";
    public override string ModuleDescription => "Clientprefs plugin for CounterStrikeSharp";
    public override string ModuleAuthor => "Cruze";
    public override string ModuleVersion => "1.0.3Fix";

    public class ClientPrefs
    {
        public int Id { get; set; } = -1;
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public CookieAccess Access { get; set; } = CookieAccess.CookieAccess_Public;

        public ClientPrefs()
        {
            Id = -1;
            Name = "";
            Description = "";
            Access = CookieAccess.CookieAccess_Public;
        }
    }

    public class PlayerClientPrefs
    {
        public int Id { get; set; } = -1;
        public string OldValue { get; set; } = "";
        public string NewValue { get; set; } = "";  // save if oldvalue != newvalue

        public PlayerClientPrefs()
        {
            Id = -1;
            OldValue = "";
            NewValue = "";
        }
    }

    public class PlayerSettings
    {
        public bool Loaded { get; set; } = false;

        public PlayerSettings()
        {
            Loaded = false;
        }
    }

    private bool g_bDatabaseLoaded = false;

    public int g_iLatestClientprefID = 0;

    private const string LogPrefix = "[Clientprefs]";

    public ClientprefsConfig Config { get; set; } = new();
    public PluginCapability<IClientprefsApi> g_PluginCapability = new("Clientprefs");
    public required ClientprefsApi ClientprefsApi { get; set; }
    public List<ClientPrefs> g_ClientPrefs = new();
    public Dictionary<string, List<PlayerClientPrefs>> g_PlayerClientPrefs = new();
    public Dictionary<string, PlayerSettings> g_PlayerSettings = new();


    public void OnConfigParsed(ClientprefsConfig config)
    {
        Config = config;
    }

    public override void Load(bool hotReload)
    {
        base.Load(hotReload);

        g_ClientPrefs = new();
        g_PlayerClientPrefs = new();
        g_PlayerSettings = new();

        ClientprefsApi = new ClientprefsApi(this);
        Capabilities.RegisterPluginCapability(g_PluginCapability, () => ClientprefsApi);

        g_bDatabaseLoaded = false;
        Database_OnPluginLoad();

        RegisterListener<Listeners.OnMapStart>((mapname)=>
        {
            g_PlayerClientPrefs = new();
        });

        Task.Run(ConnectDatabaseTable).Wait();
    }

    public override void Unload(bool hotReload)
    {
        base.Unload(hotReload);

        SavePlayerCookies();
    }

    [ConsoleCommand("css_cookies", "sm_cookies <name> [value]")]
	[ConsoleCommand("css_cookie", "sm_cookie <name> [value]")]
	public void OnCookiesCommand(CCSPlayerController? player, CommandInfo command)
	{
        if (command.ArgCount <= 1)
        {
            command.ReplyToCommand("[CSS] " + Localizer["Cookie Usage"]);
            command.ReplyToCommand("[CSS] " + Localizer["Printing Cookie List"]);

            int count = 1;
            foreach(var pref in g_ClientPrefs)
            {
                command.ReplyToCommand($"[CSS] [{count}] {pref.Name} {pref.Description}");
                count++;
            }
            return;
        }
        
        if (player == null || !player.IsValid)
        {
            command.ReplyToCommand("[CSS] " + Localizer["No Console"]);
            return;
        }

        var name = command.GetArg(1);

        int cookie = FindPlayerCookie(name);

        if (cookie < 0)
        {
            command.ReplyToCommand("[CSS] " + Localizer["Cookie not Found", name]);
            return;
        }

        CookieAccess access = GetCookieAccess(cookie);

        if (access == CookieAccess.CookieAccess_Private)
        {
            command.ReplyToCommand("[CSS] " + Localizer["Cookie not Found", name]);
            return;
        }

        var steamId = player.SteamID.ToString();

        string value = g_PlayerClientPrefs[steamId].First(p => p.Id == cookie).NewValue;
        string description = g_ClientPrefs.First(p => p.Id == cookie).Description;
		
        command.ReplyToCommand($"[CSS] " + Localizer["Cookie Value", name, description, value]);

        if (access == CookieAccess.CookieAccess_Protected)
        {
            command.ReplyToCommand($"[CSS] " + Localizer["Protected Cookie"]);
            return;
        }

        value = command.GetArg(2);
        
        g_PlayerClientPrefs[steamId].First(p => p.Id == cookie).NewValue = value;
        command.ReplyToCommand("[CSS] " + Localizer["Cookie Changed Value", name, value]);
    }

    [ConsoleCommand("css_settings", "Settings command for clientprefs")]
	public void OnSettingsCommand(CCSPlayerController? player, CommandInfo command)
	{
        if (player == null || !player.IsValid)
        {
            command.ReplyToCommand("[CSS] " + Localizer["No Console"]);
            return;
        }
        command.ReplyToCommand("[CSS] Not yet implemented");
        // ClientprefsApi.ShowCookieMenu(player);
    }

    [GameEventHandler]
    public HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo _)
    {
        var player = @event.Userid;

        if (player == null || !player.IsValidPlayer())
        {
            return HookResult.Continue;
        }

        var steamId = player.SteamID.ToString();
        g_PlayerSettings[steamId] = new();
        GetPlayerCookies(player, steamId);
        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo _)
    {
        var player = @event.Userid;

        if (player == null || !player.IsValidPlayer() || @event.Reason == 1)
        {
            return HookResult.Continue;
        }

        var steamId = player.SteamID.ToString();
        AddTimer(0.5f, () => SavePlayerCookies(steamId)); // So that devs can save prefs at player disconnect safely
        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnMatchEnd(EventCsWinPanelMatch _, GameEventInfo __)
    {
        SavePlayerCookies();
        return HookResult.Continue;
    }

    private int GetEpochTime()
    {
        return (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

    private void DebugLog(string message)
    {
        if (Config.Debug)
        {
            Logger.LogInformation($"{LogPrefix} {message}");
        }
    }
}