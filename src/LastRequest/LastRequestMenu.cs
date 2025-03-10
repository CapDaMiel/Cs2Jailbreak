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

public partial class LastRequest
{
    bool CanStartLR(CCSPlayerController? player)
    {
        if(!player.IsLegal())
        {
            return false;
        }

       
        

        if(!IsValidT(player))
        {
            return false;
        } 

        if(JailPlugin.warden.IsAliveRebel(player) && Config.rebelCantLr)
        {
            player.LocalizePrefix(LR_PREFIX,"lr.rebel_cant_lr");
            return false;
        }

        
        if(Lib.AliveTCount() > activeLR.Length)
        {
            player.LocalizePrefix(LR_PREFIX,"lr.too_many",activeLR.Length);
            return false;
        }

        return true;
    }

    public void FinaliseChoice(CCSPlayerController? player, string option)
    {
        // called from pick_parter -> finalise the type struct
        LRChoice? choice = ChoiceFromPlayer(player);
        
        if(choice == null)
        {
            Server.PrintToChatAll("finalisechoice");
            return;
        }
        
        String name = option;

        choice.ctSlot = Player.SlotFromName(name);
        
        // finally setup the lr
        InitLR(choice);
    }

    public void PickedOption(CCSPlayerController? player, string option, LRType choice)
    {
        PickPartnerInternal(player,option, choice);
    }

    /*public void PickOption(CCSPlayerController? player, ChatMenuOption option)
    {
        // called from lr_type selection
        // save type
        LRChoice? choice = ChoiceFromPlayer(player);

        if(choice == null || !player.IsLegal())
        {
            return;
        }

        choice.type = TypeFromName(option.Text);

        String lrName = LR_NAME[(int)choice.type];

        // now select option
        
        switch(choice.type)
        {
            case LRType.KNIFE:
            {
                var lrMenu = new ChatMenu($"Meniu de optiuni ({lrName})");

                lrMenu.AddMenuOption("Normal", PickedOption);
                lrMenu.AddMenuOption("Gravitatie mica", PickedOption);
                lrMenu.AddMenuOption("Viteza mare", PickedOption);
                lrMenu.AddMenuOption("One hit", PickedOption);
                
                MenuManager.OpenChatMenu(player, lrMenu);                
                break;
            }

            case LRType.DODGEBALL:
            {
                var lrMenu = new ChatMenu($"Meniu de optiuni ({lrName})");

                lrMenu.AddMenuOption("Normal", PickedOption);
                lrMenu.AddMenuOption("Gravitatie mica", PickedOption);

                MenuManager.OpenChatMenu(player, lrMenu);
                break;
            }

            case LRType.WAR:
            {
                var lrMenu = new ChatMenu($"Meniu de optiuni ({lrName})");

                lrMenu.AddMenuOption("XM1014", PickedOption);
                lrMenu.AddMenuOption("M249", PickedOption);
                lrMenu.AddMenuOption("P90", PickedOption);
                lrMenu.AddMenuOption("AK47", PickedOption);

                MenuManager.OpenChatMenu(player, lrMenu);
                break;
            }

            case LRType.NO_SCOPE:
            {
                var lrMenu = new ChatMenu($"Meniu de optiuni ({lrName})");

                lrMenu.AddMenuOption("Awp", PickedOption);
                lrMenu.AddMenuOption("Scout", PickedOption);

                MenuManager.OpenChatMenu(player, lrMenu);
                break;                
            }

            case LRType.GRENADE:
            {
                var lrMenu = new ChatMenu($"Meniu de optiuni ({lrName})");

                lrMenu.AddMenuOption("Normal", PickedOption);
                lrMenu.AddMenuOption("Gravitatie Mica", PickedOption);

                MenuManager.OpenChatMenu(player, lrMenu);
                break;
            }

            case LRType.SHOT_FOR_SHOT:
            case LRType.MAG_FOR_MAG:
            {
                var lrMenu = new ChatMenu($"Meniu de optiuni ({lrName})");

                lrMenu.AddMenuOption("Deagle",PickedOption);
                //lrMenu.AddMenuOption("Usp",PickedOption);
                lrMenu.AddMenuOption("Glock",PickedOption);
                lrMenu.AddMenuOption("Five seven",PickedOption);
                lrMenu.AddMenuOption("Dual Elite",PickedOption);

                MenuManager.OpenChatMenu(player, lrMenu);
                break;
            }

            // no choices just pick a partner
            default:
            {
                PickPartnerInternal(player,"");
                break;
            }
        }
    }*/

    bool LegalLrPartnerT(CCSPlayerController? player)
    {
        return player.IsLegalAliveT() && !InLR(player);
    }

    bool LegalLrPartnerCT(CCSPlayerController? player)
    {
        return player.IsLegalAliveCT() && !InLR(player);
    }

    void PickPartnerInternal(CCSPlayerController? player, String name, LRType name2)
    {
        // called from pick_choice -> pick partner
        LRChoice? choice = ChoiceFromPlayer(player);
        if (choice == null || !player.IsLegal())
        {
            return;
        }
        choice.type = name2;
        choice.option = name;

        String lrName = LR_NAME[(int)choice.type];
        String menuName = $"Meniu partener ({lrName})";
        List<MenuItem> list = new List<MenuItem>();
        list.Add(new MenuItem(MenuItemType.Button, [new MenuValue("Cu Bhop")]));
        list.Add(new MenuItem(MenuItemType.Button, [new MenuValue("Fara Bhop")]));
        JailPlugin.lrMenu.ShowScrollableMenu(player, "Optiuni Bhop", list, (menubuttons, currentMenu, selectedItem) =>
        {
            if(menubuttons == MenuButtons.Exit) { return; }
            else if(menubuttons == MenuButtons.Select)
            {
                if (currentMenu.Option == 1) { Server.ExecuteCommand("sv_autobunnyhopping false"); }
                if (choice.bypass && player.IsCt())
                {
                    Lib.InvokePlayerMenu(player, menuName, FinaliseChoice, LegalLrPartnerT);
                }

                else
                {
                    Lib.InvokePlayerMenu(player, menuName, FinaliseChoice, LegalLrPartnerCT);
                }
            }
        },isSubmenu: true, freezePlayer:true, disableDeveloper:true);
        // Debugging pick t's
           
    }

}