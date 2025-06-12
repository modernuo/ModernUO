using System;
using Server;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.Systems.JailSystem
{
     public class JailRecordGump : Gump
     {
          private PlayerMobile m_Player;
          private JailSystem.JailRecord m_Record;

          public JailRecordGump(PlayerMobile player, JailSystem.JailRecord record) : base(50, 50)
          {
               m_Player = player;
               m_Record = record;

               Closable = true;
               Disposable = true;
               Draggable = true;
               Resizable = false;

               AddPage(0);
               AddBackground(0, 0, 330, 300, 9200);
               AddAlphaRegion(10, 10, 310, 280);

               AddHtml(20, 20, 360, 30, "<size=6 color=#FF6600>JAIL RECORD</basefont>", false, false);
               AddHtml(20, 60, 360, 25, $"<size=5 color=#FFFF00>Player: {player.Name}</basefont>", false, false);
               AddHtml(20, 80, 360, 25, $"<size=5 color=#FFFFFF>Jail Count: {record.JailCount}</basefont>", false, false);

               string lastJailedText = record.LastJailed == DateTime.MinValue ? "Never" : record.LastJailed.ToString("MM/dd/yyyy HH:mm");
               AddHtml(20, 100, 360, 25, $"<size=5 color=#FFFFFF>Last Jailed: {lastJailedText}</basefont>", false, false);

               string reasonText = string.IsNullOrEmpty(record.LastJailReason) ? "" : record.LastJailReason;
               AddHtml(20, 120, 290, 25, $"<size=4 color=#FFFFFF>Jail Reason: {reasonText}</basefont>", false, true);

               if (record.IsCurrentlyJailed)
               {
                    TimeSpan remaining = record.JailEndTime - Core.Now;
               
                    AddHtml(20, 150, 360, 25, "<size=5 color=#FF6666>Status: Currently Jailed</basefont>", false, false);
               
                    if (remaining > TimeSpan.Zero)
                    {
                         if (remaining.TotalMinutes <= 2)
                         {
                              AddHtml(20, 170, 360, 25, "<size=5 color=#FFFF00>Jail Time: Less than 2 minutes...</basefont>", false, false);
                         }
                         else
                         {
                              string timeText = $"{remaining.Hours}h {remaining.Minutes}m";
                              AddHtml(20, 170, 360, 25, $"<size=5 color=#FF6666>Jail Time: {timeText}</basefont>", false, false);
                         }
                    }
               }
               else
               {
                    AddHtml(20, 150, 360, 25, "<size=5 color=#00FF00>Status: Not Jailed</basefont>", false, false);
               }

               AddHtml(20, 200, 360, 25, "<size=4 color=#CCCCCC>If you believe you were jailed in error,</basefont>", false, false);
               AddHtml(20, 220, 360, 25, "<size=4 color=#CCCCCC>please contact staff through normal channels.</basefont>", false, false);
               AddHtml(20, 240, 360, 25, "<size=4 color=#CCCCCC>Each time you are jailed, the wait increases.</basefont>", false, false);
               AddHtml(20, 260, 360, 25, "<size=4 color=#CCCCCC>Follow all shard rules to avoid future jail time.</basefont>", false, false);
          }
     }
}