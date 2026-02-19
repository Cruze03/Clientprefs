using CounterStrikeSharp.API.Core;
using Clientprefs.API;
using Microsoft.Extensions.Logging;

namespace Clientprefs;

public partial class Clientprefs
{
    public void AddClientprefCommands(string name, string description, CookieAccess access)
    {
        Cookies.Add(new Cookie()
        {
            Id = LatestClientprefID,
            Name = name,
            Description = description,
            Access = access
        });
    }

    public int FindPlayerCookie(string name)
    {
        if (ClientPrefExists(name))
        {
            return GetClientPrefByName(name);
        }
        return -1;
    }

    public CookieAccess GetCookieAccess(int cookieId)
    {
        return Cookies.Find(p => p.Id == cookieId)!.Access;
    }

    public void AddPlayerClientPrefNewValue(string steamId, int cookieId, string value)
    {
        if (!PlayerSettings.ContainsKey(steamId))
        {
            PlayerSettings.Add(steamId, new());
        }

        PlayerSettings[steamId].ModifyCookie(cookieId, newValue: value);
    }

    public int ClientPrefCount()
    {
        return Cookies.Count();
    }

    public bool ClientPrefExists(string name)
    {
        return Cookies.Any(p => p.Name == name || p.Id == LatestClientprefID);
    }

    public int GetClientPrefByName(string name)
    {
        return Cookies.First(p => p.Name == name).Id;
    }

    public void LogWarning(string message)
    {
        Logger.LogWarning(message);
    }
}

public class ClientprefsApi : IClientprefsApi
{
    private Clientprefs _plugin;

    public ClientprefsApi(Clientprefs plugin)
    {
        _plugin = plugin;
    }

    public event Action<CCSPlayerController>? OnPlayerCookiesCached;
    public event Action? OnDatabaseLoaded;

    // The event 'ClientprefsApi.OnPlayerCookiesCached' can only appear on the left hand side of +=
    // or -= (except when used from within the type 'ClientprefsApi')
    public void CallOnDatabaseLoaded()
    {
        OnDatabaseLoaded?.Invoke();
    }
    public void CallOnPlayerCookiesCached(CCSPlayerController player)
    {
        OnPlayerCookiesCached?.Invoke(player);
    }

    public int RegPlayerCookie(string name, string description, CookieAccess access = CookieAccess.CookieAccess_Public)
    {
        if (_plugin.ClientPrefExists(name))
        {
            return _plugin.GetClientPrefByName(name);
        }

        if (name.Length > IClientprefsApi.COOKIE_MAX_NAME_LENGTH)
        {
            _plugin.LogWarning($"RegPlayerCookie was used with name being too long");
        }
        if (description.Length > IClientprefsApi.COOKIE_MAX_DESCRIPTION_LENGTH)
        {
            _plugin.LogWarning($"RegPlayerCookie was used with description being too long");
        }

        if (_plugin.CreatePlayerCookie(name, description, access))
        {
            _plugin.AddClientprefCommands(name, description, access);
            return _plugin.LatestClientprefID++;
        }
        else
        {
            return -1;
        }
    }

    public async Task<int> RegPlayerCookieAsync(string name, string description, CookieAccess access = CookieAccess.CookieAccess_Public)
    {
        if (_plugin.ClientPrefExists(name))
        {
            return _plugin.GetClientPrefByName(name);
        }

        if (name.Length > IClientprefsApi.COOKIE_MAX_NAME_LENGTH)
        {
            _plugin.LogWarning($"RegPlayerCookieAsync was used with name being too long");
        }
        if (description.Length > IClientprefsApi.COOKIE_MAX_DESCRIPTION_LENGTH)
        {
            _plugin.LogWarning($"RegPlayerCookieAsync was used with description being too long");
        }

        bool success = await _plugin.CreatePlayerCookieNew(name, description, access);

        if (success)
        {
            _plugin.AddClientprefCommands(name, description, access);
            return _plugin.LatestClientprefID++;
        }
        else
        {
            return -1;
        }
    }

    public int FindPlayerCookie(string name)
    {
        if (_plugin.ClientPrefExists(name))
        {
            return _plugin.GetClientPrefByName(name);
        }
        return -1;
    }

    public string GetPlayerCookie(CCSPlayerController player, int cookieId)
    {
        if (!player.IsValidPlayer())
        {
            throw new Exception($"GetPlayerCookie failed due to player being invalid");
        }

        if (!_plugin.PlayerSettings.TryGetValue(player.SteamID.ToString(), out var pref) || !pref.Loaded)
        {
            throw new Exception($"GetPlayerCookie failed due to player not being loaded yet. Use OnPlayerCookiesCached");
        }

        var steamId = player.SteamID.ToString();

        if (!_plugin.PlayerSettings.TryGetValue(steamId, out var _))
        {
            throw new Exception($"GetPlayerCookie failed due to it being called before cookies were loaded for player {steamId}");
        }

        if (_plugin.PlayerSettings[steamId].TryGetCookie(cookieId, out var cookie))
        {
            return cookie.NewValue;
        }
        return "";
    }

    public void SetPlayerCookie(CCSPlayerController player, int cookieId, string value)
    {
        if (!player.IsValidPlayer())
        {
            throw new Exception($"SetPlayerCookie failed due to player being invalid");
        }

        if (value.Length > IClientprefsApi.COOKIE_MAX_VALUE_LENGTH)
        {
            _plugin.LogWarning($"RegPlayerCookie was used with value being too long");
        }

        if (!_plugin.PlayerSettings.TryGetValue(player.SteamID.ToString(), out var pref) || !pref.Loaded)
        {
            throw new Exception($"SetPlayerCookie failed due to player not being loaded");
        }

        var steamId = player.SteamID.ToString();

        if (!_plugin.PlayerSettings.TryGetValue(steamId, out var _))
        {
            throw new Exception($"SetPlayerCookie failed due to it being called before cookies were loaded for player {steamId}");
        }

        _plugin.AddPlayerClientPrefNewValue(steamId, cookieId, value);
    }

    public void SetPlayerCookie(string steamId, int cookieId, string value)
    {
        if (!_plugin.PlayerSettings.TryGetValue(steamId, out var _))
        {
            _plugin.AddPlayerClientPrefNewValue(steamId, cookieId, value);
            return;
        }

        _plugin.AddPlayerClientPrefNewValue(steamId, cookieId, value);
    }

    public bool ArePlayerCookiesCached(CCSPlayerController player)
    {
        if (!player.IsValidPlayer())
        {
            throw new Exception($"ArePlayerCookiesCached failed due to player being invalid");
        }

        var steamId = player.SteamID.ToString();

        return !_plugin.PlayerSettings.TryGetValue(steamId, out var pref) || !pref.Loaded;
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