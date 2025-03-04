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
using CounterStrikeSharp.API.Modules.Admin;
using Menu;
using Menu.Enums;
using CSTimer = CounterStrikeSharp.API.Modules.Timers;
using static LastRequest;
using System.Diagnostics.Eventing.Reader;
using CounterStrikeSharp.API.Modules.Timers;
using System.Drawing;



public partial class LastRequest
{
    public LastRequest()
    {
        for(int c = 0; c < lrChoice.Length; c++)
        {
            lrChoice[c] = new LRChoice();
        }

        for(int lr = 0; lr < activeLR.Length; lr++)
        {
            activeLR[lr] = null;
        }
    }

    public void LRConfigReload()
    {
        CreateLRSlots(Config.lrCount);
    }

    void CreateLRSlots(uint slots)
    {
        activeLR = new LRBase[slots];

        for(int lr = 0; lr < activeLR.Length; lr++)
        {
            activeLR[lr] = null;
        }
    }

    void InitPlayerCommon(CCSPlayerController? player, String lrName, String option)
    {
        if(!player.IsLegalAlive())
        {
            return;
        }

        // strip weapons restore hp
        player.SetHealth(100);
        player.SetArmour(100);
        player.StripWeapons(true);
        player.GiveArmour();

        if(option == "")
        {
            player.Announce(LR_PREFIX,$"{lrName} is starting\n");
        }

        else
        {
            player.Announce(LR_PREFIX,$"{lrName} ({option}) is starting\n");
        }
    }

    bool LRExists(LRBase lr)
    {
        for(int l = 0; l < activeLR.Count(); l++)
        {
            if(activeLR[l] == lr)
            {
                return true;
            }
        }

        return false;
    }

    // called back by the lr countdown function
    public void ActivateLR(LRBase lr)
    {
        if(LRExists(lr))
        {
            // call the final LR init function and mark it as truly active
            lr.Activate();
            lr.PairActivate();
        }
    }
    public void DrawBeaconOnPlayer(CCSPlayerController? player)
    {
        if (player == null || !player.IsValid || player.Pawn.Value?.LifeState != (byte)LifeState_t.LIFE_ALIVE) return;

        Vector mid = new Vector(player?.PlayerPawn.Value?.AbsOrigin?.X, player?.PlayerPawn.Value?.AbsOrigin?.Y, player?.PlayerPawn.Value?.AbsOrigin?.Z);

        int lines = 20;
        int[] ent = new int[lines];
        CBeam[] beam_ent = new CBeam[lines];

        // draw piecewise approx by stepping angle
        // and joining points with a dot to dot
        float step = (float)(2.0f * Math.PI) / (float)lines;
        float radius = 20.0f;

        float angle_old = 0.0f;
        float angle_cur = step;

        float BeaconTimerSecond = 0.0f;


        for (int i = 0; i < lines; i++) // Drawing Beacon Circle
        {
            Vector start = angle_on_circle(angle_old, radius, mid);
            Vector end = angle_on_circle(angle_cur, radius, mid);

            if (player.TeamNum == 2)
            {
                var result = DrawLaserBetween(start, end, Color.Red, 1.0f, 2.0f);
                ent[i] = result.Item1;
                beam_ent[i] = result.Item2;
            }
            if (player.TeamNum == 3)
            {
                var result = DrawLaserBetween(start, end, Color.Blue, 1.0f, 2.0f);
                ent[i] = result.Item1;
                beam_ent[i] = result.Item2;
            }

            angle_old = angle_cur;
            angle_cur += step;
        }

        JailPlugin.globalCtx.AddTimer(0.1f, () =>
        {
            if (BeaconTimerSecond >= 0.9f)
            {
                return;
            }
            for (int i = 0; i < lines; i++) // Moving Beacon Circle
            {
                Vector start = angle_on_circle(angle_old, radius, mid);
                Vector end = angle_on_circle(angle_cur, radius, mid);

                TeleportLaser(beam_ent[i], start, end);

                angle_old = angle_cur;
                angle_cur += step;
            }
            radius += 10;
            BeaconTimerSecond += 0.1f;
        }, TimerFlags.REPEAT);
        PlaySoundOnPlayer(player, "sounds/tools/sfm/beep.vsnd_c");
        return;
    }
    private void PlaySoundOnPlayer(CCSPlayerController? player, String sound)
    {
        if (player == null || !player.IsValid) return;
        player.ExecuteClientCommand($"play {sound}");
    }
    private static readonly Vector VectorZero = new Vector(0, 0, 0);
    private static readonly QAngle RotationZero = new QAngle(0, 0, 0);
    public (int, CBeam) DrawLaserBetween(Vector startPos, Vector endPos, Color color, float life, float width)
    {
        if (startPos == null || endPos == null)
            return (-1, null);

        CBeam beam = Utilities.CreateEntityByName<CBeam>("beam");

        if (beam == null)
        {
            Server.PrintToChatAll($"Failed to create beam...");
            return (-1, null);
        }

        beam.Render = color;
        beam.Width = width;

        beam.Teleport(startPos, RotationZero, VectorZero);
        beam.EndPos.X = endPos.X;
        beam.EndPos.Y = endPos.Y;
        beam.EndPos.Z = endPos.Z;
        beam.DispatchSpawn();

        JailPlugin.globalCtx.AddTimer(life, () => { if (beam != null && beam.IsValid) beam.Remove(); }); // destroy beam after specific time

        return ((int)beam.Index, beam);
    }
    public void TeleportLaser(CBeam? laser, Vector start, Vector end)
    {
        if (laser == null || !laser.IsValid) return;
        // set pos
        laser.Teleport(start, RotationZero, VectorZero);
        // end pos
        // NOTE: we cant just move the whole vec
        laser.EndPos.X = end.X;
        laser.EndPos.Y = end.Y;
        laser.EndPos.Z = end.Z;
        Utilities.SetStateChanged(laser, "CBeam", "m_vecEndPos");
    }
    private float CalculateDistance(Vector point1, Vector point2)
    {
        float dx = point2.X - point1.X;
        float dy = point2.Y - point1.Y;
        float dz = point2.Z - point1.Z;

        return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }
    private Vector angle_on_circle(float angle, float radius, Vector mid)
    {
        // {r * cos(x),r * sin(x)} + mid
        // NOTE: we offset Z so it doesn't clip into the ground
        return new Vector((float)(mid.X + (radius * Math.Cos(angle))), (float)(mid.Y + (radius * Math.Sin(angle))), mid.Z + 6.0f);
    }
    async void InitLR(LRChoice choice)
    {
        // Okay type, choice, partner selected
        // now we have all the info we need to setup the lr

        CCSPlayerController? tPlayer = Utilities.GetPlayerFromSlot(choice.tSlot);
        CCSPlayerController? ctPlayer = Utilities.GetPlayerFromSlot(choice.ctSlot);
        

        // check we still actually have all the players
        // our handlers only check once we have actually triggered the LR
        if(!tPlayer.IsLegalAlive() || !ctPlayer.IsLegalAlive())
        {
            Server.PrintToChatAll($"{LR_PREFIX}Disconnection during lr setup");
            return;
        }

        // Double check we can still do an LR before we trigger!
        if(!choice.bypass)
        {
            // check we still actually have all the players
            // our handlers only check once we have actually triggered the LR
            if(InLR(tPlayer) || InLR(ctPlayer))
            {
                tPlayer.Announce(LR_PREFIX,"Un jucator este deja in LR.");
                return;
            }

            if(!CanStartLR(tPlayer) || !IsValidCT(ctPlayer))
            {
                Server.PrintToChatAll("canstartlr");
                return;
            }
        }

        int slot = -1;

        // find a slot to install the lr
        for(int lr = 0; lr < activeLR.Length; lr++)
        {
            if(activeLR[lr] == null)
            {
                slot = lr;
                break;
            }
        }

        if(slot == -1)
        {
            Chat.Announce(LR_PREFIX,"error Could not find empty lr slot");  
            return;
        }

        // create the LR
        LRBase? tLR = null;
        LRBase? ctLR = null;
        Server.PrintToChatAll($"1. {choice.option}");
        Server.PrintToChatAll($"2. {choice.type}");
        switch(choice.type)
        {
            case LRType.KNIFE:
            {
                tLR = new LRKnife(this,choice.type,slot,choice.tSlot,choice.option);
                ctLR = new LRKnife(this,choice.type,slot,choice.ctSlot,choice.option);
                break;
            }

            case LRType.GUN_TOSS:
            {
                tLR = new LRGunToss(this,choice.type,slot,choice.tSlot,choice.option);
                ctLR = new LRGunToss(this,choice.type,slot,choice.ctSlot,choice.option);
                break;
            }

            case LRType.DODGEBALL:
            {
                tLR = new LRDodgeball(this,choice.type,slot,choice.tSlot,choice.option);
                ctLR = new LRDodgeball(this,choice.type,slot,choice.ctSlot,choice.option);
                break;              
            }

            case LRType.GRENADE:
            {
                tLR = new LRGrenade(this,choice.type,slot,choice.tSlot,choice.option);
                ctLR = new LRGrenade(this,choice.type,slot,choice.ctSlot,choice.option);
                break;              
            }

            case LRType.WAR:
            {
                tLR = new LRWar(this,choice.type,slot,choice.tSlot,choice.option);
                ctLR = new LRWar(this,choice.type,slot,choice.ctSlot,choice.option);
                break;              
            }
    
            case LRType.SCOUT_KNIFE:
            {
                tLR = new LRScoutKnife(this,choice.type,slot,choice.tSlot,choice.option);
                ctLR = new LRScoutKnife(this,choice.type,slot,choice.ctSlot,choice.option);
                break;              
            }

            case LRType.SHOT_FOR_SHOT:
            {
                tLR = new LRShotForShot(this,choice.type,slot,choice.tSlot,choice.option);
                ctLR = new LRShotForShot(this,choice.type,slot,choice.ctSlot,choice.option);
                break;              
            }

            case LRType.MAG_FOR_MAG:
            {
                tLR = new LRShotForShot(this,choice.type,slot,choice.tSlot,choice.option,true);
                ctLR = new LRShotForShot(this,choice.type,slot,choice.ctSlot,choice.option,true);
                break;              
            }

            case LRType.HEADSHOT_ONLY:
            {
                tLR = new LRHeadshotOnly(this,choice.type,slot,choice.tSlot,choice.option);
                ctLR = new LRHeadshotOnly(this,choice.type,slot,choice.ctSlot,choice.option);
                break;              
            }

            case LRType.RUSSIAN_ROULETTE:
            {
                tLR = new LRRussianRoulette(this,choice.type,slot,choice.tSlot,choice.option);
                ctLR = new LRRussianRoulette(this,choice.type,slot,choice.ctSlot,choice.option);
                break;              
            }

            case LRType.NO_SCOPE:
            {
                tLR = new LRNoScope(this,choice.type,slot,choice.tSlot,choice.option);
                ctLR = new LRNoScope(this,choice.type,slot,choice.ctSlot,choice.option);
                break;                 
            }

            case LRType.NONE:
            {
                return;
            }
        }


        // This should not happen
        if(slot == -1 || tLR == null || ctLR == null)
        {
            Chat.Announce(LR_PREFIX,$"Internal LR error in init_lr {slot} {tLR} {ctLR}");
            return;
        }

        // do common player setup
        InitPlayerCommon(tPlayer,tLR.lrName,choice.option);
        InitPlayerCommon(ctPlayer,ctLR.lrName,choice.option); 
 
        // bind lr pair
        tLR.partner = ctLR;
        ctLR.partner = tLR;

        activeLR[slot] = tLR;

        // begin counting down the lr
        tLR.CountdownStart();
        Dictionary<CCSPlayerController, CounterStrikeSharp.API.Modules.Timers.Timer?> PlayerBeaconTimer = new Dictionary<CCSPlayerController, CounterStrikeSharp.API.Modules.Timers.Timer>();
        if (PlayerBeaconTimer == null) PlayerBeaconTimer = new Dictionary<CCSPlayerController, CounterStrikeSharp.API.Modules.Timers.Timer?>(); // Initialize if null
        if (!PlayerBeaconTimer?.ContainsKey(ctPlayer) == true) PlayerBeaconTimer[ctPlayer] = null; // Add the key if not present
        if (PlayerBeaconTimer?[ctPlayer] != null) PlayerBeaconTimer?[ctPlayer]?.Kill(); // Kill Timer if running
                                                                                    // Start Beacon
        PlayerBeaconTimer[ctPlayer] = JailPlugin.globalCtx.AddTimer(1.0f, () =>
        {
            if (ctPlayer.IsLegalAlive() == false || tPlayer.IsLegalAlive() == false)
            {
                
                PlayerBeaconTimer?[ctPlayer].Kill(); // Kill Timer if player die or leave
            }
            else DrawBeaconOnPlayer(ctPlayer);
        }, TimerFlags.REPEAT);
        if (!PlayerBeaconTimer?.ContainsKey(tPlayer) == true) PlayerBeaconTimer[tPlayer] = null; // Add the key if not present
        if (PlayerBeaconTimer?[tPlayer] != null) PlayerBeaconTimer?[tPlayer]?.Kill(); // Kill Timer if running
                                                                                        // Start Beacon
        PlayerBeaconTimer[tPlayer] = JailPlugin.globalCtx.AddTimer(1.0f, () =>
        {
            if (ctPlayer.IsLegalAlive() == false || tPlayer.IsLegalAlive() == false)
            {
                Server.ExecuteCommand("sv_autobunnyhopping true");
                PlayerBeaconTimer?[tPlayer].Kill(); // Kill Timer if player die or leave
            }
            else DrawBeaconOnPlayer(tPlayer);
        }, TimerFlags.REPEAT);
    }
    

    public void PurgeLR()
    {
        for(int l = 0; l < activeLR.Length; l++)
        {
            EndLR(l);
        }

        rebelType = RebelType.NONE;
        lrReadyPrintFired = false;
    }

    bool IsPair(CCSPlayerController? v1, CCSPlayerController? v2)
    {
        LRBase? lr1 = FindLR(v1);
        LRBase? lr2 = FindLR(v2);

        // if either aint in lr they aernt a pair
        if(lr1 == null || lr2 == null)
        {
            return false;
        }

        // same slot must be a pair!
        return lr1.slot == lr2.slot;
    }



    // end an lr
    public void EndLR(int slot)
    {
        LRBase? lr = activeLR[slot];

        if(lr == null)
        {
            return;
        }

        // cleanup each lr
        lr.Cleanup();

        if(lr.partner != null)
        {
            lr.partner.Cleanup();
        }

        // Remove lookup

        // remove the slot
        activeLR[slot] = null;
    }

    bool IsValid(CCSPlayerController? player)
    {
        if(!player.IsLegal())
        {
            return false;
        }

        if(!player.IsLegalAlive())
        {
            player.LocalizeAnnounce(LR_PREFIX,"lr.alive");
            return false;
        }

        if(InLR(player))
        {
            player.LocalizeAnnounce(LR_PREFIX,"lr.in_lr");
            return false;            
        }

        return true;
    }

    bool IsValidT(CCSPlayerController? player)
    {
        if(!player.IsLegal() || !IsValid(player))
        {
            return false;
        }

        if(!player.IsT())
        {
            player.LocalizeAnnounce(LR_PREFIX,"lr.req_t");
            return false;        
        }

        return true;
    }


    bool IsValidCT(CCSPlayerController? player)
    {
        if(!player.IsLegal() || !IsValid(player))
        {
            return false;
        }

        if(!player.IsCt())
        {
            player.LocalizeAnnounce(LR_PREFIX,"lr.req_ct");
            return false;        
        }

        return true;
    }


    LRBase? FindLR(CCSPlayerController? player)
    {
        // NOTE: dont use anything much from player
        // because the pawn is not their as they may be dced
        if(player == null || !player.IsValid)
        {
            return null;
        }

        int slot = player.Slot;

        // scan each active lr for player and partner
        // a HashTable setup is probably not worthwhile here
        foreach(LRBase? lr in activeLR)
        {
            if(lr == null)
            {
                continue;
            }

            if(lr.playerSlot == slot)
            {
                return lr;
            }

            if(lr.partner != null && lr.partner.playerSlot == slot)
            {
                return lr.partner;
            }
        }

        // could not find
        return null;
    }

    public bool InLR(CCSPlayerController? player)
    {
        return FindLR(player) != null;        
    }


    public void AddLR(List<MenuItem> lritems, bool cond, LRType type)
    {
        if (type.ToString() == "KNIFE")
            //LRitems.
            lritems.Add(new MenuItem(MenuItemType.Button, [new MenuValue($"{type}")]));
        else if (type.ToString() == "DODGEBALL")
        {
            lritems.Add(new MenuItem(MenuItemType.Button, [new MenuValue($"{type}")]));
        }
        else if (type.ToString() == "NO_SCOPE")
        {
            lritems.Add(new MenuItem(MenuItemType.Button, [new MenuValue("NO SCOPE")]));
        }
        else if (type.ToString() == "GRENADE")
        {
            lritems.Add(new MenuItem(MenuItemType.Button, [new MenuValue($"{type}")]));
        }
        
    }

    public void LRCmdInternal(CCSPlayerController? player, bool bypass, CommandInfo command)
    {
        // check player can start lr
        // NOTE: higher level function checks its valid to start an lr
        // so we can do a bypass for debugging
        if (!player.IsLegal() || rebelType != RebelType.NONE || JailPlugin.EventActive())
        {
            return;
        }

        int playerSlot = player.Slot;
        lrChoice[playerSlot].tSlot = playerSlot;
        lrChoice[playerSlot].bypass = bypass;

        var lrMenu = new ChatMenu("LR Menu");
        var lritems = new List<MenuItem>();

        



        lritems.Add(new MenuItem(MenuItemType.Button, [new MenuValue("KNIFE")]));
        lritems.Add(new MenuItem(MenuItemType.Button, [new MenuValue("DODGEBALL")]));
        lritems.Add(new MenuItem(MenuItemType.Button, [new MenuValue("NO SCOPE")]));
        lritems.Add(new MenuItem(MenuItemType.Button, [new MenuValue("GRENADE")]));
        //lritems.Add(new MenuItem(MenuItemType.Button, [new MenuValue("WAR")]));
        lritems.Add(new MenuItem(MenuItemType.Button, [new MenuValue("RUSSIAN ROULETTE")]));
        //AddLR(lritems, Config.lrScoutKnife,LRType.SCOUT_KNIFE);
        lritems.Add(new MenuItem(MenuItemType.Button, [new MenuValue("HEADSHOT ONLY")]));
        lritems.Add(new MenuItem(MenuItemType.Button, [new MenuValue("SHOT FOR SHOT")]));
        lritems.Add(new MenuItem(MenuItemType.Button, [new MenuValue("MAG FOR MAG")]));
        JailPlugin.lrMenu.ShowScrollableMenu(player, "Meniu LR", lritems, (menuButtons, currentMenu, selectedItem) =>
        {
            if (menuButtons == MenuButtons.Exit)
            {
                return;
            }
            if (menuButtons == MenuButtons.Select)
            {


                if (currentMenu.Option == 0)
                {
                    //pune PickedOption dupa
                    var lritems2 = new List<MenuItem>();
                    lritems2.Add(new MenuItem(MenuItemType.Button, [new MenuValue("Normal")]));
                    lritems2.Add(new MenuItem(MenuItemType.Button, [new MenuValue("Gravitatie mica")]));
                    lritems2.Add(new MenuItem(MenuItemType.Button, [new MenuValue("Viteza mare")]));
                    lritems2.Add(new MenuItem(MenuItemType.Button, [new MenuValue("One hit")]));
                    JailPlugin.lrMenu.ShowScrollableMenu(player, "Optiuni KNIFE", lritems2, (menuButtons, currentMenu, selectedItem) =>
                    {
                        if (menuButtons == MenuButtons.Exit)
                        {
                            return;
                        }
                        if (menuButtons == MenuButtons.Select)
                        {

                            LRType choice = LRType.KNIFE;
                            PickedOption(player, $"{selectedItem.Values.ElementAt(0)}", choice);
                            //JailPlugin.lrMenu.ClearMenus(player);
                        }
                    }, isSubmenu: true, freezePlayer: true, disableDeveloper: true);
                }
                else if (currentMenu.Option == 1)
                {
                    var lritems2 = new List<MenuItem>();
                    lritems2.Add(new MenuItem(MenuItemType.Button, [new MenuValue("Normal")]));
                    lritems2.Add(new MenuItem(MenuItemType.Button, [new MenuValue("Gravitatie mica")]));
                    JailPlugin.lrMenu.ShowScrollableMenu(player, "Optiuni DODGEBALL", lritems2, (menuButtons, currentMenu, selectedItem) =>
                    {
                        if (menuButtons == MenuButtons.Exit)
                        {
                            return;
                        }
                        if (menuButtons == MenuButtons.Select)
                        {
                            LRType choice = LRType.DODGEBALL;
                            PickedOption(player, $"{selectedItem.Values.ElementAt(0)}", choice);
                            //JailPlugin.lrMenu.ClearMenus(player);
                        }
                    }, isSubmenu: true, freezePlayer: true, disableDeveloper: true);

                }
                else if (currentMenu.Option == 2)
                {
                    var lritems2 = new List<MenuItem>();
                    lritems2.Add(new MenuItem(MenuItemType.Button, [new MenuValue("AWP")]));
                    lritems2.Add(new MenuItem(MenuItemType.Button, [new MenuValue("SSG08")]));
                    lritems2.Add(new MenuItem(MenuItemType.Button, [new MenuValue("G3SG1")]));
                    lritems2.Add(new MenuItem(MenuItemType.Button, [new MenuValue("SCAR20")]));
                    JailPlugin.lrMenu.ShowScrollableMenu(player, "Optiuni NO SCOPE", lritems2, (menuButtons, currentMenu, selectedItem) =>
                    {
                        if (menuButtons == MenuButtons.Exit)
                        {
                            return;
                        }
                        if (menuButtons == MenuButtons.Select)
                        {
                            LRType choice = LRType.NO_SCOPE;
                            PickedOption(player, $"{selectedItem.Values.ElementAt(0)}", choice);

                        }
                    }, isSubmenu: true, freezePlayer: true, disableDeveloper: true);

                }
                else if (currentMenu.Option == 3)
                {
                    var lritems2 = new List<MenuItem>();
                    lritems2.Add(new MenuItem(MenuItemType.Button, [new MenuValue("Normal")]));
                    lritems2.Add(new MenuItem(MenuItemType.Button, [new MenuValue("Gravitatie mica")]));
                    JailPlugin.lrMenu.ShowScrollableMenu(player, "Optiuni GRENADE", lritems2, (menuButtons, currentMenu, selectedItem) =>
                    {
                        if (menuButtons == MenuButtons.Exit) { return; }
                        if (menuButtons == MenuButtons.Select)
                        {
                            LRType choice = LRType.GRENADE;
                            PickedOption(player, $"{selectedItem.Values.ElementAt(0)}", choice);
                        }
                    }, isSubmenu:true, freezePlayer:true, disableDeveloper:true);
                }
                else if(currentMenu.Option == 4)
                {
                    LRType choice = LRType.RUSSIAN_ROULETTE;
                    PickPartnerInternal(player, "", choice);
                }
                else if(currentMenu.Option == 5)
                {
                    LRType choice = LRType.HEADSHOT_ONLY;
                    PickPartnerInternal(player, "", choice);
                }
                else if(currentMenu.Option == 6)
                {
                    var lritems2 = new List<MenuItem>();
                    lritems2.Add(new MenuItem(MenuItemType.Button, [new MenuValue("DEAGLE")]));
                    lritems2.Add(new MenuItem(MenuItemType.Button, [new MenuValue("GLOCK")]));
                    lritems2.Add(new MenuItem(MenuItemType.Button, [new MenuValue("FIVE SEVEN")]));
                    lritems2.Add(new MenuItem(MenuItemType.Button, [new MenuValue("DUAL ELITE")]));
                    JailPlugin.lrMenu.ShowScrollableMenu(player, "Optiuni SHOT FOR SHOT", lritems2, (menuButtons, currentMenu, selectedItem) =>
                    {
                        if(menuButtons == MenuButtons.Exit) { return; }
                        if(menuButtons == MenuButtons.Select)
                        {
                            LRType choice = LRType.SHOT_FOR_SHOT;
                            PickedOption(player, $"{selectedItem.Values.ElementAt(0)}", choice);
                        }
                    }, isSubmenu:true, freezePlayer:true, disableDeveloper:true);
                }
                else if(currentMenu.Option == 7)
                {
                    var lritems2 = new List<MenuItem>();
                    lritems2.Add(new MenuItem(MenuItemType.Button, [new MenuValue("DEAGLE")]));
                    lritems2.Add(new MenuItem(MenuItemType.Button, [new MenuValue("GLOCK")]));
                    lritems2.Add(new MenuItem(MenuItemType.Button, [new MenuValue("FIVE SEVEN")]));
                    lritems2.Add(new MenuItem(MenuItemType.Button, [new MenuValue("DUAL ELITE")]));
                    JailPlugin.lrMenu.ShowScrollableMenu(player, "Optiuni MAG FOR MAG", lritems2, (menuButtons, currentMenu, selectedItem) =>
                    {
                        if (menuButtons == MenuButtons.Exit) { return; }
                        if (menuButtons == MenuButtons.Select)
                        {
                            LRType choice = LRType.MAG_FOR_MAG;
                            PickedOption(player, $"{selectedItem.Values.ElementAt(0)}", choice);
                        }
                    }, isSubmenu: true, freezePlayer: true, disableDeveloper: true);
                }
            }

        }, freezePlayer: true); 
    
    
    
    }

        // rebel
        /*if(CanRebel())
        {
            lrMenu.AddMenuOption("Knife rebel",StartKnifeRebel);
            lrMenu.AddMenuOption("Rebel",StartRebel);
        
            if(Config.riotEnable)
            {
                lrMenu.AddMenuOption("Riot",StartRiot);
            }
        }

        MenuManager.OpenChatMenu(player, lrMenu);
    }*/

    public void LRCmd(CCSPlayerController? player, CommandInfo command)
    {   
        if(!CanStartLR(player))
        {
            return;
        }

        LRCmdInternal(player,false,command);
    }

    // bypasses validity checks
    [RequiresPermissions("@jail/debug")]
    public void LRDebugCmd(CCSPlayerController? player, CommandInfo command)
    {
        LRCmdInternal(player, true, command);
    }

    public void CancelLRCmd(CCSPlayerController? player, CommandInfo command)
    {
        if(!player.IsLegal())
        {
            return;
        }

        // must be admin or warden
        if(!player.IsGenericAdmin() && !JailPlugin.IsWarden(player))
        {
            player.LocalizePrefix(LR_PREFIX,"lr.cancel_admin");
            return;
        }

        Chat.LocalizeAnnounce(LR_PREFIX,"lr.cancel");
        PurgeLR();
    }

    // TODO: when we can pass extra data in menus this should not be needed
    LRType TypeFromName(String name)
    {
        for(int t = 0; t < LR_NAME.Length; t++)
        {
            if(name == LR_NAME[t])
            {
                return (LRType)t;
            }
        }

        return LRType.NONE;
    }

    static LRChoice? ChoiceFromPlayer(CCSPlayerController? player)
    {
        if(!player.IsLegal())
        {
            return null;
        }

        return lrChoice[player.Slot];
    }

    // our current LR's we use as an event dispatch
    // NOTE: each one of these is the T lr and each holds the other pair
    LRBase?[] activeLR = new LRBase[2];

    public enum LRType
    {
        KNIFE,
        GUN_TOSS,
        DODGEBALL,
        NO_SCOPE,
        GRENADE,
        WAR,
        RUSSIAN_ROULETTE,
        SCOUT_KNIFE,
        HEADSHOT_ONLY,
        SHOT_FOR_SHOT,
        MAG_FOR_MAG,
        NONE,
    };

    public static String[] LR_NAME = {
        "Knife Fight",
        "Gun toss",
        "Dodgeball",
        "No Scope",
        "Grenade",
        "War",
        "Russian roulette",
        "Scout knife",
        "Headshot only",
        "Shot for shot",
        "Mag for mag",
        "None",
    };

    static public readonly int LR_SIZE = 10;

    // Selection for LR
    internal class LRChoice
    {
        public LRType type = LRType.NONE;
        public String option = "";
        public int tSlot = -1;
        public int ctSlot = -1;
        public bool bypass = false;
    } 


    public JailConfig Config = new JailConfig();

    static LRChoice[] lrChoice = new LRChoice[64];
    
    long startTimestamp = 0;

    public static String LR_PREFIX = $" {ChatColors.Green}[Alphacs.Ro]: {ChatColors.White}";
}