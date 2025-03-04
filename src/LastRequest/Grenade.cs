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


public class LRGrenade : LRBase
{
    public LRGrenade(LastRequest manager,LastRequest.LRType type,int LRSlot, int playerSlot, String choice) : base(manager,type,LRSlot,playerSlot,choice)
    {

    }

    public override void InitPlayer(CCSPlayerController player)
    {    
        weaponRestrict = "hegrenade";

        if(player.IsLegalAlive())
        {
            player.SetHealth(150);

            player.GiveWeapon("hegrenade");

            switch(choice)
            {
                case "Normal": 
                {
                    break;
                }
                
                case "Gravitatie mica":
                {
                    player.SetGravity(0.6f);
                    break;
                }
            }
        }
    }

    public override void PairActivate()
    {
        DelayFailSafe(35.0f);
    }


    public override void PlayerHurt(int damage, int health, int hitgroup)
    {
        CCSPlayerController? player = Utilities.GetPlayerFromSlot(playerSlot);
    
        // instantly drop the player if the failsafe is active
        if(player.IsLegalAlive() && failSafe)
        {
            player.Announce(LastRequest.LR_PREFIX,"Boom!");
            player.Slay();
        }
    }

    public override void GrenadeThrown()
    {
        CCSPlayerController? player = Utilities.GetPlayerFromSlot(playerSlot);
        player.StripWeapons(true);
        GiveLRNadeDelay(1.4f,"weapon_hegrenade");
    }
}