
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
using CounterStrikeSharp.API.Modules.Admin;
using Menu;
using Menu.Enums;

public enum SDState
{
    INACTIVE,
    STARTED,
    ACTIVE
};

// TODO: this will be done after lr and warden
// are in a decent state as its just an extra, and should 
// not take too long to port from css
public partial class SpecialDay
{

    public void EndSD(bool forced = false)
    {
        if(activeSD != null)
        {
            JailPlugin.EndEvent();
            activeSD.EndCommon();
            activeSD = null;

            countdown.Kill();

            // restore all players if from a cancel
            if(forced)
            {
                foreach(CCSPlayerController player in Lib.GetAliveCt())
                {
                    player.GiveWeapon("m4a1");
                    player.GiveWeapon("deagle");
                }

                Chat.Announce(SPECIALDAY_PREFIX,"Ziua speciala anulata!");
            }  

            teamSave.Restore();
        }     
    }

    public void SetupSD(CCSPlayerController? invoke, string option, int a)
    {
        if (a != 1)
        {
            if (!invoke.IsLegal())
            {
                return;
            }

            if (activeSD != null)
            {
                invoke.Announce(SPECIALDAY_PREFIX, "Nu poti face doua zile speciale in acelasi timp!");
                return;
            }

            // invoked as warden
            // reset the round counter so they can't do it again
            if (wsdCommand)
            {
                wsdRound = 0;
            }
        }


        String name = option;

        switch(name)
        {
            case "Zeus Day":
                {
                    activeSD = new SDZeusDay();
                    type = SDType.ZEUSDAY;
                    break;
                }
            case "Godmode Day":
                {
                    activeSD = new SDGodmode();
                    type = SDType.GODMODE;
                    break;
                }
            case "Friendly fire Day":
            {
                activeSD = new SDFriendlyFire();
                type = SDType.FRIENDLY_FIRE;
                break;
            }

            case "Juggernaut Day":
            {
                activeSD = new SDJuggernaut();
                type = SDType.JUGGERNAUT;
                break;             
            }

            case "Tank Day":
            {
                activeSD = new SDTank();
                type = SDType.TANK;
                break;                          
            }

            case "Scout knife Day":
            {
                activeSD = new SDScoutKnife();
                type = SDType.SCOUT_KNIFE;
                break;
            }

            case "Headshot only Day":
            {
                activeSD = new SDHeadshotOnly();
                type = SDType.HEADSHOT_ONLY;
                break;             
            }

            case "Knife War Day":
            {
                activeSD = new SDKnifeWarday();
                type = SDType.KNIFE_WARDAY;
                break;             
            }

            case "Hide and seek Day":
            {
                activeSD = new SDHideAndSeek();
                type = SDType.HIDE_AND_SEEK;
                break;               
            }

            case "Dodgeball Day":
            {
                activeSD = new SDDodgeball();
                type = SDType.DODGEBALL;
                break;             
            }

            case "Spectre Day":
            {
                activeSD = new SDSpectre();
                type = SDType.SPECTRE;
                break;                            
            }

            case "Grenade Day":
            {
                activeSD = new SDGrenade();
                type = SDType.GRENADE;
                break;             
            }

            case "Gun game Day":
            {
                activeSD = new SDGunGame();
                type = SDType.GUN_GAME;
                break;                
            }

            case "Zombie Day":
            {
                activeSD = new SDZombie();
                type = SDType.ZOMBIE;
                break;                
            }
        }

        // 1up dead players
        Lib.RespawnPlayers();

        // call the intiail sd setup
        if(activeSD != null)
        {
            teamSave.Save();

            JailPlugin.StartEvent();

            activeSD.delay = delay;
            activeSD.SetupCommon();

            // start the countdown for enable
            countdown.Start($"{name} starts in",delay,0,null,StartSD);
        }
    }

    public void StartSD(int unused)
    {
        if(activeSD != null)
        {
            // force ff active
            if(overrideFF)
            {
                Chat.LocalizeAnnounce(SPECIALDAY_PREFIX,"sd.ffd_enable");
                Lib.EnableFriendlyFire();
            }

            activeSD.StartCommon();
        }  
    }

    [RequiresPermissions("@css/generic")]
    public void CancelSDCmd(CCSPlayerController? player,CommandInfo command)
    {
        EndSD(true);
    }

    public void SDCmdInternal(CCSPlayerController? player,CommandInfo command)
    {
        if(!Config.enableSd)
        {
            player.Announce(SPECIALDAY_PREFIX,"Special day is disabled!");
            return;
        }

        if(!player.IsLegal())
        {
            return;
        }

        delay = 15;

        if(Int32.TryParse(command.ArgByIndex(1),out int delayOpt))
        {
            delay = delayOpt;

            if(delayOpt > 200)
            {
                player.LocalizePrefix(SPECIALDAY_PREFIX, "warden.countdown_max_delay");
                return;
            }
        }

        var sdMenu = new ChatMenu("Specialday");
        List<MenuItem> list = new List<MenuItem>();
        // Build the basic LR menu
        for(int s = 0; s < SD_NAME.Length - 1; s++)
        {
            list.Add(new MenuItem(MenuItemType.Button, [new MenuValue($"{SD_NAME[s]}")]));
            //sdMenu.AddMenuOption(SD_NAME[s], SetupSD);
        }
        JailPlugin.Menu.ShowScrollableMenu(player, "Meniu zile speciale", list, (menuButtons, currentMenu, selectedItems) =>
        {
            if(menuButtons == MenuButtons.Exit) { return; }
            else if(menuButtons == MenuButtons.Select)
            {
                SetupSD(player, $"{selectedItems.Values.ElementAt(0)}");
                JailPlugin.Menu.ClearMenus(player);
            }
        }, freezePlayer:true);
        
    }


    [RequiresPermissions("@jail/debug")]
    public void SDRigCmd(CCSPlayerController? player,CommandInfo command)
    {
        if(!player.IsLegal())
        {
            return;
        }

        if(activeSD != null && activeSD.state == SDState.STARTED)
        {
            player.PrintToChat($"Rigged sd boss to {player.PlayerName}");
            activeSD.riggedSlot = player.Slot;
        }
    }   

    [RequiresPermissions("@css/generic")]
    public void SDCmd(CCSPlayerController? player,CommandInfo command)
    {
        overrideFF = false;
        wsdCommand = false;
        SDCmdInternal(player,command);
    }   

    [RequiresPermissions("@css/generic")]
    public void SDFFCmd(CCSPlayerController? player,CommandInfo command)
    {
        overrideFF = true;
        wsdCommand = false;
        SDCmdInternal(player,command);
    }   

    public void WardenSDCmdInternal(CCSPlayerController? player,CommandInfo command)
    {
        if(!JailPlugin.IsWarden(player))
        {
            player.Announce(SPECIALDAY_PREFIX,"Trebuie sa fi Simon pentru a folosi aceasta comanda!");
            return;
        }

        // Not ready yet
        if(wsdRound < Config.wsdRound)
        {
            player.Announce(SPECIALDAY_PREFIX,$"Mai trebuie sa astepti {Config.wsdRound - wsdRound} runde!");
            return;
        }

        // Go!
        wsdCommand = true;
        SDCmdInternal(player,command);
    }

    public void WardenSDCmd(CCSPlayerController? player,CommandInfo command)
    {
        overrideFF = false;

        WardenSDCmdInternal(player,command);
    }   

    public void WardenSDFFCmd(CCSPlayerController? player,CommandInfo command)
    {
        overrideFF = true;

        WardenSDCmdInternal(player,command);
    }   

    public enum SDType
    {
        ZEUSDAY,
        GODMODE,
        FRIENDLY_FIRE,
        JUGGERNAUT,
        TANK,
        SPECTRE,
        DODGEBALL,
        GRENADE,
        SCOUT_KNIFE,
        HIDE_AND_SEEK,
        HEADSHOT_ONLY,
        KNIFE_WARDAY,
        GUN_GAME,
        ZOMBIE,
        NONE
    };

    public static String SPECIALDAY_PREFIX = $"  {ChatColors.Green}[Alpahcs.ro]: {ChatColors.White}";

    static String[] SD_NAME = {
        "Zeus Day",
        "Godmode Day",
        "Friendly fire Day",
        "Juggernaut Day",
        "Tank Day",
        "Spectre Day",
        "Dodgeball Day",
        "Grenade Day",
        "Scout knife Day",
        "Hide and seek Day",
        "Headshot only Day",
        "Knife War Day",
        "Gun Game Day",
        //"Zombie",
        "None"
    };

    int delay = 15;

    public int wsdRound = 0;

    // NOTE: if we cared we would make this per player
    // so we can't get weird conflicts, but its not a big deal
    bool wsdCommand = false;

    SDBase? activeSD = null;

    bool overrideFF = false;

    Countdown<int> countdown = new Countdown<int>();

    SDType type = SDType.NONE;

    public JailConfig Config = new JailConfig();

    TeamSave teamSave = new TeamSave();
};