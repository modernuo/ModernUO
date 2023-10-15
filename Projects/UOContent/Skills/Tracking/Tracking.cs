using System;
using System.Buffers;
using System.Collections.Generic;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;
using Server.Spells;
using Server.Spells.Necromancy;

namespace Server.SkillHandlers
{
    public static class Tracking
    {
        private static readonly Dictionary<Mobile, TrackingInfo> m_Table = new();

        public static unsafe void Configure()
        {
            IncomingExtendedCommandPackets.RegisterExtended(0x07, true, &QuestArrow);
        }

        public static void Initialize()
        {
            SkillInfo.Table[(int)SkillName.Tracking].Callback = OnUse;
        }

        public static void QuestArrow(NetState state, SpanReader reader, int packetLength)
        {
            if (state.Mobile is PlayerMobile from)
            {
                var rightClick = reader.ReadBoolean();

                from.QuestArrow?.OnClick(rightClick);
            }
        }

        public static TimeSpan OnUse(Mobile m)
        {
            if (m is PlayerMobile pm)
            {
                m.SendLocalizedMessage(1011350); // What do you wish to track?

                m.CloseGump<TrackWhatGump>();
                m.CloseGump<TrackWhoGump>();
                m.SendGump(new TrackWhatGump(pm));
            }

            return TimeSpan.FromSeconds(10.0); // 10 second delay before being able to re-use a skill
        }

        public static void AddInfo(Mobile tracker, Mobile target)
        {
            var info = new TrackingInfo(tracker, target);
            m_Table[tracker] = info;
        }

        public static double GetStalkingBonus(Mobile tracker, Mobile target)
        {
            if (!m_Table.Remove(tracker, out var info) || info.m_Target != target || info.m_Map != target.Map)
            {
                return 0.0;
            }

            var xDelta = info.m_Location.X - target.X;
            var yDelta = info.m_Location.Y - target.Y;

            var bonus = Math.Sqrt(xDelta * xDelta + yDelta * yDelta);

            return Core.ML ? Math.Min(bonus, 10 + tracker.Skills.Tracking.Value / 10) : bonus;
        }

        public static void ClearTrackingInfo(Mobile tracker)
        {
            m_Table.Remove(tracker);
        }

        public class TrackingInfo
        {
            public Point2D m_Location;
            public Map m_Map;
            public Mobile m_Target;
            public Mobile m_Tracker;

            public TrackingInfo(Mobile tracker, Mobile target)
            {
                m_Tracker = tracker;
                m_Target = target;
                m_Location = new Point2D(target);
                m_Map = target.Map;
            }
        }
    }

    public class TrackWhatGump : Gump
    {
        private readonly PlayerMobile m_From;
        private readonly bool m_Success;

        public TrackWhatGump(PlayerMobile from) : base(20, 30)
        {
            m_From = from;
            m_Success = from.CheckSkill(SkillName.Tracking, 0.0, 21.1);

            AddPage(0);

            AddBackground(0, 0, 440, 135, 5054);

            AddBackground(10, 10, 420, 75, 2620);
            AddBackground(10, 85, 420, 25, 3000);

            AddItem(20, 20, 9682);
            AddButton(20, 110, 4005, 4007, 1);
            AddHtmlLocalized(20, 90, 100, 20, 1018087); // Animals

            AddItem(120, 20, 9607);
            AddButton(120, 110, 4005, 4007, 2);
            AddHtmlLocalized(120, 90, 100, 20, 1018088); // Monsters

            AddItem(220, 20, 8454);
            AddButton(220, 110, 4005, 4007, 3);
            AddHtmlLocalized(220, 90, 100, 20, 1018089); // Human NPCs

            AddItem(320, 20, 8455);
            AddButton(320, 110, 4005, 4007, 4);
            AddHtmlLocalized(320, 90, 100, 20, 1018090); // Players
        }

        public override void OnResponse(NetState state, RelayInfo info)
        {
            if (info.ButtonID >= 1 && info.ButtonID <= 4)
            {
                TrackWhoGump.DisplayTo(m_Success, m_From, info.ButtonID - 1);
            }
        }
    }

    public delegate bool TrackTypeDelegate(Mobile m);

    public class TrackWhoGump : Gump
    {
        private static readonly TrackTypeDelegate[] m_Delegates =
        {
            IsAnimal,
            IsMonster,
            IsHumanNPC,
            IsPlayer
        };

        private readonly PlayerMobile m_From;

        private readonly List<Mobile> m_List;
        private readonly int m_Range;

        private TrackWhoGump(PlayerMobile from, List<Mobile> list, int range) : base(20, 30)
        {
            m_From = from;
            m_List = list;
            m_Range = range;

            AddPage(0);

            AddBackground(0, 0, 440, 155, 5054);

            AddBackground(10, 10, 420, 75, 2620);
            AddBackground(10, 85, 420, 45, 3000);

            if (list.Count > 4)
            {
                AddBackground(0, 155, 440, 155, 5054);

                AddBackground(10, 165, 420, 75, 2620);
                AddBackground(10, 240, 420, 45, 3000);

                if (list.Count > 8)
                {
                    AddBackground(0, 310, 440, 155, 5054);

                    AddBackground(10, 320, 420, 75, 2620);
                    AddBackground(10, 395, 420, 45, 3000);
                }
            }

            for (var i = 0; i < list.Count && i < 12; ++i)
            {
                var m = list[i];

                AddItem(20 + i % 4 * 100, 20 + i / 4 * 155, ShrinkTable.Lookup(m));
                AddButton(20 + i % 4 * 100, 130 + i / 4 * 155, 4005, 4007, i + 1);

                if (m.Name != null)
                {
                    AddHtml(20 + i % 4 * 100, 90 + i / 4 * 155, 90, 40, m.Name);
                }
            }
        }

        public static void DisplayTo(bool success, PlayerMobile from, int type)
        {
            if (!success)
            {
                from.SendLocalizedMessage(1018092); // You see no evidence of those in the area.
                return;
            }

            var map = from.Map;

            if (map == null)
            {
                return;
            }

            var check = m_Delegates[type];

            from.CheckSkill(SkillName.Tracking, 21.1, 100.0); // Passive gain

            var range = 10 + (int)(from.Skills.Tracking.Value / 10);

            var eable = from.GetMobilesInRange(range);
            var list = new List<Mobile>();
            foreach (var m in eable)
            {
                if (m != from && (!Core.AOS || m.Alive) &&
                    (!m.Hidden || m.AccessLevel == AccessLevel.Player || from.AccessLevel > m.AccessLevel) &&
                    check(m) && CheckDifficulty(from, m))
                {
                    list.Add(m);
                }
            }

            if (list.Count > 0)
            {
                list.Sort(new InternalSorter(from));

                from.SendGump(new TrackWhoGump(from, list, range));
                from.SendLocalizedMessage(1018093); // Select the one you would like to track.
            }
            else
            {
                if (type == 0)
                {
                    from.SendLocalizedMessage(502991); // You see no evidence of animals in the area.
                }
                else if (type == 1)
                {
                    from.SendLocalizedMessage(502993); // You see no evidence of creatures in the area.
                }
                else
                {
                    from.SendLocalizedMessage(502995); // You see no evidence of people in the area.
                }
            }
        }

        // Tracking players uses tracking and detect hidden vs. hiding and stealth
        private static bool CheckDifficulty(Mobile from, Mobile m)
        {
            if (!Core.AOS || !m.Player)
            {
                return true;
            }

            var tracking = from.Skills.Tracking.Fixed;
            var detectHidden = from.Skills.DetectHidden.Fixed;

            if (Core.ML && m.Race == Race.Elf)
            {
                tracking /= 2; // The 'Guide' says that it requires twice as Much tracking SKILL to track an elf.  Not the total difficulty to track.
            }

            var hiding = m.Skills.Hiding.Fixed;
            var stealth = m.Skills.Stealth.Fixed;
            var divisor = hiding + stealth;

            // Necromancy forms affect tracking difficulty
            if (TransformationSpellHelper.UnderTransformation(m, typeof(HorrificBeastSpell)))
            {
                divisor -= 200;
            }
            else if (TransformationSpellHelper.UnderTransformation(m, typeof(VampiricEmbraceSpell)) && divisor < 500)
            {
                divisor = 500;
            }
            else if (TransformationSpellHelper.UnderTransformation(m, typeof(WraithFormSpell)) && divisor <= 2000)
            {
                divisor += 200;
            }

            int chance;
            if (divisor > 0)
            {
                if (Core.SE)
                {
                    chance = 50 * (tracking * 2 + detectHidden) / divisor;
                }
                else
                {
                    chance = 50 * (tracking + detectHidden + 10 * Utility.RandomMinMax(1, 20)) / divisor;
                }
            }
            else
            {
                chance = 100;
            }

            return chance > Utility.Random(100);
        }

        private static bool IsAnimal(Mobile m) => !m.Player && m.Body.IsAnimal;

        private static bool IsMonster(Mobile m) => !m.Player && m.Body.IsMonster;

        private static bool IsHumanNPC(Mobile m) => !m.Player && m.Body.IsHuman;

        private static bool IsPlayer(Mobile m) => m.Player;

        public override void OnResponse(NetState state, RelayInfo info)
        {
            var index = info.ButtonID - 1;

            if (index >= 0 && index < m_List.Count && index < 12)
            {
                var m = m_List[index];

                m_From.QuestArrow = new TrackArrow(m_From, m, m_Range * 2);

                if (Core.SE)
                {
                    Tracking.AddInfo(m_From, m);
                }
            }
        }

        private class InternalSorter : IComparer<Mobile>
        {
            private readonly Mobile m_From;

            public InternalSorter(Mobile from) => m_From = from;

            public int Compare(Mobile x, Mobile y)
            {
                if (x == null && y == null)
                {
                    return 0;
                }

                if (x == null)
                {
                    return -1;
                }

                if (y == null)
                {
                    return 1;
                }

                return m_From.GetDistanceToSqrt(x).CompareTo(m_From.GetDistanceToSqrt(y));
            }
        }
    }
}
