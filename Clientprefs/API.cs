using CounterStrikeSharp.API.Core;
using Clientprefs.API;
using CounterStrikeSharp.API.Modules.Entities;
using Microsoft.Extensions.Logging;

namespace Clientprefs;

public partial class Clientprefs
{
    public void AddClientprefCommands(string name, string description, CookieAccess access)
    {
        g_ClientPrefs.Add(new ClientPrefs()
        {
            Id = g_iLatestClientprefID,
            Name = name,
            Description = description,
            Access = access
        });
    }

    public int FindPlayerCookie(string name)
    {
        if(ClientPrefExists(name))
        {
            return GetClientPrefByName(name);
        }
        return -1;
    }

    public CookieAccess GetCookieAccess(int cookieId)
    {
        return g_ClientPrefs.Find(p => p.Id == cookieId)!.Access;
    }

    public void ChangePlayerClientPrefNewValue(string steamId, int cookieId, string value)
    {
        g_PlayerClientPrefs[steamId].First(p => p.Id == cookieId).NewValue = value;
    }

    public void AddPlayerClientPrefNewValue(string steamId, int cookieId, string value)
    {
        if (!g_PlayerClientPrefs.ContainsKey(steamId))
        {
            g_PlayerClientPrefs.Add(steamId, new List<PlayerClientPrefs>());
        }
    
        g_PlayerClientPrefs[steamId].Add(new PlayerClientPrefs()
        {
            Id = cookieId,
            NewValue = value
        });
    }

    public int ClientPrefCount()
    {
        return g_ClientPrefs.Count();
    }

    public bool ClientPrefExists(string name)
    {
        return g_ClientPrefs.Any(p => p.Name == name || p.Id == g_iLatestClientprefID);
    }

    public int GetClientPrefByName(string name)
    {
        return g_ClientPrefs.First(p => p.Name == name).Id;
    }

    public void LogWarning(string message)
    {
        Logger.LogWarning($"{LogPrefix} {message}");
    }
}

public class ClientprefsApi : IClientprefsApi
{
    public Clientprefs plugin;
    
    public ClientprefsApi(Clientprefs plugin)
    {
        this.plugin = plugin;
    }

    private List<IClientprefsApi.PlayerCookiesCached> _onPlayerCookiesCachedHooks = new();
    private List<IClientprefsApi.DatabaseLoaded> _onDatabaseReadyHooks = new();

    public int RegPlayerCookie(string name, string description, CookieAccess access = CookieAccess.CookieAccess_Public)
    {
        if(plugin.ClientPrefExists(name))
        {
            return plugin.GetClientPrefByName(name);
        }
        
        if(name.Length > IClientprefsApi.COOKIE_MAX_NAME_LENGTH)
        {
            plugin.LogWarning($"RegPlayerCookie was used with name being too long");
        }
        if(description.Length > IClientprefsApi.COOKIE_MAX_DESCRIPTION_LENGTH)
        {
            plugin.LogWarning($"RegPlayerCookie was used with to description being too long");
        }
        
        if(plugin.CreatePlayerCookie(name, description, access))
        {
            plugin.AddClientprefCommands(name, description, access);
            return plugin.g_iLatestClientprefID++;
        }
        else
        {
            return -1;
        }
    }

    public int FindPlayerCookie(string name)
    {
        if(plugin.ClientPrefExists(name))
        {
            return plugin.GetClientPrefByName(name);
        }
        return -1;
    }

    public string GetPlayerCookie(CCSPlayerController player, int cookieId)
    {
        if(!player.IsValidPlayer())
        {
            throw new Exception($"GetPlayerCookie failed due to player being invalid");
        }

        if(!plugin.g_PlayerSettings.TryGetValue(player.SteamID.ToString(), out var pref) || !pref.Loaded)
        {
            throw new Exception($"GetPlayerCookie failed due to player not being loaded");
        }
        
        var steamId = player.SteamID.ToString();

        if(!plugin.g_PlayerClientPrefs.TryGetValue(steamId, out var _))
        {
            throw new Exception($"GetPlayerCookie failed due to it being called before cookies were loaded for player {steamId}");
        }

        if(plugin.g_PlayerClientPrefs[steamId].Any(p => p.Id == cookieId))
        {
            return plugin.g_PlayerClientPrefs[steamId].First(p => p.Id == cookieId).NewValue;
        }
        return "";
    }
    
    public void SetPlayerCookie(CCSPlayerController player, int cookieId, string value)
    {
        if(!player.IsValidPlayer())
        {
            throw new Exception($"SetPlayerCookie failed due to player being invalid");
        }

        if(value.Length > IClientprefsApi.COOKIE_MAX_VALUE_LENGTH)
        {
            plugin.LogWarning($"RegPlayerCookie was used with value being too long");
        }

        if(!plugin.g_PlayerSettings.TryGetValue(player.SteamID.ToString(), out var pref) || !pref.Loaded)
        {
            throw new Exception($"GetPlayerCookie failed due to player not being loaded");
        }
        
        var steamId = player.SteamID.ToString();

        if(!plugin.g_PlayerClientPrefs.TryGetValue(steamId, out var _))
        {
            throw new Exception($"SetPlayerCookie failed due to it being called before cookies were loaded for player {steamId}");
        }

        if(plugin.g_PlayerClientPrefs[steamId].Any(p => p.Id == cookieId))
        {
            plugin.ChangePlayerClientPrefNewValue(steamId, cookieId, value);
        }
        else
        {
            plugin.AddPlayerClientPrefNewValue(steamId, cookieId, value);
        }
    }

    public void SetPlayerCookie(string steamId, int cookieId, string value)
    {
        if(!plugin.g_PlayerClientPrefs.TryGetValue(steamId, out var _))
        {
            plugin.AddPlayerClientPrefNewValue(steamId, cookieId, value);
            return;
        }

        if(plugin.g_PlayerClientPrefs[steamId].Any(p => p.Id == cookieId))
        {
            plugin.ChangePlayerClientPrefNewValue(steamId, cookieId, value);
        }
        else
        {
            plugin.AddPlayerClientPrefNewValue(steamId, cookieId, value);
        }
    }

    public bool ArePlayerCookiesCached(CCSPlayerController player)
    {
        if(!player.IsValidPlayer())
        {
            throw new Exception($"ArePlayerCookiesCached failed due to player being invalid");
        }
        
        var steamId = player.SteamID.ToString();

        return !plugin.g_PlayerSettings.TryGetValue(steamId, out var pref) || !pref.Loaded;
    }

    public void HookPlayerCache(IClientprefsApi.PlayerCookiesCached hook)
    {
        _onPlayerCookiesCachedHooks.Add(hook);
    }

    public void UnhookPlayerCache(IClientprefsApi.PlayerCookiesCached hook)
    {
        _onPlayerCookiesCachedHooks.Remove(hook);
    }

    public void HookRegisterCookie(IClientprefsApi.DatabaseLoaded hook)
    {
        _onDatabaseReadyHooks.Add(hook);
    }

    public void UnhookRegisterCookie(IClientprefsApi.DatabaseLoaded hook)
    {
        _onDatabaseReadyHooks.Remove(hook);
    }

    public void ClearAPIHooks()
    {
        _onPlayerCookiesCachedHooks.Clear();
        _onDatabaseReadyHooks.Clear();
    }
    
    public void OnPlayerCookiesCached(CCSPlayerController player)
    {
        foreach (var hook in _onPlayerCookiesCachedHooks)
        {
            if(!player.IsValidPlayer()) continue;
            
            hook.Invoke(player);
        }
    }

    public void OnDatabaseLoaded()
    {
        foreach (var hook in _onDatabaseReadyHooks)
        {
            hook.Invoke();
        }
    }

    public void SetCookiePrefabMenu(int cookieId, CookieMenu type, string display, Action<CCSPlayerController, CookieMenuAction, string> cookieMenuHandler)
    {
        throw new NotImplementedException();
    }

    public void SetCookieMenuItem(Action<CCSPlayerController, CookieMenuAction, string> cookieMenuHandler, string display)
    {
        throw new NotImplementedException();
    }

    public void ShowCookieMenu(CCSPlayerController player)
    {
        throw new NotImplementedException();
    }
}