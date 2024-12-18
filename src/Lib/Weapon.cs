using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Menu;
using System.Drawing;
using System.Diagnostics.CodeAnalysis;
using Menu.Enums;
using Menu;
using System.Linq;
using CounterStrikeSharp.API.Core;
using Menu.Enums;
using Menu;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Commands;
using System.Linq;
using CounterStrikeSharp.API.Modules.Utils;


public static class Weapon
{
    static public bool IsLegal([NotNullWhen(true)] this CBasePlayerWeapon? weapon)
    {
        return weapon != null && weapon.IsValid;
    }


    static public void SetColour(this CBasePlayerWeapon? weapon, Color colour)
    {
        if(weapon.IsLegal())
        {
            weapon.RenderMode = RenderMode_t.kRenderTransColor;
            weapon.Render = colour;
            Utilities.SetStateChanged(weapon,"CBaseModelEntity","m_clrRender");
        }
    }

    static public CBasePlayerWeapon? FindWeapon(this CCSPlayerController? player, String name)
    {
        // only care if player is alive
        if(!player.IsLegalAlive())
        {
            return null;
        }

        CCSPlayerPawn? pawn = player.Pawn();

        if(pawn == null)
        {
            return null;
        }

        var weapons = pawn.WeaponServices?.MyWeapons;

        if(weapons == null)
        {
            return null;
        }

        foreach (var weaponOpt in weapons)
        {
            CBasePlayerWeapon? weapon = weaponOpt.Value;

            if(weapon == null)
            {
                continue;
            }
         
            if(weapon.DesignerName.Contains(name))
            {
                return weapon;
            }
        }

        return null;
    }



    static public void SetAmmo(this CBasePlayerWeapon? weapon, int clip, int reserve)
    {
        if(!weapon.IsLegal())
        {
            return;
        }

        // overide reserve max so it doesn't get clipped when
        // setting "infinite ammo"
        // thanks 1Mack
        CCSWeaponBaseVData? weaponData = weapon.As<CCSWeaponBase>().VData;
    
        
        if(weaponData != null)
        {
            // TODO: this overide it for every gun the player has...
            // when not a map gun, this is not a big deal
            // for the reserve ammo it is for the clip though
        /*
            if(clip > weaponData.MaxClip1)
            {
                weaponData.MaxClip1 = clip;
            }
        */
            if(reserve > weaponData.PrimaryReserveAmmoMax)
            {
                weaponData.PrimaryReserveAmmoMax = reserve;
            }
        }

        if(clip != -1)
        {
            weapon.Clip1 = clip;
            Utilities.SetStateChanged(weapon,"CBasePlayerWeapon","m_iClip1");
        }

        if(reserve != -1)
        {
            weapon.ReserveAmmo[0] = reserve;
            Utilities.SetStateChanged(weapon,"CBasePlayerWeapon","m_pReserveAmmo");
        }
    }

    public static void EventGunMenuCallback(CCSPlayerController player, ChatMenuOption option)
    {
        // Event has been cancelled in the mean time dont give any guns
        if(!JailPlugin.EventActive())
        {
            return;
        }

        GunMenuGive(player,option);
    }

    static public void EventGunMenu(this CCSPlayerController? player)
    {
        // Event has been cancelled in the mean time dont give any guns
        if(!JailPlugin.EventActive())
        {
            return;
        }

        player.GunMenuInternal(false,EventGunMenuCallback);
    }
    public static KitsuneMenu Menu { get; set; } = null!;

   
    public static void GunMenuGive(this CCSPlayerController player, ChatMenuOption option)
    {
        if(!player.IsLegalAlive())
        {
            return;
        }

        player.StripWeapons();

        player.GiveMenuWeapon(option.Text);
        player.GiveWeapon("deagle");

        player.GiveArmour();

        CBasePlayerWeapon? primary = player.FindWeapon(GUN_LIST[option.Text]);
        primary.SetAmmo(-1,999);

        CBasePlayerWeapon? secondary = player.FindWeapon("deagle");
        secondary.SetAmmo(-1,999);
    }

    static void GiveMenuWeaponCallback(this CCSPlayerController player, ChatMenuOption option)
    {
        if(!player.IsLegal())
        {
            return;
        }

        GunMenuGive(player,option);
    }

    static public void GunMenuInternal(this CCSPlayerController? player, bool no_awp, Action<CCSPlayerController, ChatMenuOption> callback)
    {
        // player must be alive and active!
        if (!player.IsLegalAlive())
        {
            return;
        }


        List<MenuItem> guns = new List<MenuItem>();
        var items = new List<MenuItem>();
        foreach (var weapon_pair in GUN_LIST)
        {
            var weapon_name = weapon_pair.Key;




            items.Add(new MenuItem(MenuItemType.Button, new MenuValue(weapon_name)));
            {


            };
        }

        // Afișează meniul Kitsune
        Menu?.ShowScrollableMenu(player, "Meniu de arme", items, (buttons, Menu, selected) =>
        {
            if (selected == null) return;
            // Identifică arma aleasă
            string selectedWeaponName = selected.DataString;
            if (!GUN_LIST.TryGetValue(selectedWeaponName, out string weaponCode))
            {
                player.LocalizeAnnounce("[server]","failed");
                return;
            }
            player.LocalizeAnnounce("[Server]","passed");
            // Curăță armele jucătorului și oferă noua armă
            player.StripWeapons();
            player.GiveWeapon(weaponCode);
            player.GiveWeapon("deagle"); // Adaugă un pistol implicit



        }, isSubmenu: false, freezePlayer: false);


    }

    public static Dictionary<String, String> GUN_LIST = new Dictionary<String, String>()
    {
        {"AK47","ak47"},
        {"M4","m4a1_silencer"},
        {"GALIL", "galilar" },
        {"M3","nova"},
        {"MAG", "mag7" },
        {"SAWED-OFF", "sawedoff" },
        {"P90","p90"},
        {"M249","m249"},
        {"NEGEV", "negev"},
        {"MP5","mp5sd"},
        {"FAL","galilar"},
        {"SG556","sg556"},
        {"BIZON","bizon"},
        {"AUG","aug"},
        {"FAMAS","famas"},
        {"XM1014","xm1014"},
        {"SCOUT","ssg08"},
        {"AWP", "awp"},
        {"G3SG1", "g3sg1" }

    };

    public static Dictionary<String, String> PISTOL_LIST = new Dictionary<String, String>()
    {

        {"DEAGLE", "deagle" },
        {"GLOCK", "glock" },
        {"CZ75A", "cz75a" },
        {"USP", "usp_silencer" },
        {"TEC9", "tec9" },
        {"P250", "p250" },
        {"FIVESEVEN", "fiveseven" },

    };


    public static String GunGiveName(String name)
    {
        return "weapon_" + GUN_LIST[name];
    }

    static public void GiveWeapon(this CCSPlayerController? player,String name)
    {
        if(player.IsLegalAlive())
        {
            player.GiveNamedItem("weapon_" + name);
        }
    }


    static public void GiveMenuWeapon(this CCSPlayerController? player,String name)
    {
        player.GiveWeapon(GUN_LIST[name]);
    }

    

    static public void GunMenu(this CCSPlayerController? player, bool no_awp)
    {
        // give bots some test guns
        if(player.IsLegalAlive() && player.IsBot)
        {
            player.GiveWeapon("ak47");
            player.GiveWeapon("deagle");
        }

        GunMenuInternal(player,no_awp,GiveMenuWeaponCallback);
    }    
}