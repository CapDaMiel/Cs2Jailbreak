using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API;

public class WardenApi : IWardenService
{
    public WardenApi(JailPlugin plugin) { _plugin = plugin; }
    private readonly JailPlugin _plugin;
    public event Action<CCSPlayerController?, int>? OnLRWin;
    public event Action<CCSPlayerController?, int>? OnLRLost;
    public bool IsWarden(CCSPlayerController? player)
    {
        return JailPlugin.IsWarden(player);
    }

    public void SetWarden(CCSPlayerController player)
    {
        if (player.IsLegalAlive() && player.IsCt())
        {
            JailPlugin.warden.SetWarden(player.Slot);
        }
    }

    public CCSPlayerController? GetWarden()
    {
        return JailPlugin.warden.GetWarden();
    }

    public bool IsDeputy(CCSPlayerController? player)
    {
        return player != null && player.Slot == JailPlugin.warden.deputySlot;
    }
    public CCSPlayerController? GetDeputy()
    {
        return Utilities.GetPlayerFromSlot(JailPlugin.warden.deputySlot);
    }

    public void LRWin(CCSPlayerController? player, LastRequest.LRType type)
    {
        OnLRLost?.Invoke(player, (int)type);
    }
    public void LRLost(CCSPlayerController? player, LastRequest.LRType type)
    {
        OnLRWin?.Invoke(player, (int)type);
    }
}