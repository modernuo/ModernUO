using System;
using System.Collections.Generic;
using Server.Engines.CannedEvil;
using Server.Items;

namespace Server.Mobiles
{
    public abstract class BaseChampion : BaseCreature
    {
        public BaseChampion(AIType aiType, FightMode mode = FightMode.Closest) : base(aiType, mode, 18)
        {
        }

        public BaseChampion(Serial serial) : base(serial)
        {
        }

        public override bool CanMoveOverObstacles => true;
        public override bool CanDestroyObstacles => true;

        public abstract ChampionSkullType SkullType { get; }

        public abstract Type[] UniqueList { get; }
        public abstract Type[] SharedList { get; }
        public abstract Type[] DecorativeList { get; }
        public abstract MonsterStatuetteType[] StatueTypes { get; }

        public virtual bool NoGoodies => false;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }

        public Item GetArtifact() =>
            Utility.RandomDouble() switch
            {
                < 0.05 => CreateArtifact(UniqueList),
                < 0.15 => CreateArtifact(SharedList),
                < 0.30 => CreateArtifact(DecorativeList),
                _      => null
            };

        public Item CreateArtifact(Type[] list)
        {
            if (list.Length == 0)
            {
                return null;
            }

            var type = list.RandomElement();

            var artifact = Loot.Construct(type);

            if (StatueTypes.Length > 0 && artifact is MonsterStatuette statuette)
            {
                statuette.Type = StatueTypes.RandomElement();
                statuette.LootType = LootType.Regular;
            }

            return artifact;
        }

        private static PowerScroll CreateRandomPowerScroll()
        {
            var level = Utility.RandomDouble() switch
            {
                < 0.05 => 20,
                < 0.4  => 15,
                _       => 10
            };

            return PowerScroll.CreateRandomNoCraft(level, level);
        }

        public void GivePowerScrolls()
        {
            if (Map != Map.Felucca)
            {
                return;
            }

            var toGive = new List<Mobile>();
            var rights = GetLootingRights(DamageEntries, HitsMax);

            for (var i = rights.Count - 1; i >= 0; --i)
            {
                var ds = rights[i];

                if (ds.m_HasRight)
                {
                    toGive.Add(ds.m_Mobile);
                }
            }

            if (toGive.Count == 0)
            {
                return;
            }

            for (var i = 0; i < toGive.Count; i++)
            {
                var m = toGive[i];

                if (m is not PlayerMobile)
                {
                    continue;
                }

                var gainedPath = false;

                var pointsToGain = 800;

                if (VirtueHelper.Award(m, VirtueName.Valor, pointsToGain, ref gainedPath))
                {
                    if (gainedPath)
                    {
                        m.SendLocalizedMessage(1054032); // You have gained a path in Valor!
                    }
                    else
                    {
                        m.SendLocalizedMessage(1054030); // You have gained in Valor!
                    }

                    // No delay on Valor gains
                }
            }

            // Randomize
            toGive.Shuffle();

            for (var i = 0; i < 6; ++i)
            {
                var m = toGive[i % toGive.Count];

                var ps = CreateRandomPowerScroll();

                GivePowerScrollTo(m, ps);
            }
        }

        public static void GivePowerScrollTo(Mobile m, PowerScroll ps)
        {
            if (ps == null || m == null) // sanity
            {
                return;
            }

            m.SendLocalizedMessage(1049524); // You have received a scroll of power!

            if (!Core.SE || m.Alive)
            {
                m.AddToBackpack(ps);
            }
            else
            {
                if (m.Corpse?.Deleted == false)
                {
                    m.Corpse.DropItem(ps);
                }
                else
                {
                    m.AddToBackpack(ps);
                }
            }

            if (m is not PlayerMobile pm)
            {
                return;
            }

            for (var j = 0; j < pm.JusticeProtectors.Count; ++j)
            {
                var prot = pm.JusticeProtectors[j];

                if (prot.Map != pm.Map || prot.Kills >= 5 || prot.Criminal || !JusticeVirtue.CheckMapRegion(pm, prot))
                {
                    continue;
                }

                var chance = VirtueHelper.GetLevel(prot, VirtueName.Justice) switch
                {
                    VirtueLevel.Seeker   => 60,
                    VirtueLevel.Follower => 80,
                    VirtueLevel.Knight   => 100,
                    _                    => 0
                };

                if (chance > Utility.Random(100))
                {
                    var powerScroll = new PowerScroll(ps.Skill, ps.Value);

                    prot.SendLocalizedMessage(1049368); // You have been rewarded for your dedication to Justice!

                    if (!Core.SE || prot.Alive)
                    {
                        prot.AddToBackpack(powerScroll);
                    }
                    else if (prot.Corpse?.Deleted == false)
                    {
                        prot.Corpse.DropItem(powerScroll);
                    }
                    else
                    {
                        prot.AddToBackpack(powerScroll);
                    }
                }
            }
        }

        public override bool OnBeforeDeath()
        {
            if (!NoKillAwards)
            {
                GivePowerScrolls();

                if (NoGoodies)
                {
                    return base.OnBeforeDeath();
                }

                var map = Map;

                if (map != null)
                {
                    for (var x = -12; x <= 12; ++x)
                    {
                        for (var y = -12; y <= 12; ++y)
                        {
                            var dist = Math.Sqrt(x * x + y * y);

                            if (dist <= 12)
                            {
                                new GoodiesTimer(map, X + x, Y + y).Start();
                            }
                        }
                    }
                }
            }

            return base.OnBeforeDeath();
        }

        public override void OnDeath(Container c)
        {
            if (Map == Map.Felucca)
            {
                // TODO: Confirm SE change or AoS one too?
                var rights = GetLootingRights(DamageEntries, HitsMax);
                var toGive = new List<Mobile>();

                for (var i = rights.Count - 1; i >= 0; --i)
                {
                    var ds = rights[i];

                    if (ds.m_HasRight)
                    {
                        toGive.Add(ds.m_Mobile);
                    }
                }

                if (toGive.Count > 0)
                {
                    toGive.RandomElement().AddToBackpack(new ChampionSkull(SkullType));
                }
                else
                {
                    c.DropItem(new ChampionSkull(SkullType));
                }
            }

            base.OnDeath(c);
        }

        private class GoodiesTimer : Timer
        {
            private readonly Map m_Map;
            private readonly int m_X;
            private readonly int m_Y;

            public GoodiesTimer(Map map, int x, int y) : base(TimeSpan.FromSeconds(Utility.RandomDouble() * 10.0))
            {
                m_Map = map;
                m_X = x;
                m_Y = y;
            }

            protected override void OnTick()
            {
                var z = m_Map.GetAverageZ(m_X, m_Y);
                var canFit = m_Map.CanFit(m_X, m_Y, z, 6, false, false);

                for (var i = -3; !canFit && i <= 3; ++i)
                {
                    canFit = m_Map.CanFit(m_X, m_Y, z + i, 6, false, false);

                    if (canFit)
                    {
                        z += i;
                    }
                }

                if (!canFit)
                {
                    return;
                }

                var g = new Gold(500, 1000);

                g.MoveToWorld(new Point3D(m_X, m_Y, z), m_Map);

                if (Utility.RandomDouble() < 0.05)
                {
                    return;
                }

                switch (Utility.Random(3))
                {
                    case 0: // Fire column
                        {
                            Effects.SendLocationParticles(
                                EffectItem.Create(g.Location, g.Map, EffectItem.DefaultDuration),
                                0x3709,
                                10,
                                30,
                                5052
                            );
                            Effects.PlaySound(g, 0x208);

                            break;
                        }
                    case 1: // Explosion
                        {
                            Effects.SendLocationParticles(
                                EffectItem.Create(g.Location, g.Map, EffectItem.DefaultDuration),
                                0x36BD,
                                20,
                                10,
                                5044
                            );
                            Effects.PlaySound(g, 0x307);

                            break;
                        }
                    case 2: // Ball of fire
                        {
                            Effects.SendLocationParticles(
                                EffectItem.Create(g.Location, g.Map, EffectItem.DefaultDuration),
                                0x36FE,
                                10,
                                10,
                                5052
                            );

                            break;
                        }
                }
            }
        }
    }
}
