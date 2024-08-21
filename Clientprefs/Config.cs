using CounterStrikeSharp.API.Core;
using System.Text.Json.Serialization;

namespace Clientprefs;
public class ClientprefsConfig : BasePluginConfig
{
    [JsonPropertyName("TableName")]
    public string TableName { get; set; } = "css_cookies";
    [JsonPropertyName("TableNamePlayerData")]
    public string TableNamePlayerData { get; set; } = "css_cookies_playerdata";
    [JsonPropertyName("DatabaseType")]
    public string DatabaseType { get; set; } = "sqlite";
    [JsonPropertyName("DatabaseHost")]
    public string DatabaseHost { get; set; } = "";
    [JsonPropertyName("DatabaseName")]
    public string DatabaseName { get; set; } = "";
    [JsonPropertyName("DatabaseUsername")]
    public string DatabaseUsername { get; set; } = "";
    [JsonPropertyName("DatabasePassword")]
    public string DatabasePassword { get; set; } = "";
    [JsonPropertyName("DatabasePort")]
    public int DatabasePort { get; set; } = 3306;
    [JsonPropertyName("DatabaseSslmode")]
	public string DatabaseSslmode { get; set; } = "";
    [JsonPropertyName("Debug")]
    public bool Debug { get; set; } = false;
}