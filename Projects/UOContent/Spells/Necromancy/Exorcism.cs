using System;
using Server.Collections;
using Server.Engines.CannedEvil;
using Server.Engines.PartySystem;
using Server.Factions;
using Server.Guilds;
using Server.Items;
using Server.Regions;

namespace Server.Spells.Necromancy
{
    public class ExorcismSpell : NecromancerSpell
    {
        private static readonly SpellInfo _info = new(
            "Exorcism",
            "Ort Corp Grav",
            203,
            9031,
            Reagent.NoxCrystal,
            Reagent.GraveDust
        );

        private static readonly int Range = Core.ML ? 48 : 18;

        private static readonly Point3D[] m_BritanniaLocs =
        {
            new(1470, 843, 0),
            new(1857, 865, -1),
            new(4220, 563, 36),
            new(1732, 3528, 0),
            new(1300, 644, 8),
            new(3355, 302, 9),
            new(1606, 2490, 5),
            new(2500, 3931, 3),
            new(4264, 3707, 0)
        };

        private static readonly Point3D[] m_IllshLocs =
        {
            new(1222, 474, -17),
            new(718, 1360, -60),
            new(297, 1014, -19),
            new(986, 1006, -36),
            new(1180, 1288, -30),
            new(1538, 1341, -3),
            new(528, 223, -38)
        };

        private static readonly Point3D[] m_MalasLocs =
        {
            new(976, 517, -30)
        };

        private static readonly Point3D[] m_TokunoLocs =
        {
            new(710, 1162, 25),
            new(1034, 515, 18),
            new(295, 712, 55)
        };

        public ExorcismSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
        {
        }

        public override TimeSpan CastDelayBase => TimeSpan.FromSeconds(2.0);

        public override double RequiredSkill => 80.0;
        public override int RequiredMana => 40;

        public override bool DelayedDamage => false;

        public override bool CheckCast()
        {
            if (Caster.Skills.SpiritSpeak.Value < 100.0)
            {
                Caster.SendLocalizedMessage(1072112); // You must have GM Spirit Speak to use this spell
                return false;
            }

            return base.CheckCast();
        }

        public override int ComputeKarmaAward() => 0;

        public override void OnCast()
        {
            var r = Caster.Region.GetRegion<ChampionSpawnRegion>();
            if (r == null || !Caster.InRange(r.Spawn, Range))
            {
                Caster.SendLocalizedMessage(1072111); // You are not in a valid exorcism region.
            }
            else if (CheckSequence())
            {
                var map = Caster.Map;

                if (map != null)
                {
                    // Cannot move a mobile while iterating mobiles in range, so use a queue
                    using var queue = PooledRefQueue<Mobile>.Create();
                    foreach (var m in r.Spawn.GetMobilesInRange(Range))
                    {
                        if (IsValidTarget(m))
                        {
                            queue.Enqueue(m);
                        }
                    }

                    while (queue.Count > 0)
                    {
                        var m = queue.Dequeue();

                        // Surprisingly, no sparkle type effects
                        m.Location = GetNearestShrine(m);
                    }
                }
            }

            FinishSequence();
        }

        private bool IsValidTarget(Mobile m)
        {
            if (!m.Player || m.Alive)
            {
                return false;
            }

            var c = m.Corpse as Corpse;
            var map = m.Map;

            if (c?.Deleted == false && map != null && c.Map == map)
            {
                if (SpellHelper.IsAnyT2A(map, c.Location) && SpellHelper.IsAnyT2A(map, m.Location))
                {
                    return false; // Same Map, both in T2A, ie, same 'sub server'.
                }

                if (m.Region.IsPartOf<DungeonRegion>() == Region.Find(c.Location, map).IsPartOf<DungeonRegion>())
                {
                    return false; // Same Map, both in Dungeon region OR They're both NOT in a dungeon region.
                }

                // Just an approximation cause RunUO doesn't divide up the world the same way OSI does ;p
            }

            if (Party.Get(m)?.Contains(Caster) == true)
            {
                return false;
            }

            if (m.Guild != null && Caster.Guild != null)
            {
                var mGuild = m.Guild as Guild;
                var cGuild = Caster.Guild as Guild;

                if (mGuild?.IsAlly(cGuild) == true || mGuild == cGuild)
                {
                    return false;
                }
            }

            var f = Faction.Find(m);

            return m.Map != Faction.Facet || f == null || f != Faction.Find(Caster);
        }

        private static Point3D GetNearestShrine(Mobile m)
        {
            var map = m.Map;

            Point3D[] locList;

            if (map == Map.Felucca || map == Map.Trammel)
            {
                locList = m_BritanniaLocs;
            }
            else if (map == Map.Ilshenar)
            {
                locList = m_IllshLocs;
            }
            else if (map == Map.Tokuno)
            {
                locList = m_TokunoLocs;
            }
            else if (map == Map.Malas)
            {
                locList = m_MalasLocs;
            }
            else
            {
                locList = Array.Empty<Point3D>();
            }

            var closest = Point3D.Zero;
            var minDist = double.MaxValue;

            for (var i = 0; i < locList.Length; i++)
            {
                var p = locList[i];

                var dist = m.GetDistanceToSqrt(p);
                if (minDist > dist)
                {
                    closest = p;
                    minDist = dist;
                }
            }

            return closest;
        }
    }
}
