using System;
using Server.Items;
using Server.Network;

namespace Server
{
#pragma warning disable CA1052
    public class LightCycle
    {
        public const int DayLevel = 0;
        public const int NightLevel = 12;
        public const int DungeonLevel = 26;
        public const int JailLevel = 9;

        private static int m_LevelOverride = int.MinValue;

        public static int LevelOverride
        {
            get => m_LevelOverride;
            set
            {
                m_LevelOverride = value;

                foreach (var ns in TcpServer.Instances)
                {
                    var m = ns.Mobile;

                    m?.CheckLightLevels(false);
                }
            }
        }

        public static void Initialize()
        {
            new LightCycleTimer().Start();
            EventSink.Login += OnLogin;

            CommandSystem.Register("GlobalLight", AccessLevel.GameMaster, Light_OnCommand);
        }

        [Usage("GlobalLight <value>"), Description("Sets the current global light level.")]
        private static void Light_OnCommand(CommandEventArgs e)
        {
            if (e.Length >= 1)
            {
                LevelOverride = e.GetInt32(0);
                e.Mobile.SendMessage($"Global light level override has been changed to {m_LevelOverride}.");
            }
            else
            {
                LevelOverride = int.MinValue;
                e.Mobile.SendMessage("Global light level override has been cleared.");
            }
        }

        public static void OnLogin(Mobile m)
        {
            m.CheckLightLevels(true);
        }

        public static int ComputeLevelFor(Mobile from)
        {
            if (m_LevelOverride > int.MinValue)
            {
                return m_LevelOverride;
            }

            Clock.GetTime(from.Map, from.X, from.Y, out var hours, out int minutes);

            /* OSI times:
             *
             * Midnight ->  3:59 AM : Night
             *  4:00 AM -> 11:59 PM : Day
             *
             * RunUO times:
             *
             * 10:00 PM -> 11:59 PM : Scale to night
             * Midnight ->  3:59 AM : Night
             *  4:00 AM ->  5:59 AM : Scale to day
             *  6:00 AM ->  9:59 PM : Day
             */

            return hours switch
            {
                < 4  => NightLevel,
                < 6  => NightLevel + ((hours - 4) * 60 + minutes) * (DayLevel - NightLevel) / 120,
                < 22 => DayLevel,
                < 24 => DayLevel + ((hours - 22) * 60 + minutes) * (NightLevel - DayLevel) / 120,
                _    => NightLevel
            };
        }

        private class LightCycleTimer : Timer
        {
            public LightCycleTimer() : base(TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(5.0))
            {
            }

            protected override void OnTick()
            {
                foreach (var ns in TcpServer.Instances)
                {
                    ns.Mobile?.CheckLightLevels(false);
                }
            }
        }

        public class NightSightTimer : Timer
        {
            private readonly Mobile m_Owner;

            public NightSightTimer(Mobile owner) : base(TimeSpan.FromMinutes(Utility.Random(15, 25)))
            {
                m_Owner = owner;
            }

            protected override void OnTick()
            {
                m_Owner.EndAction<LightCycle>();
                m_Owner.LightLevel = 0;
                BuffInfo.RemoveBuff(m_Owner, BuffIcon.NightSight);
            }
        }
    }
#pragma warning restore CA1052
}
