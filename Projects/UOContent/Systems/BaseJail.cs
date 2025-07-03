/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2025 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: BaseJail.cs                                                    *
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
using Server;
using Server.Mobiles;
using Server.Network;
using Server.Commands;
using Server.Regions;
using Server.Items;
using Server.Gumps;

namespace Server.Systems.JailSystem
{
     public static class JailSystem
     {
          // Jail locations (modify if using custom maps)
          private static readonly Point3D[] JailLocations = new Point3D[]
          {    // felucca void
               new Point3D(5276, 1164, 0),
               new Point3D(5286, 1164, 0),
               new Point3D(5296, 1164, 0),
               new Point3D(5306, 1164, 0),
               new Point3D(5276, 1174, 0),
               new Point3D(5286, 1174, 0),
               new Point3D(5286, 1174, 0),
               new Point3D(5306, 1174, 0),
               new Point3D(5283, 1184, 0),
               new Point3D(5304, 1184, 0)
          };

          // Jail map, change this for custom maps
          public static Map JailMap = Map.Felucca;
          private static readonly System.Random random = new System.Random();

          private static readonly HashSet<Mobile> CurrentlyBeingJailed = new HashSet<Mobile>();
          private static readonly Dictionary<Mobile, JailRecord> PlayerJailRecords = new Dictionary<Mobile, JailRecord>();
          private static readonly Dictionary<Mobile, Timer> JailTimers = new Dictionary<Mobile, Timer>();

          // Jail time scales from 5 minutes to 2 hours based on the number of offenses
          private static readonly TimeSpan MinJailTime = TimeSpan.FromMinutes(5);
          private static readonly TimeSpan MaxJailTime = TimeSpan.FromMinutes(120);

          // Release location, change this for custom maps
          private static readonly Point3D ReleaseLocation = new Point3D(1444, 1697, 10); // Britain Bank
          public static Map ReleaseMap = Map.Felucca;

          // [jail <player> [reason] - Jail with time escalation per offense (GM only)
          // [unjail <player> - Manual release from jail regardless of time (GM only)
          // [jailinfo <player> - Check jail status and history (GM only)
          // [jailrecord - Checks their own jail record stats (player access)
          public static void Initialize()
          {
               CommandSystem.Register("jail", AccessLevel.Counselor, new CommandEventHandler(Jail_OnCommand));
               CommandSystem.Register("unjail", AccessLevel.Counselor, new CommandEventHandler(Unjail_OnCommand));
               CommandSystem.Register("jailinfo", AccessLevel.Counselor, new CommandEventHandler(JailInfo_OnCommand));
               CommandSystem.Register("jailrecord", AccessLevel.Player, new CommandEventHandler(MyJailInfo_OnCommand));
          }

          public class JailRecord
          {
               public int JailCount { get; set; }
               public DateTime LastJailed { get; set; }
               public DateTime JailEndTime { get; set; }
               public bool IsCurrentlyJailed { get; set; }
               public string LastJailReason { get; set; }

               public JailRecord()
               {
                    JailCount = 0;
                    LastJailed = DateTime.MinValue;
                    JailEndTime = DateTime.MinValue;
                    IsCurrentlyJailed = false;
                    LastJailReason = "";
               }
          }

          // Can be used by other systems to check player jail status
          public static bool IsPlayerJailed(PlayerMobile player)
          {
               return PlayerJailRecords.ContainsKey(player) && PlayerJailRecords[player].IsCurrentlyJailed;
          }

          private static Point3D GetRandomJailLocation()
          {
               return JailLocations[random.Next(JailLocations.Length)];
          }

          private static TimeSpan CalculateJailTime(int jailCount)
          {
               double totalMinutes = MinJailTime.TotalMinutes + 
                    (jailCount - 1) * (MaxJailTime.TotalMinutes - MinJailTime.TotalMinutes) / 9.0;
               
               totalMinutes = Math.Max(MinJailTime.TotalMinutes, Math.Min(MaxJailTime.TotalMinutes, totalMinutes));
               
               return TimeSpan.FromMinutes(totalMinutes);
          }

          private static void JailPlayer(PlayerMobile player, string reason = "")
          {
               if (CurrentlyBeingJailed.Contains(player))
               {
                    return;
               }

               CurrentlyBeingJailed.Add(player);

               if (!PlayerJailRecords.ContainsKey(player))
               {
                    PlayerJailRecords[player] = new JailRecord();
               }

               JailRecord record = PlayerJailRecords[player];
               record.JailCount++;
               record.LastJailed = Core.Now;
               record.IsCurrentlyJailed = true;
               record.LastJailReason = reason;
               
               TimeSpan jailTime = CalculateJailTime(record.JailCount);
               record.JailEndTime = Core.Now + jailTime;

               player.Frozen = true;
               player.SendMessage(0x35, "You are being sent to jail!");
               player.PlaySound(0x204);

               CommandLogging.WriteLine(player, $"Player {player.Name} jailed for: {reason} (Offense #{record.JailCount}, {jailTime.TotalMinutes} minutes)");

               foreach (NetState ns in NetState.Instances)
               {
                    if (ns.Mobile is PlayerMobile staff && staff.AccessLevel >= AccessLevel.Counselor)
                    {
                         staff.SendMessage(0x35, $"Player {player.Name} has been jailed for {jailTime.TotalMinutes} minutes. Reason: {reason} (Offense #{record.JailCount})");
                    }
               }

               Timer.DelayCall(TimeSpan.FromSeconds(2.0), () => { DismountPlayer(player, reason, jailTime); });
          }

         private static void DismountPlayer(PlayerMobile player, string reason, TimeSpan jailTime)
          {
               try
               {
                    if (player.Mount != null)
                    {
                         IMount mount = player.Mount;
                         mount.Rider = null;
                         player.SendMessage(0x35, "You have been dismounted.");
                    }

                    CommandLogging.WriteLine(player, $"Player {player.Name} dismounted before jail teleport");

                    Timer.DelayCall(TimeSpan.FromSeconds(3.0), () => { TeleportToJail(player, reason, jailTime); });
               }
               catch (Exception ex)
               {
                    Console.WriteLine($"Error dismounting player {player.Name}: {ex.Message}");

                    Timer.DelayCall(TimeSpan.FromSeconds(3.0), () => { TeleportToJail(player, reason, jailTime); });
               }
          }

          private static void TeleportToJail(PlayerMobile player, string reason, TimeSpan jailTime)
          {
               try
               {
                    Point3D jailLocation = GetRandomJailLocation();

                    player.MoveToWorld(jailLocation, JailMap);

                    player.SendMessage(0x35, "Use [jailrecord to pull up your record.");
                    player.SendMessage(0x35, "Please contact staff if you believe this was a mistake.");

                    CommandLogging.WriteLine(player, $"Player {player.Name} teleported to jail at {jailLocation}");

                    Timer.DelayCall(TimeSpan.FromSeconds(5.0), () => { UnfreezePlayer(player, reason, jailTime); });
               }
               catch (Exception ex)
               {
                    Console.WriteLine($"Error teleporting player {player.Name}: {ex.Message}");

                    player.Frozen = false;
                    CurrentlyBeingJailed.Remove(player);

                    if (PlayerJailRecords.ContainsKey(player))
                    {
                         PlayerJailRecords[player].IsCurrentlyJailed = false;
                    }
               }
          }

          private static void UnfreezePlayer(PlayerMobile player, string reason, TimeSpan jailTime)
          {
               try
               {
                    player.Frozen = false;

                    CommandLogging.WriteLine(player, $"Player {player.Name} unfrozen in jail");

                    CurrentlyBeingJailed.Remove(player);

                    Timer releaseTimer = Timer.DelayCall(jailTime, () => { ReleasePlayer(player); });
                    JailTimers[player] = releaseTimer;
               }
               catch (Exception ex)
               {
                    Console.WriteLine($"Error unfreezing player {player.Name}: {ex.Message}");
                    player.Frozen = false;

                    CurrentlyBeingJailed.Remove(player);

                    if (PlayerJailRecords.ContainsKey(player))
                    {
                         PlayerJailRecords[player].IsCurrentlyJailed = false;
                    }
               }
          }

          private static void ReleasePlayer(PlayerMobile player)
          {
               try
               {
                    if (PlayerJailRecords.ContainsKey(player))
                    {
                         PlayerJailRecords[player].IsCurrentlyJailed = false;
                    }

                    if (JailTimers.ContainsKey(player))
                    {
                         JailTimers.Remove(player);
                    }

                    // Freeze player for release sequence
                    player.Frozen = true;
                    player.SendMessage(0x35, "You have been released from jail!");
                    player.PlaySound(0x1FF);

                    CommandLogging.WriteLine(player, $"Player {player.Name} released from jail, starting teleport sequence");

                    foreach (NetState ns in NetState.Instances)
                    {
                         if (ns.Mobile is PlayerMobile staff && staff.AccessLevel >= AccessLevel.Counselor)
                         {
                              staff.SendMessage(0x35, $"Player {player.Name} has been released from jail.");
                         }
                    }

                    Timer.DelayCall(TimeSpan.FromSeconds(5.0), () => { TeleportFromJail(player); });
               }
               catch (Exception ex)
               {
                    Console.WriteLine($"Error releasing player {player.Name}: {ex.Message}");
               }
          }

          private static void TeleportFromJail(PlayerMobile player)
          {
               try
               {
                    player.MoveToWorld(ReleaseLocation, ReleaseMap);

                    CommandLogging.WriteLine(player, $"Player {player.Name} teleported from jail to {ReleaseLocation}");

                    Timer.DelayCall(TimeSpan.FromSeconds(5.0), () => { UnfreezeFromRelease(player); });
               }
               catch (Exception ex)
               {
                    Console.WriteLine($"Error teleporting player {player.Name} from jail: {ex.Message}");
                    
                    player.Frozen = false;
               }
          }

          private static void UnfreezeFromRelease(PlayerMobile player)
          {
               try
               {
                    player.Frozen = false;
                    player.SendMessage(0x35, "Welcome back!");
                    player.SendMessage(0x35, "Please follow the shard rules.");
                    player.SendMessage(0x35, "Have a nice day!");

                    CommandLogging.WriteLine(player, $"Player {player.Name} unfrozen after jail release");
               }
               catch (Exception ex)
               {
                    Console.WriteLine($"Error unfreezing player {player.Name} after release: {ex.Message}");
                    
                    player.Frozen = false;
               }
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

               string playerName = e.GetString(0);
               
               string reason = "";

               if (e.Length > 1)
               {
                    string fullCommand = e.ArgString;
                    int playerNameEnd = fullCommand.IndexOf(playerName) + playerName.Length;
        
                    if (playerNameEnd < fullCommand.Length)
                    {
                         reason = fullCommand.Substring(playerNameEnd).Trim();
                    }
               }

               Mobile target = null;
               
               foreach (Mobile m in World.Mobiles.Values)
               {
                    if (m is PlayerMobile && string.Equals(m.Name, playerName, StringComparison.OrdinalIgnoreCase))
                    {
                         target = m;
                         break;
                    }
               }

               if (target == null)
               {
                    e.Mobile.SendMessage(0x35, $"Player '{playerName}' not found.");
                    return;
               }

               if (!(target is PlayerMobile player))
               {
                    e.Mobile.SendMessage(0x35, "Only players have jail records.");
                    return;
               }

               if (player.AccessLevel > AccessLevel.Player)
               {
                    e.Mobile.SendMessage(0x35, "You cannot jail staff members.");
                    return;
               }

               if (PlayerJailRecords.ContainsKey(player) && PlayerJailRecords[player].IsCurrentlyJailed)
               {
                    e.Mobile.SendMessage(0x35, $"Player {player.Name} is already jailed.");
                    return;
               }

               JailPlayer(player, reason);
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

               string playerName = e.GetString(0);
               Mobile target = null;

               foreach (Mobile m in World.Mobiles.Values)
               {
                    if (m is PlayerMobile && string.Equals(m.Name, playerName, StringComparison.OrdinalIgnoreCase))
                    {
                         target = m;
                         break;
                    }
               }

               if (target == null)
               {
                    e.Mobile.SendMessage(0x35, $"Player '{playerName}' not found.");
                    return;
               }

               if (!(target is PlayerMobile player))
               {
                    e.Mobile.SendMessage(0x35, "Only players have jail records.");
                    return;
               }

               if (!PlayerJailRecords.ContainsKey(player) || !PlayerJailRecords[player].IsCurrentlyJailed)
               {
                    e.Mobile.SendMessage(0x35, $"Player {player.Name} is not currently jailed.");
                    return;
               }

               if (JailTimers.ContainsKey(player))
               {
                    JailTimers[player].Stop();
                    JailTimers.Remove(player);
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

               string playerName = e.GetString(0);
               Mobile target = null;

               foreach (Mobile m in World.Mobiles.Values)
               {
                    if (m is PlayerMobile && string.Equals(m.Name, playerName, StringComparison.OrdinalIgnoreCase))
                    {
                         target = m;
                         break;
                    }
               }

               if (target == null)
               {
                    e.Mobile.SendMessage(0x35, $"Player '{playerName}' not found.");
                    return;
               }

               if (!(target is PlayerMobile player))
               {
                    e.Mobile.SendMessage(0x35, "Only players have jail records.");
                    return;
               }

               if (!(e.Mobile is PlayerMobile staff))
               {
                    e.Mobile.SendMessage(0x35, "This command is only available to staff.");
                    return;
               }

               if (!PlayerJailRecords.ContainsKey(player))
               {
                    JailRecord emptyRecord = new JailRecord();
                    staff.SendGump(new JailRecordGump(player, emptyRecord));
                    return;
               }

               JailRecord record = PlayerJailRecords[player];
               staff.SendGump(new JailRecordGump(player, record));
          }

          private static readonly Dictionary<Mobile, DateTime> JailRecordCooldowns = new Dictionary<Mobile, DateTime>();
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

               if (JailRecordCooldowns.ContainsKey(player))
               {
                    TimeSpan timeSinceLastUse = Core.Now - JailRecordCooldowns[player];

                    if (timeSinceLastUse < JailRecordCooldown)
                    {
                         TimeSpan remaining = JailRecordCooldown - timeSinceLastUse;
                         e.Mobile.SendMessage(0x35, $"You must wait {remaining.TotalSeconds:F0} seconds.");
                         return;
                    }
               }

               JailRecordCooldowns[player] = Core.Now;

               if (!PlayerJailRecords.ContainsKey(player))
               {
                    JailRecord emptyRecord = new JailRecord();
                    player.SendGump(new JailRecordGump(player, emptyRecord));
                    return;
               }

               JailRecord record = PlayerJailRecords[player];
               player.SendGump(new JailRecordGump(player, record));
          }

          // for other systems to use
          public static void ManualJail(PlayerMobile player, string reason = "")
          {
               JailPlayer(player, reason);
          }
     }
}
