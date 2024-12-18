using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Entities.Constants;


public class LRGunToss : LRBase
{
    private LastRequest.LRType lrType;
    private float bestDistance = 0.0f;
    private CCSPlayerController? bestPlayer = null;

    public LRGunToss(LastRequest manager, LastRequest.LRType type, int LRSlot, int playerSlot, string choice)
        : base(manager, type, LRSlot, playerSlot, choice)
    {
        lrType = type;
    }

    public override void InitPlayer(CCSPlayerController player)
    {
        weaponRestrict = "deagle";
        player.GiveWeapon("knife");
        player.GiveWeapon("deagle");
        var deagle = player.FindWeapon("weapon_deagle");
        if (deagle != null)
        {
            deagle.SetColour(player.IsT() ? Lib.RED : Lib.CYAN);
            deagle.SetAmmo(0, 0);
        }

        player.PrintToChat("[Gun Toss] Throw your deagle as far as you can!");
    }

    public override bool WeaponEquip(string name)
    {
        return name.Contains("knife") || name.Contains("deagle");
    }

    public override bool WeaponDrop(string name)
    {
        if (name.Contains("deagle"))
        {
            var player = Utilities.GetPlayerFromSlot(playerSlot);
            if (player != null)
            {
                var distance = CalculateTossDistance(player);
                player.PrintToChat($"[Gun Toss] Your toss distance: {distance:F2} units.");

                if (distance > bestDistance)
                {
                    bestDistance = distance;
                    bestPlayer = player;
                    Announce($"[Gun Toss] {player.PlayerName} is now leading with {distance:F2} units!");
                }
            }
        }

        return base.WeaponDrop(name);
    }

    private float CalculateTossDistance(CCSPlayerController player)
    {
        var weapon = player.FindWeapon("weapon_deagle");
        if (weapon == null) return 0.0f;

        var playerOrigin = player.Pawn()?.AbsOrigin;
        var weaponOrigin = weapon.AbsOrigin; // Corecție: utilizăm metoda pentru poziția entității

        if (playerOrigin == null || weaponOrigin == null) return 0.0f;

        return (float)Math.Sqrt(
            Math.Pow(playerOrigin.X - weaponOrigin.X, 2) +
            Math.Pow(playerOrigin.Y - weaponOrigin.Y, 2) +
            Math.Pow(playerOrigin.Z - weaponOrigin.Z, 2)
        );
    }

    public override void Cleanup()
    {
        base.Cleanup();

        if (bestPlayer != null)
        {
            var winner = bestPlayer;
            var loser = winner == Utilities.GetPlayerFromSlot(playerSlot)
                ? Utilities.GetPlayerFromSlot(partner?.playerSlot ?? -1)
                : Utilities.GetPlayerFromSlot(playerSlot);

            if (winner != null && loser != null)
            {
                Announce($"[Gun Toss] {winner.PlayerName} wins with a toss of {bestDistance:F2} units!");
                JailPlugin.WinLR(winner, lrType);
                JailPlugin.LoseLR(loser, lrType);
            }
        }
        else
        {
            Announce("[Gun Toss] No valid tosses were made. No winner this time!");
        }
    }

    private void Announce(string message)
    {
        Chat.Announce("[Gun Toss]", message); // Prefixul poate fi personalizat
    }
}