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
        private static readonly Dictionary<Mobile, TrackingInfo> _table = new();

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
            _table[tracker] = info;
        }

        public static double GetStalkingBonus(Mobile tracker, Mobile target)
        {
            if (!_table.Remove(tracker, out var info) || info._target != target || info._map != target.Map)
            {
                return 0.0;
            }

            var xDelta = info._location.X - target.X;
            var yDelta = info._location.Y - target.Y;

            var bonus = Math.Sqrt(xDelta * xDelta + yDelta * yDelta);

            return Core.ML ? Math.Min(bonus, 10 + tracker.Skills.Tracking.Value / 10) : bonus;
        }

        public static void ClearTrackingInfo(Mobile tracker)
        {
            _table.Remove(tracker);
        }

        private class TrackingInfo
        {
            public Point2D _location;
            public readonly Map _map;
            public readonly Mobile _target;
            public Mobile _tracker;

            public TrackingInfo(Mobile tracker, Mobile target)
            {
                _tracker = tracker;
                _target = target;
                _location = new Point2D(target);
                _map = target.Map;
            }
        }
    }

    public class TrackWhatGump : Gump
    {
        private readonly PlayerMobile _from;
        private readonly bool _success;

        public TrackWhatGump(PlayerMobile from) : base(20, 30)
        {
            _from = from;
            _success = from.CheckSkill(SkillName.Tracking, 0.0, 21.1);

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
                TrackWhoGump.DisplayTo(_success, _from, info.ButtonID - 1);
            }
        }
    }

    public class TrackWhoGump : Gump
    {
        private const int MaxClosest = 12;

        private readonly PlayerMobile _from;
        private readonly Mobile[] _targets;
        private readonly int _range;

        private TrackWhoGump(PlayerMobile from, Mobile[] targets, int range) : base(20, 30)
        {
            _from = from;
            _targets = targets;
            _range = range;

            AddPage(0);

            AddBackground(0, 0, 440, 155, 5054);

            AddBackground(10, 10, 420, 75, 2620);
            AddBackground(10, 85, 420, 45, 3000);

            if (targets.Length > 4)
            {
                AddBackground(0, 155, 440, 155, 5054);

                AddBackground(10, 165, 420, 75, 2620);
                AddBackground(10, 240, 420, 45, 3000);

                if (targets.Length > 8)
                {
                    AddBackground(0, 310, 440, 155, 5054);

                    AddBackground(10, 320, 420, 75, 2620);
                    AddBackground(10, 395, 420, 45, 3000);
                }
            }

            for (var i = 0; i < targets.Length; ++i)
            {
                var m = targets[i];

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

            from.CheckSkill(SkillName.Tracking, 21.1, 100.0); // Passive gain

            var range = 10 + (int)(from.Skills.Tracking.Value / 10);

            var mobs = GetClosestMobs(from, range, type);

            if (mobs.Length > 0)
            {
                from.SendGump(new TrackWhoGump(from, mobs, range));
                from.SendLocalizedMessage(1018093); // Select the one you would like to track.
            }
            else if (type == 0)
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

        private static Mobile[] GetClosestMobs(Mobile from, int range, int type)
        {
            var loc = from.Location;

            // We only track the closest 12
            var mobs = new Mobile[MaxClosest];
            Span<double> distances = stackalloc double[MaxClosest];
            distances.Fill(double.MaxValue); // Fill with max values
            var total = 0;

            foreach (var m in from.GetMobilesInRange(range))
            {
                if (m == from || Core.AOS && !m.Alive ||
                    m.Hidden && m.AccessLevel != AccessLevel.Player && from.AccessLevel <= m.AccessLevel ||
                    !IsValidMobileType(m, type) || !CheckDifficulty(from, m))
                {
                    continue;
                }

                total++;

                var distance = m.GetDistanceToSqrt(loc);
                for (var i = 0; i < MaxClosest; i++)
                {
                    if (distance < distances[i])
                    {
                        // Shift down the rest
                        for (int j = MaxClosest - 1; j > i; j--)
                        {
                            mobs[j] = mobs[j - 1];
                            distances[j] = distances[j - 1];
                        }

                        mobs[i] = m;
                        distances[i] = distance;
                        break;
                    }
                }
            }

            if (total < MaxClosest)
            {
                Array.Resize(ref mobs, total);
            }

            return mobs;
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

        private static bool IsValidMobileType(Mobile m, int type) =>
            type switch
            {
                0 => !m.Player && m.Body.IsAnimal,
                1 => !m.Player && m.Body.IsMonster,
                2 => !m.Player && m.Body.IsHuman,
                _ => m.Player
            };

        public override void OnResponse(NetState state, RelayInfo info)
        {
            var index = info.ButtonID - 1;

            if (index >= 0 && index < _targets.Length && index < 12)
            {
                var m = _targets[index];

                _from.QuestArrow = new TrackArrow(_from, m, _range * 2);

                if (Core.SE)
                {
                    Tracking.AddInfo(_from, m);
                }
            }
        }
    }
}
