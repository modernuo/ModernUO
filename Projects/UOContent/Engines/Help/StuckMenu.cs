using System;
using Server.Factions;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.Menus.Questions
{
    public class StuckMenuEntry
    {
        public StuckMenuEntry(int name, Point3D[] locations)
        {
            Name = name;
            Locations = locations;
        }

        public int Name { get; }

        public Point3D[] Locations { get; }
    }

    public class StuckMenu : Gump
    {
        private static readonly StuckMenuEntry[] m_Entries =
        {
            // Britain
            new(
                1011028,
                new[]
                {
                    new Point3D(1522, 1757, 28),
                    new Point3D(1519, 1619, 10),
                    new Point3D(1457, 1538, 30),
                    new Point3D(1607, 1568, 20),
                    new Point3D(1643, 1680, 18)
                }
            ),

            // Trinsic
            new(
                1011029,
                new[]
                {
                    new Point3D(2005, 2754, 30),
                    new Point3D(1993, 2827, 0),
                    new Point3D(2044, 2883, 0),
                    new Point3D(1876, 2859, 20),
                    new Point3D(1865, 2687, 0)
                }
            ),

            // Vesper
            new(
                1011030,
                new[]
                {
                    new Point3D(2973, 891, 0),
                    new Point3D(3003, 776, 0),
                    new Point3D(2910, 727, 0),
                    new Point3D(2865, 804, 0),
                    new Point3D(2832, 927, 0)
                }
            ),

            // Minoc
            new(
                1011031,
                new[]
                {
                    new Point3D(2498, 392, 0),
                    new Point3D(2433, 541, 0),
                    new Point3D(2445, 501, 15),
                    new Point3D(2501, 469, 15),
                    new Point3D(2444, 420, 15)
                }
            ),

            // Yew
            new(
                1011032,
                new[]
                {
                    new Point3D(490, 1166, 0),
                    new Point3D(652, 1098, 0),
                    new Point3D(650, 1013, 0),
                    new Point3D(536, 979, 0),
                    new Point3D(464, 970, 0)
                }
            ),

            // Cove
            new(
                1011033,
                new[]
                {
                    new Point3D(2230, 1159, 0),
                    new Point3D(2218, 1203, 0),
                    new Point3D(2247, 1194, 0),
                    new Point3D(2236, 1224, 0),
                    new Point3D(2273, 1231, 0)
                }
            )
        };

        private static readonly StuckMenuEntry[] m_T2AEntries =
        {
            // Papua
            new(
                1011057,
                new[]
                {
                    new Point3D(5720, 3109, -1),
                    new Point3D(5677, 3176, -3),
                    new Point3D(5678, 3227, 0),
                    new Point3D(5769, 3206, -2),
                    new Point3D(5777, 3270, -1)
                }
            ),

            // Delucia
            new(
                1011058,
                new[]
                {
                    new Point3D(5216, 4033, 37),
                    new Point3D(5262, 4049, 37),
                    new Point3D(5284, 4006, 37),
                    new Point3D(5189, 3971, 39),
                    new Point3D(5243, 3960, 37)
                }
            )
        };

        private readonly bool m_MarkUse;

        private readonly Mobile m_Mobile;
        private readonly Mobile m_Sender;

        private Timer m_Timer;

        public StuckMenu(Mobile beholder, Mobile beheld, bool markUse) : base(150, 50)
        {
            m_Sender = beholder;
            m_Mobile = beheld;
            m_MarkUse = markUse;

            Closable = false;
            Draggable = false;
            Disposable = false;

            AddBackground(0, 0, 270, 320, 2600);

            AddHtmlLocalized(50, 20, 250, 35, 1011027); // Chose a town:

            var entries = IsInSecondAgeArea(beheld) ? m_T2AEntries : m_Entries;

            for (var i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];

                AddButton(50, 55 + 35 * i, 208, 209, i + 1);
                AddHtmlLocalized(75, 55 + 35 * i, 335, 40, entry.Name);
            }

            AddButton(55, 263, 4005, 4007, 0);
            AddHtmlLocalized(90, 265, 200, 35, 1011012); // CANCEL
        }

        private static bool IsInSecondAgeArea(Mobile m) =>
            (m.Map == Map.Trammel || m.Map == Map.Felucca) &&
            (m.X >= 5120 && m.Y >= 2304 || m.Region.IsPartOf("Terathan Keep"));

        public void BeginClose()
        {
            StopClose();

            m_Timer = new CloseTimer(m_Mobile);
            m_Timer.Start();

            m_Mobile.Frozen = true;
        }

        public void StopClose()
        {
            m_Timer?.Stop();

            m_Mobile.Frozen = false;
        }

        public override void OnResponse(NetState state, RelayInfo info)
        {
            StopClose();

            if (Sigil.ExistsOn(m_Mobile))
            {
                m_Mobile.SendLocalizedMessage(1061632); // You can't do that while carrying the sigil.
            }
            else if (info.ButtonID == 0)
            {
                if (m_Mobile == m_Sender)
                {
                    m_Mobile.SendLocalizedMessage(1010588); // You choose not to go to any city.
                }
            }
            else
            {
                var index = info.ButtonID - 1;
                var entries = IsInSecondAgeArea(m_Mobile) ? m_T2AEntries : m_Entries;

                if (index >= 0 && index < entries.Length)
                {
                    Teleport(entries[index]);
                }
            }
        }

        private void Teleport(StuckMenuEntry entry)
        {
            if (m_MarkUse)
            {
                m_Mobile.SendLocalizedMessage(1010589); // You will be teleported within the next two minutes.

                new TeleportTimer(m_Mobile, entry, TimeSpan.FromSeconds(10.0 + Utility.RandomDouble() * 110.0)).Start();

                if (m_Mobile is PlayerMobile mobile)
                {
                    mobile.UsedStuckMenu();
                }
            }
            else
            {
                new TeleportTimer(m_Mobile, entry, TimeSpan.Zero).Start();
            }
        }

        private class CloseTimer : Timer
        {
            private readonly DateTime m_End;
            private readonly Mobile m_Mobile;

            public CloseTimer(Mobile m) : base(TimeSpan.Zero, TimeSpan.FromSeconds(1.0))
            {
                m_Mobile = m;
                m_End = Core.Now + TimeSpan.FromMinutes(3.0);
            }

            protected override void OnTick()
            {
                if (m_Mobile.NetState == null || Core.Now > m_End)
                {
                    m_Mobile.Frozen = false;
                    m_Mobile.CloseGump<StuckMenu>();

                    Stop();
                }
                else
                {
                    m_Mobile.Frozen = true;
                }
            }
        }

        private class TeleportTimer : Timer
        {
            private readonly StuckMenuEntry m_Destination;
            private readonly DateTime m_End;
            private readonly Mobile m_Mobile;

            public TeleportTimer(Mobile mobile, StuckMenuEntry destination, TimeSpan delay) : base(
                TimeSpan.Zero,
                TimeSpan.FromSeconds(1.0)
            )
            {
                Priority = TimerPriority.TwoFiftyMS;

                m_Mobile = mobile;
                m_Destination = destination;
                m_End = Core.Now + delay;
            }

            protected override void OnTick()
            {
                if (Core.Now < m_End)
                {
                    m_Mobile.Frozen = true;
                }
                else
                {
                    m_Mobile.Frozen = false;
                    Stop();

                    if (Sigil.ExistsOn(m_Mobile))
                    {
                        m_Mobile.SendLocalizedMessage(1061632); // You can't do that while carrying the sigil.
                        return;
                    }

                    var dest = m_Destination.Locations.RandomElement();

                    Map destMap;
                    if (m_Mobile.Map == Map.Trammel)
                    {
                        destMap = Map.Trammel;
                    }
                    else if (m_Mobile.Map == Map.Felucca)
                    {
                        destMap = Map.Felucca;
                    }
                    else
                    {
                        destMap = m_Mobile.Kills >= 5 ? Map.Felucca : Map.Trammel;
                    }

                    BaseCreature.TeleportPets(m_Mobile, dest, destMap);
                    m_Mobile.MoveToWorld(dest, destMap);
                }
            }
        }
    }
}
