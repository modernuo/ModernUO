// AFK Command v1.1.0
// Author: Felladrin
// Started: 2013-08-14
// Updated: 2016-01-03

using System;
using System.Collections.Generic;
using Server;
using Server.Commands;
using Server.Mobiles;

namespace Server
{
    public static class AFK
    {
        public static class Config
        {
            public static bool Enabled = true;    // Is this command enabled?
        }

        public static void Initialize()
        {
            if (Config.Enabled)
            {
                CommandSystem.Register("AFK", AccessLevel.Player, new CommandEventHandler(OnCommand));
                EventSink.Speech += OnSpeech;
            }
        }

        class AfkInfo
        {
            public string Message = "Away From Keyborad";
            public DateTime Time = DateTime.Now;
            public Point3D Location;

            public AfkInfo(string message, Point3D location)
            {
                Message = message;
                Location = location;
            }
        }

        static Dictionary<int, AfkInfo> PlayersAfk = new Dictionary<int, AfkInfo>();

        [Usage("AFK [<message>]")]
        [Description("Puts your char in 'Away From Keyboard' mode.")]
        static void OnCommand(CommandEventArgs e)
        {
            PlayerMobile pm = e.Mobile as PlayerMobile;

            if (isAFK(pm))
            {
                SetBack(pm);
            }
            else
            {
                if (e.Length == 0)
                {
                    SetAFK(pm, "Away From Keyborad");
                }
                else
                {
                    SetAFK(pm, e.ArgString);
                }

                AnnounceAFK(pm);
            }
        }

        static void OnSpeech(SpeechEventArgs e)
        {
            var playerMobile = e.Mobile as PlayerMobile;
            if (playerMobile != null && isAFK(playerMobile))
            {
                SetBack(playerMobile);
            }
        }

        static void AnnounceAFK(PlayerMobile pm)
        {
            if (!isAFK(pm))
                return;

            if (pm.Location != GetAFKLocation(pm) || pm.NetState == null || pm.Deleted)
            {
                SetBack(pm);
                return;
            }

            TimeSpan ts = GetAFKTimeSpan(pm);

            pm.Emote("*{0}*", GetAFKMessage(pm));

            if (ts.Hours != 0)
            {
                pm.Emote("*AFK for {0} hour{1} and {2} minute{3}*", ts.Hours, (ts.Hours > 1 ? "s" : ""), ts.Minutes, (ts.Minutes > 1 ? "s" : ""));
            }
            else if (ts.Minutes != 0)
            {
                pm.Emote("*AFK for {0} minute{1}*", ts.Minutes, (ts.Minutes > 1 ? "s" : ""));
            }
            else if (ts.Seconds != 0)
            {
                pm.Emote("*AFK for {0} seconds*", ts.Seconds);
            }

            Timer.DelayCall(TimeSpan.FromSeconds(10), delegate { AnnounceAFK(pm); });
        }

        static void SetAFK(Mobile m, string message)
        {
            PlayersAfk.Add((int)m.Serial.Value, new AfkInfo(message, m.Location));
            m.Emote("*Is now AFK*");
        }

        static void SetBack(Mobile m)
        {
            PlayersAfk.Remove((int)m.Serial.Value);
            m.Emote("*Returns to the game*");
        }

        static bool isAFK(IEntity e)
        {
            return PlayersAfk.ContainsKey((int)e.Serial.Value);
        }

        static string GetAFKMessage(IEntity e)
        {
            if (PlayersAfk.ContainsKey((int)e.Serial.Value))
            {
                AfkInfo info;
                PlayersAfk.TryGetValue((int)e.Serial.Value, out info);
                if (info != null)
                {
                    return info.Message;
                }
            }

            return "Away From Keyboard";
        }

        static Point3D GetAFKLocation(IEntity e)
        {
            if (PlayersAfk.ContainsKey((int)e.Serial.Value))
            {
                AfkInfo info;
                PlayersAfk.TryGetValue((int)e.Serial.Value, out info);
                if (info != null)
                {
                    return info.Location;
                }
            }

            return new Point3D();
        }

        static TimeSpan GetAFKTimeSpan(IEntity pm)
        {
            if (PlayersAfk.ContainsKey((int)pm.Serial.Value))
            {
                AfkInfo info;
                PlayersAfk.TryGetValue((int)pm.Serial.Value, out info);
                if (info != null)
                {
                    TimeSpan time;

                    try
                    {
                        time = DateTime.Now - info.Time;
                    }
                    catch
                    {
                        time = TimeSpan.Zero;
                    }

                    return time;
                }
            }

            return TimeSpan.Zero;
        }
    }
}
