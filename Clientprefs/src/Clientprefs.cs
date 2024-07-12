using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;

namespace Clientprefs;

[MinimumApiVersion(215)]
public class Clientprefs : BasePlugin
{
    public override string ModuleName => "Clientprefs";
    public override string ModuleDescription => "Clientprefs plugin for CounterStrikeSharp";
    public override string ModuleAuthor => "Cruze";
    public override string ModuleVersion => "1.0.0";

    public override void Load(bool hotReload)
    {
        base.Load(hotReload);
        RegisterListener<Listeners.OnMapStart>(OnMapStart);
    }

    public void OnMapStart(string map)
    {
        
    }
}