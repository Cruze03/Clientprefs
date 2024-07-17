using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace Clientprefs;
internal static class CCSPlayerControllerEx
{
    internal static bool IsValidPlayer(this CCSPlayerController? controller)
    {
        return controller != null && controller.IsValid && controller.Handle != IntPtr.Zero && controller.Connected == PlayerConnectedState.PlayerConnected && !controller.IsHLTV && controller.SteamID.ToString().Length == 17;
    }
}

internal static class CHandleCCSPlayerPawnEx
{
    internal static bool IsValidPawn(this CHandle<CCSPlayerPawn>? pawn)
    {
        return pawn != null && pawn.IsValid && pawn != IntPtr.Zero && pawn.Value != null && pawn.Value.IsValid && pawn.Value.WeaponServices != null && pawn.Value.WeaponServices.MyWeapons != null && pawn.Value.ItemServices != null;
    }

    internal static bool IsValidPawnAlive(this CHandle<CCSPlayerPawn>? pawn)
    {
        return IsValidPawn(pawn) && pawn!.Value!.LifeState == (byte)LifeState_t.LIFE_ALIVE && pawn.Value.Health > 0;
    }
}

internal static class CCSPlayerPawnEx
{
    internal static bool IsValidPawn(this CCSPlayerPawn? pawn)
    {
        return pawn != null && pawn.IsValid && pawn.WeaponServices != null && pawn.WeaponServices.MyWeapons != null && pawn.ItemServices != null;
    }

    internal static bool IsValidPawnAlive(this CCSPlayerPawn? pawn)
    {
        return IsValidPawn(pawn) && pawn!.LifeState == (byte)LifeState_t.LIFE_ALIVE && pawn!.Health > 0;
    }
}