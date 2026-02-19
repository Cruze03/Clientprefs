using CounterStrikeSharp.API.Core.Capabilities;
using Clientprefs.API;

namespace Clientprefs;

public partial class Clientprefs
{
    public class Cookie
    {
        public int Id { get; set; } = -1;
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public CookieAccess Access { get; set; } = CookieAccess.CookieAccess_Public;

        public Cookie()
        {
            Id = -1;
            Name = "";
            Description = "";
            Access = CookieAccess.CookieAccess_Public;
        }
    }

    public class ClientCookie
    {
        public int Id { get; set; } = -1;
        public string OldValue { get; set; } = "";
        public string NewValue { get; set; } = "";  // save if oldvalue != newvalue

        public ClientCookie()
        {
            Id = -1;
            OldValue = "";
            NewValue = "";
        }
    }

    public class PlayerSetting
    {
        public bool Loaded { get; set; } = false;
        private Dictionary<int, ClientCookie> _cookieDict { get; set; } = [];

        public PlayerSetting()
        {
            Loaded = false;
            _cookieDict = new();
        }

        public void ModifyCookie(int id, string oldValue = "", string newValue = "")
        {
            if (_cookieDict.TryGetValue(id, out var cookie))
            {
                if (!string.IsNullOrWhiteSpace(oldValue))
                    cookie.OldValue = oldValue;
                if (!string.IsNullOrWhiteSpace(newValue))
                    cookie.NewValue = newValue;
                return;
            }

            var newCookie = new ClientCookie
            {
                Id = id,
                OldValue = oldValue,
                NewValue = newValue
            };

            _cookieDict.Add(id, newCookie);
        }

        public bool TryGetCookie(int id, out ClientCookie clientCookie)
        {
            return _cookieDict.TryGetValue(id, out clientCookie!);
        }

        public int CookieCount
        {
            get => _cookieDict.Count;
        }

        public List<ClientCookie> Cookies
        {
            get => _cookieDict.Values.ToList();
        }
    }

    public ClientprefsConfig Config { get; set; } = new();
    public PluginCapability<IClientprefsApi> PluginCapability = new("Clientprefs");
    public required ClientprefsApi ClientprefsApi { get; set; }
    public List<Cookie> Cookies = new();
    public Dictionary<string, PlayerSetting> PlayerSettings = new();
    public int LatestClientprefID = 0;

    private bool _databaseLoaded = false, _isMySQL = false;
}