﻿// base lr class
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
using CSTimer = CounterStrikeSharp.API.Modules.Timers;


public class SDGodmode : SDBase
{
    public override void Setup()
    {
        LocalizeAnnounce("Godmode day a inceput");
        //LocalizeAnnounce("sd.damage_enable", delay);
    }

    public override void Start()
    {
        LocalizeAnnounce("sd.fight");
    }

    public override void End()
    {
        LocalizeAnnounce("Godmode day s-a terminat");
    }

    public override void SetupPlayer(CCSPlayerController player)
    {
        
    }

    public override void PlayerHurt(CCSPlayerController? player, CCSPlayerController? attacker, int health, int damage, int hitgroup)
    {
        if (!player.IsLegalAlive())
        {
            return;
        }

        // dont allow damage when its not to head
        if (player.Team == CsTeam.CounterTerrorist)
        {
            player.RestoreHP(damage, health);
        }
        
    }
}