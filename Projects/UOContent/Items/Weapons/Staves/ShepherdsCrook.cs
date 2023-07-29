using System;
using ModernUO.Serialization;
using Server.Engines.CannedEvil;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Items
{
    [Flippable(0xE81, 0xE82)]
    [SerializationGenerator(0, false)]
    public partial class ShepherdsCrook : BaseStaff
    {
        [Constructible]
        public ShepherdsCrook() : base(0xE81) => Weight = 4.0;

        public override WeaponAbility PrimaryAbility => WeaponAbility.CrushingBlow;
        public override WeaponAbility SecondaryAbility => WeaponAbility.Disarm;

        public override int AosStrengthReq => 20;
        public override int AosMinDamage => 13;
        public override int AosMaxDamage => 15;
        public override int AosSpeed => 40;
        public override float MlSpeed => 2.75f;

        public override int OldStrengthReq => 10;
        public override int OldMinDamage => 3;
        public override int OldMaxDamage => 12;
        public override int OldSpeed => 30;

        public override int InitMinHits => 31;
        public override int InitMaxHits => 50;

        public override void OnDoubleClick(Mobile from)
        {
            from.SendLocalizedMessage(502464); // Target the animal you wish to herd.
            from.Target = new HerdingTarget();
        }

        private class HerdingTarget : Target
        {
            private static readonly Type[] m_ChampTamables =
            {
                typeof(StrongMongbat), typeof(Imp), typeof(Scorpion), typeof(GiantSpider),
                typeof(Snake), typeof(LavaLizard), typeof(Drake), typeof(Dragon),
                typeof(Kirin), typeof(Unicorn), typeof(GiantRat), typeof(Slime),
                typeof(DireWolf), typeof(HellHound), typeof(DeathwatchBeetle),
                typeof(LesserHiryu), typeof(Hiryu)
            };

            public HerdingTarget() : base(10, false, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object targ)
            {
                if (targ is not BaseCreature bc)
                {
                    from.SendLocalizedMessage(502472); // You don't seem to be able to persuade that to move.
                    return;
                }

                if (!IsHerdable(bc))
                {
                    from.SendLocalizedMessage(502468); // That is not a herdable animal.
                    return;
                }

                if (bc.Controlled)
                {
                    bc.PrivateOverheadMessage(
                        MessageType.Regular,
                        0x3B2,
                        502467, // That animal looks tame already.
                        from.NetState
                    );
                }
                else
                {
                    from.SendLocalizedMessage(502475); // Click where you wish the animal to go.
                    from.Target = new InternalTarget(bc);
                }
            }

            private static bool IsHerdable(BaseCreature bc)
            {
                if (bc.IsParagon)
                {
                    return false;
                }

                if (bc.Tamable)
                {
                    return true;
                }

                var map = bc.Map;

                if (Region.Find(bc.Home, map) is ChampionSpawnRegion region)
                {
                    var spawn = region.Spawn;

                    if (spawn?.IsChampionSpawn(bc) == true)
                    {
                        var t = bc.GetType();

                        foreach (var type in m_ChampTamables)
                        {
                            if (type == t)
                            {
                                return true;
                            }
                        }
                    }
                }

                return false;
            }

            private class InternalTarget : Target
            {
                private readonly BaseCreature m_Creature;

                public InternalTarget(BaseCreature c) : base(10, true, TargetFlags.None) => m_Creature = c;

                protected override void OnTarget(Mobile from, object targ)
                {
                    if (targ is not IPoint2D p)
                    {
                        return;
                    }

                    var min = m_Creature.MinTameSkill - 30;
                    var max = m_Creature.MinTameSkill + 30 + Utility.Random(10);

                    if (max <= from.Skills.Herding.Value)
                    {
                        m_Creature.PrivateOverheadMessage(
                            MessageType.Regular,
                            0x3B2,
                            502471, // That wasn't even challenging.
                            from.NetState
                        );
                    }

                    if (from.CheckTargetSkill(SkillName.Herding, m_Creature, min, max))
                    {
                        m_Creature.TargetLocation = targ != from ? new Point2D(p) : p;
                        from.SendLocalizedMessage(502479); // The animal walks where it was instructed to.
                    }
                    else
                    {
                        from.SendLocalizedMessage(502472); // You don't seem to be able to persuade that to move.
                    }
                }
            }
        }
    }
}
