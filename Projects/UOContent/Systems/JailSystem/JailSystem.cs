/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2025 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: JailSystem.cs                                                   *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System;
using System.Collections.Generic;
using Server.Mobiles;
using Server.Network;
using Server.Commands;
using Server.Gumps;

namespace Server.Systems.JailSystem;

public class JailSystem : GenericPersistence
{
    // Jail locations (modify if using custom maps)
    // felucca void
    private static readonly Point3D[] JailLocations =
    [
        new(5276, 1164, 0),
        new(5286, 1164, 0),
        new(5296, 1164, 0),
        new(5306, 1164, 0),
        new(5276, 1174, 0),
        new(5286, 1174, 0),
        new(5296, 1174, 0),
        new(5306, 1174, 0),
        new(5283, 1184, 0),
        new(5304, 1184, 0)
    ];

    // Jail map, change this for custom maps
    public static readonly Map JailMap = Map.Felucca;

    private static readonly HashSet<PlayerMobile> CurrentlyBeingJailed = [];
    private static readonly Dictionary<PlayerMobile, JailRecord> PlayerJailRecords = [];
    private static readonly Dictionary<PlayerMobile, Timer> JailTimers = [];

    // Jail time scales from 5 minutes to 2 hours based on the number of offenses
    private static readonly TimeSpan MinJailTime = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan MaxJailTime = TimeSpan.FromHours(12);

    // Release location, change this for custom maps
    private static readonly Point3D ReleaseLocation = new(1444, 1697, 10); // Britain Bank
    public static readonly Map ReleaseMap = Map.Felucca;

    private static JailSystem Instance;

    // [jail <player> [reason] - Jail with time escalation per offense (GM only)
    // [unjail <player> - Manual release from jail regardless of time (GM only)
    // [jailinfo <player> - Check jail status and history (GM only)
    // [jailrecord - Checks their own jail record stats (player access)

    public static void Configure()
    {
        Instance = new JailSystem();
        CommandSystem.Register("jail", AccessLevel.Counselor, Jail_OnCommand);
        CommandSystem.Register("unjail", AccessLevel.Counselor, Unjail_OnCommand);
        CommandSystem.Register("jailinfo", AccessLevel.Counselor, JailInfo_OnCommand);
        CommandSystem.Register("jailrecord", AccessLevel.Player, MyJailInfo_OnCommand);
    }

    public JailSystem() : base("Jail", 3)
    {
    }

    // Can be used by other systems to check player jail status
    public static bool IsPlayerJailed(PlayerMobile player) =>
        PlayerJailRecords.GetValueOrDefault(player)?.IsCurrentlyJailed == true;

    private static TimeSpan CalculateJailTime(int jailCount)
    {
        var totalMinutes = MinJailTime + (jailCount - 1) * (MaxJailTime - MinJailTime) / 9.0;

        return totalMinutes.Clamp(MinJailTime, MaxJailTime);
    }

    public static void JailPlayer(Mobile from, PlayerMobile player, string reason = "")
    {
        if (!CurrentlyBeingJailed.Add(player))
        {
            return;
        }

        if (!PlayerJailRecords.TryGetValue(player, out var record))
        {
            PlayerJailRecords[player] = record = new JailRecord();
        }

        record.JailCount++;
        record.LastJailed = Core.Now;
        record.LastJailReason = reason;

        var jailTime = CalculateJailTime(record.JailCount);
        record.JailEndTime = Core.Now + jailTime;

        player.Frozen = true;
        player.SendMessage(0x35, "You are being sent to jail!");
        player.PlaySound(0x204);

        CommandLogging.WriteLine(from, $"Player {player.Name} jailed for: {reason} (Offense #{record.JailCount}, {jailTime.TotalMinutes} minutes)");

        foreach (var ns in NetState.Instances)
        {
            if (ns.Mobile is PlayerMobile staff && staff.AccessLevel >= AccessLevel.Counselor)
            {
                staff.SendMessage(0x35, $"Player {player.Name} has been jailed for {jailTime.TotalMinutes} minutes. Reason: {reason} (Offense #{record.JailCount})");
            }
        }

        Timer.DelayCall(TimeSpan.FromSeconds(2.0), () => { DismountPlayer(player, jailTime); });
    }

    private static void DismountPlayer(PlayerMobile player, TimeSpan jailTime)
    {
        if (player.Mount != null)
        {
            var mount = player.Mount;
            mount.Rider = null;
            player.SendMessage(0x35, "You have been dismounted.");
        }

        CommandLogging.WriteLine(player, $"Player {player.Name} dismounted before jail teleport");

        Timer.DelayCall(TimeSpan.FromSeconds(3.0), TeleportToJail, player, jailTime);
    }

    private static void TeleportToJail(PlayerMobile player, TimeSpan jailTime)
    {
        var jailLocation = JailLocations.RandomElement();

        player.MoveToWorld(jailLocation, JailMap);

        player.SendMessage(0x35, "Use [jailrecord to pull up your record.");
        player.SendMessage(0x35, "Please contact staff if you believe this was a mistake.");

        CommandLogging.WriteLine(player, $"Player {player.Name} teleported to jail at {jailLocation}");

        Timer.DelayCall(TimeSpan.FromSeconds(5.0), UnfreezePlayer, player, jailTime);
    }

    private static void UnfreezePlayer(PlayerMobile player, TimeSpan jailTime)
    {
        player.Frozen = false;

        CommandLogging.WriteLine(player, $"Player {player.Name} unfrozen in jail");

        CurrentlyBeingJailed.Remove(player);

        var releaseTimer = Timer.DelayCall(jailTime, ReleasePlayer, player);
        JailTimers[player] = releaseTimer;
    }

    private static void ReleasePlayer(PlayerMobile player)
    {
        if (PlayerJailRecords.TryGetValue(player, out var record))
        {
            record.JailEndTime = Core.Now;
        }

        JailTimers.Remove(player);

        // Freeze player for release sequence
        player.Frozen = true;
        player.SendMessage(0x35, "You have been released from jail!");
        player.PlaySound(0x1FF);

        CommandLogging.WriteLine(player, $"Player {player.Name} released from jail, starting teleport sequence");

        foreach (var ns in NetState.Instances)
        {
            if (ns.Mobile is PlayerMobile staff && staff.AccessLevel >= AccessLevel.Counselor)
            {
                staff.SendMessage(0x35, $"Player {player.Name} has been released from jail.");
            }
        }

        Timer.DelayCall(TimeSpan.FromSeconds(5.0), TeleportFromJail, player);
    }

    private static void TeleportFromJail(PlayerMobile player)
    {
        player.MoveToWorld(ReleaseLocation, ReleaseMap);

        CommandLogging.WriteLine(player, $"Player {player.Name} teleported from jail to {ReleaseLocation}");

        Timer.DelayCall(TimeSpan.FromSeconds(5.0), UnfreezeFromRelease, player);
    }

    private static void UnfreezeFromRelease(PlayerMobile player)
    {
        player.Frozen = false;
        player.SendMessage(0x35, "Welcome back!");
        player.SendMessage(0x35, "Please follow the shard rules.");
        player.SendMessage(0x35, "Have a nice day!");

        CommandLogging.WriteLine(player, $"Player {player.Name} unfrozen after jail release");
    }

    [Usage("Jail <player> [reason]")]
    [Description("Jails a player with freeze/teleport/unfreeze sequence.")]
    private static void Jail_OnCommand(CommandEventArgs e)
    {
        if (e.Length < 1)
        {
            e.Mobile.SendMessage(0x35, "Usage: [jail <player> [reason]");
            return;
        }

        var playerName = e.GetString(0);

        var reason = "";

        if (e.Length > 1)
        {
            var fullCommand = e.ArgString;
            var playerNameEnd = fullCommand.IndexOfOrdinal(playerName) + playerName.Length;

            if (playerNameEnd < fullCommand.Length)
            {
                reason = fullCommand.AsSpan(0, playerNameEnd).Trim().ToString();
            }
        }

        PlayerMobile player = null;

        foreach (var m in World.Mobiles.Values)
        {
            if (m is PlayerMobile pm && m.Name.InsensitiveEquals(playerName))
            {
                player = pm;
                break;
            }
        }

        if (player == null)
        {
            e.Mobile.SendMessage(0x35, $"Player '{playerName}' not found.");
            return;
        }

        if (player.AccessLevel > AccessLevel.Player)
        {
            e.Mobile.SendMessage(0x35, "You cannot jail staff members.");
            return;
        }

        if (IsPlayerJailed(player))
        {
            e.Mobile.SendMessage(0x35, $"Player {player.Name} is already jailed.");
            return;
        }

        JailPlayer(e.Mobile, player, reason);
        e.Mobile.SendMessage(0x35, $"Player {player.Name} is being jailed. Reason: {reason}");
    }

    [Usage("Unjail <player>")]
    [Description("Manually releases a player from jail.")]
    private static void Unjail_OnCommand(CommandEventArgs e)
    {
        if (e.Length < 1)
        {
            e.Mobile.SendMessage(0x35, "Usage: [unjail <player>");
            return;
        }

        var playerName = e.GetString(0);
        PlayerMobile player = null;

        foreach (var m in World.Mobiles.Values)
        {
            if (m is PlayerMobile pm && m.Name.InsensitiveEquals(playerName))
            {
                player = pm;
                break;
            }
        }

        if (player == null)
        {
            e.Mobile.SendMessage(0x35, $"Player '{playerName}' not found.");
            return;
        }

        if (IsPlayerJailed(player))
        {
            e.Mobile.SendMessage(0x35, $"Player {player.Name} is not currently jailed.");
            return;
        }

        if (JailTimers.Remove(player, out var value))
        {
            value.Stop();
        }

        ReleasePlayer(player);
        e.Mobile.SendMessage(0x35, $"Player {player.Name} has been manually released from jail.");
    }

    [Usage("JailInfo <player>")]
    [Description("Shows jail information for a player.")]
    private static void JailInfo_OnCommand(CommandEventArgs e)
    {
        if (e.Length < 1)
        {
            e.Mobile.SendMessage(0x35, "Usage: [jailinfo <player>");
            return;
        }

        var playerName = e.GetString(0);
        PlayerMobile player = null;

        foreach (var m in World.Mobiles.Values)
        {
            if (m is PlayerMobile pm && m.Name.InsensitiveEquals(playerName))
            {
                player = pm;
                break;
            }
        }

        if (player == null)
        {
            e.Mobile.SendMessage(0x35, $"Player '{playerName}' not found.");
            return;
        }

        e.Mobile.SendGump(new JailRecordGump(player, PlayerJailRecords.GetValueOrDefault(player) ?? new JailRecord()));
    }

    private static readonly Dictionary<Mobile, DateTime> JailRecordCooldowns = new();
    private static readonly TimeSpan JailRecordCooldown = TimeSpan.FromSeconds(30);

    [Usage("JailRecord")]
    [Description("Shows your own jail information.")]
    private static void MyJailInfo_OnCommand(CommandEventArgs e)
    {
        if (!(e.Mobile is PlayerMobile player))
        {
            e.Mobile.SendMessage(0x35, "This command is only available to players.");
            return;
        }

        if (JailRecordCooldowns.TryGetValue(player, out var cooldown))
        {
            var timeSinceLastUse = Core.Now - cooldown;

            if (timeSinceLastUse < JailRecordCooldown)
            {
                var remaining = JailRecordCooldown - timeSinceLastUse;
                e.Mobile.SendMessage(0x35, $"You must wait {remaining.TotalSeconds:F0} seconds.");
                return;
            }
        }

        JailRecordCooldowns[player] = Core.Now;

        player.SendGump(new JailRecordGump(player, PlayerJailRecords.GetValueOrDefault(player) ?? new JailRecord()));
    }

    public override void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(0); // version
        writer.WriteEncodedInt(PlayerJailRecords.Count);
        foreach (var (m, record) in PlayerJailRecords)
        {
            writer.Write(m);
            record.Serialize(writer);
        }
    }

    public override void Deserialize(IGenericReader reader)
    {
        var version = reader.ReadEncodedInt();

        var count = reader.ReadEncodedInt();
        for (var i = 0; i < count; i++)
        {
            var player = reader.ReadEntity<PlayerMobile>();
            var record = new JailRecord();
            record.Deserialize(reader);

            if (player != null)
            {
                PlayerJailRecords[player] = record;
                if (record.IsCurrentlyJailed)
                {
                    CurrentlyBeingJailed.Add(player);
                    var jailTime = record.JailEndTime - Core.Now;
                    JailTimers[player] = Timer.DelayCall(jailTime, ReleasePlayer, player);
                }
            }
        }
    }
}
