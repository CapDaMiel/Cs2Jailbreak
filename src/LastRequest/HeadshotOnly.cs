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
using HeadshotExplosion;
using System.Numerics;
public class LRHeadshotOnly : LRBase
{
    public LRHeadshotOnly(LastRequest manager,LastRequest.LRType type,int LRSlot, int playerSlot, String choice) : base(manager,type,LRSlot,playerSlot,choice)
    {

    }

    public override void InitPlayer(CCSPlayerController player)
    {    
        weaponRestrict = "deagle";

        player.GiveWeapon("deagle");
    }

    public override void PlayerHurt(int health,int damage, int hitgroup) 
    {
        // dont allow damage when its not to head
        if(hitgroup != Lib.HITGROUP_HEAD)
        {
            CCSPlayerController? player = Utilities.GetPlayerFromSlot(playerSlot);
            player.RestoreHP(damage,health);
            // HeadshotExplosion.Settings.ReferenceEquals(health, 0);
            // HeadshotExplosion.Settings.Damage = 0;
            
        }
        CCSPlayerController? players = Utilities.GetPlayerFromSlot(playerSlot);
        SpawnExplosion(players);
    }

    public void SpawnExplosion(CCSPlayerController player, CCSPlayerController? attacker = null)
    {
        if (player.Pawn.Value == null) return;
        var pawn = player.Pawn.Value!;
        var heProjectile = Utilities.CreateEntityByName<CHEGrenadeProjectile>("hegrenade_projectile");
        if (heProjectile == null || !heProjectile.IsValid) return;
        var node = pawn.CBodyComponent?.SceneNode;
        CounterStrikeSharp.API.Modules.Utils.Vector pos = node!.AbsOrigin;
        pos.Z += 48;

        if (attacker != null)
        {
            var attackerPawn = attacker.PlayerPawn;
            heProjectile.OriginalThrower.Raw = attackerPawn;
        }

        heProjectile.TicksAtZeroVelocity = 100;

        if (attacker != null)
        {
            var attackerPawn = attacker.PlayerPawn.Value;
            heProjectile.TeamNum = attackerPawn!.TeamNum;
        }

        else
        {
            heProjectile.TeamNum = pawn.TeamNum;
        }

        heProjectile.Damage = Settings.Damage;
        heProjectile.DmgRadius = Settings.Radius;
        heProjectile.Teleport(pos, node!.AbsRotation, new CounterStrikeSharp.API.Modules.Utils.Vector(0, 0, -10));
        heProjectile.DispatchSpawn();
        heProjectile.AcceptInput("InitializeSpawnFromWorld", player.PlayerPawn.Value!, player.PlayerPawn.Value!, "");
        heProjectile.DetonateTime = 0;
    }
}

