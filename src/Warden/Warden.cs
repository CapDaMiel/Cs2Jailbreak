﻿

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
using CounterStrikeSharp.API.Core.Translations;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;
using CSTimer = CounterStrikeSharp.API.Modules.Timers;
using System.Drawing;
using PlayerModelChanger;
using CounterStrikeSharp.API.Modules.Timers;
using SQLitePCL;



public partial class Warden
{
    public Warden()
    {
        for(int p = 0; p < jailPlayers.Length; p++)
        {
            jailPlayers[p] = new JailPlayer();
        }
    }



    public CPointWorldText CreateHud(CCSPlayerController player, string text, int size = 100, Color? color = null, string font = "", float shiftX = 0f, float shiftY = 0f)
    {
        CCSPlayerPawn pawn = player?.PlayerPawn.Value!;

        var handle = new CHandle<CCSGOViewModel>((IntPtr)(pawn.ViewModelServices!.Handle + Schema.GetSchemaOffset("CCSPlayer_ViewModelServices", "m_hViewModel") + 4));
        if (!handle.IsValid)
        {
            CCSGOViewModel viewmodel = Utilities.CreateEntityByName<CCSGOViewModel>("predicted_viewmodel")!;
            viewmodel.DispatchSpawn();
            handle.Raw = viewmodel.EntityHandle.Raw;
            Utilities.SetStateChanged(pawn, "CCSPlayerPawnBase", "m_pViewModelServices");
        }

        CPointWorldText worldText = Utilities.CreateEntityByName<CPointWorldText>("point_worldtext")!;
        worldText.MessageText = text;
        worldText.Enabled = true;
        worldText.FontSize = size;
        worldText.Fullbright = true;
        worldText.Color = color ?? Color.Aquamarine;
        worldText.WorldUnitsPerPx = 0.01f;
        worldText.FontName = font;
        worldText.JustifyHorizontal = PointWorldTextJustifyHorizontal_t.POINT_WORLD_TEXT_JUSTIFY_HORIZONTAL_LEFT;
        worldText.JustifyVertical = PointWorldTextJustifyVertical_t.POINT_WORLD_TEXT_JUSTIFY_VERTICAL_TOP;
        worldText.ReorientMode = PointWorldTextReorientMode_t.POINT_WORLD_TEXT_REORIENT_NONE;

        QAngle eyeAngles = pawn.EyeAngles;
        Vector forward = new(), right = new(), up = new();
        NativeAPI.AngleVectors(eyeAngles.Handle, forward.Handle, right.Handle, up.Handle);

        Vector eyePosition = new();
        eyePosition += forward * 7;
        eyePosition += right * shiftX;
        eyePosition += up * shiftY;
        QAngle angles = new()
        {
            Y = eyeAngles.Y + 270,
            Z = 90 - eyeAngles.X,
            X = 0
        };

        worldText.DispatchSpawn();
        worldText.Teleport(pawn.AbsOrigin! + eyePosition + new Vector(0, 0, pawn.ViewOffset.Z), angles, null);
        Server.NextFrame(() =>
        {
            worldText.AcceptInput("SetParent", handle.Value, null, "!activator");
        });

        return worldText;
    }
    

    // Give a player warden
    public void SetWarden(int slot)
    {
        wardenSlot = slot;

        var player = Utilities.GetPlayerFromSlot(wardenSlot);

        // one last saftey check
        if(!player.IsLegal())
        {
            wardenSlot = INAVLID_SLOT;
            return;
        }

        Chat.LocalizeAnnounce(WARDEN_PREFIX,"warden.took_warden",player.PlayerName);

        player.LocalizeAnnounce(WARDEN_PREFIX,"warden.wcommand");

        wardenTimestamp = Lib.CurTimestamp();

        // change player color!
        //player.SetColour(Color.FromArgb(255, 0, 0, 255));

        player.PlayerPawn.Value.SetModel("characters/models/voikanaa/quiet_mgsv/quiet_mgsv.vmdl");
        Server.PrintToChatAll("d");
        player.Clan = "● Simon ●";
        CreateHud(player, $"Simon: {player.PlayerName}", 100, Color.Blue, "Arial", 10, 10);
        
        JailPlugin.logs.AddLocalized("warden.took_warden", player.PlayerName);
    }
    public int deputySlot { get; private set; } = INAVLID_SLOT;
    public void SetDeputy(int slot)
    {
        deputySlot = slot;

        var player = Utilities.GetPlayerFromSlot(deputySlot);

        // one last saftey check
        if (!player.IsLegal())
        {
            deputySlot = INAVLID_SLOT;
            Server.PrintToChatAll("dupa");
            return;
        }

        Chat.LocalizeAnnounce(WARDEN_PREFIX, "warden.took_deputy", player.PlayerName);

        // add health
        //JailPlugin._api?.AddHealth("JB - Role", player, 5, true, false);

        player.LocalizeAnnounce(WARDEN_PREFIX, "warden.dcommand");

        //player.PlayerPawn.Value.SetModel("characters/models/nozb1/laremicat_player_model/laremicat_player_model.vmdl");

    }

    public bool IsWarden(CCSPlayerController? player)
    {
        if(!player.IsLegal())
        {
            return false;
        }

        return player.Slot == wardenSlot;
    }

    void RemoveWardenInternal()
    {
        wardenSlot = INAVLID_SLOT;
        wardenTimestamp = -1;
    }

    public void RemoveWarden()
    {
        var player = Utilities.GetPlayerFromSlot(wardenSlot);

        if(player.IsLegal())
        {
            player.SetColour(Player.DEFAULT_COLOUR);
            Chat.LocalizeAnnounce(WARDEN_PREFIX,"warden.removed",player.PlayerName);
            JailPlugin.logs.AddLocalized("warden.removed", player.PlayerName);
        }

        RemoveWardenInternal();
        player.PlayerPawn.Value.SetModel("characters/models/nozb1/policeman_player_model/policeman_player_model.vmdl");
    }

    public void RemoveIfWarden(CCSPlayerController? player)
    {
        if(IsWarden(player))
        {
            RemoveWarden();
        }
    }


    public bool PlayerChat(CCSPlayerController? player, CommandInfo info)
    {
        String text = info.GetArg(1);

        if(text.StartsWith("/") || text.StartsWith("!") || String.IsNullOrWhiteSpace(text))
        {
            return true;
        }

        if(player.IsLegalAlive() && IsWarden(player))
        {
            Server.PrintToChatAll($"{WARDEN_PREFIX} {player.PlayerName}: {text}");
            return false;
        }   

        return true;
    }

    // reset variables for a new round
    void PurgeRound()
    {
        RemoveLaser();

        if(Config.wardenForceRemoval)
        {
            RemoveWardenInternal();
        }

        // reset player structs
        foreach(JailPlayer jailPlayer in jailPlayers)
        {
            jailPlayer.PurgeRound();
        }
    }

    void SetWardenIfLast(bool onDeath = false)
    {
        // dont override the warden if there is no death removal
        // also don't do it if an event is running because it's annoying
        if(!Config.wardenForceRemoval || JailPlugin.EventActive())
        {
            return;
        }

        // if there is only one ct automatically give them warden!
        var ctPlayers = Lib.GetAliveCt();

        if(ctPlayers.Count == 1)
        {
            int slot = ctPlayers[0].Slot;

            if(onDeath)
            {
                // play sfx for last ct
                // TODO: this is too loud as there is no way to control volume..
                //Lib.PlaySoundAll("sounds/vo/agents/sas/lastmanstanding03");
                var player = Utilities.GetPlayerFromSlot(slot);

                // Give last warden an extra bit of hp
                if(player.IsLegalAlive())
                {
                    player.SetHealth(150);
                }
            }
        
            SetWarden(slot);
        }
    }

    public void SetupPlayerGuns(CCSPlayerController? player)
    {
        // dont intefere with spawn guns if an event is running
        if(!player.IsLegalAlive() || JailPlugin.EventActive())
        {
            return;
        }

        // strip weapons just in case
        if(Config.stripSpawnWeapons)
        {
            player.StripWeapons();
        }

        if(player.IsCt())
        {
            if(Config.ctGuns)
            {
                var jailPlayer = JailPlayerFromPlayer(player);

                player.GiveWeapon("deagle");
                player.GiveWeapon("hegrenade");
                player.GiveWeapon("taser");
                player.GiveWeapon("tagrenade");
                player.GiveWeapon("incgrenade");
                player.GiveWeapon("smokegrenade");
                if(jailPlayer != null)
                {
                    player.GiveMenuWeapon("AK47");
                }
            }

            if(Config.ctArmour)
            {  
                player.SetArmour(100);
            }
        } 
    }

    // util func to get a jail player
    public JailPlayer? JailPlayerFromPlayer(CCSPlayerController? player)
    {
        if(!player.IsLegal())
        {
            return null;
        }

        return jailPlayers[player.Slot];
    }
    
    public CCSPlayerController? GetWarden()
    {
        if(wardenSlot == INAVLID_SLOT)
        {
            return null;
        }

        return Utilities.GetPlayerFromSlot(wardenSlot);
    }

    Countdown<int> chatCountdown = new Countdown<int>();

    CSTimer.Timer? tmpMuteTimer = null;
    long tmpMuteTimestamp = 0;

    const int INAVLID_SLOT = -3;   

    int wardenSlot = INAVLID_SLOT;
    
    public static String WARDEN_PREFIX = $" {ChatColors.Green}[Jailbreak]: {ChatColors.White}";

    long wardenTimestamp = -1;

    public JailConfig Config = new JailConfig();

    public JailPlayer[] jailPlayers = new JailPlayer[64];

    // slot for player for warden colour
    int colourSlot = -1;

    bool ctHandicap = false;

    public Warday warday = new Warday();
    public Block block = new Block();
    public Mute mute = new Mute();
};