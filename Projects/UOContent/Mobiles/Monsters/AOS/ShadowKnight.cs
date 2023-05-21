using ModernUO.Serialization;
using System;
using Server.Items;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class ShadowKnight : BaseCreature
    {
        private bool m_HasTeleportedAway;

        private TimerExecutionToken _soundTimerToken;

        [Constructible]
        public ShadowKnight() : base(AIType.AI_Mage)
        {
            Name = NameList.RandomName("shadow knight");
            Title = "the Shadow Knight";
            Body = 311;

            SetStr(250);
            SetDex(100);
            SetInt(100);

            SetHits(2000);

            SetDamage(20, 30);

            SetDamageType(ResistanceType.Physical, 60);
            SetDamageType(ResistanceType.Cold, 40);

            SetResistance(ResistanceType.Physical, 90);
            SetResistance(ResistanceType.Fire, 65);
            SetResistance(ResistanceType.Cold, 75);
            SetResistance(ResistanceType.Poison, 75);
            SetResistance(ResistanceType.Energy, 55);

            SetSkill(SkillName.Chivalry, 120.0);
            SetSkill(SkillName.DetectHidden, 80.0);
            SetSkill(SkillName.EvalInt, 100.0);
            SetSkill(SkillName.Magery, 100.0);
            SetSkill(SkillName.Meditation, 100.0);
            SetSkill(SkillName.MagicResist, 120.0);
            SetSkill(SkillName.Tactics, 100.0);
            SetSkill(SkillName.Wrestling, 100.0);

            Fame = 25000;
            Karma = -25000;

            VirtualArmor = 54;
        }

        public override string CorpseName => "a shadow knight corpse";

        public override bool IgnoreYoungProtection => Core.ML;

        public override OppositionGroup OppositionGroup => OppositionGroup.FeyAndUndead;

        public override bool BardImmune => !Core.SE;
        public override bool Unprovokable => Core.SE;
        public override bool AreaPeaceImmune => Core.SE;
        public override Poison PoisonImmune => Poison.Lethal;

        public override int TreasureMapLevel => 1;

        public override WeaponAbility GetWeaponAbility() =>
            Utility.RandomBool() ? WeaponAbility.ConcussionBlow : WeaponAbility.CrushingBlow;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.UltraRich, 2);
        }

        public override void OnDeath(Container c)
        {
            base.OnDeath(c);

            if (!Summoned && !NoKillAwards && DemonKnight.CheckArtifactChance(this))
            {
                DemonKnight.DistributeArtifact(this);
            }
        }

        public override int GetIdleSound() => 0x2CE;

        public override int GetDeathSound() => 0x2C1;

        public override int GetHurtSound() => 0x2D1;

        public override int GetAttackSound() => 0x2C8;

        public override void OnCombatantChange()
        {
            base.OnCombatantChange();

            if (Hidden && Combatant != null)
            {
                Combatant = null;
            }
        }

        public virtual void SendTrackingSound()
        {
            if (Hidden)
            {
                Effects.PlaySound(Location, Map, 0x2C8);
                Combatant = null;
            }
            else
            {
                Frozen = false;
                _soundTimerToken.Cancel();
            }
        }

        public override void OnThink()
        {
            // TODO: Can the shadow knight teleport twice? What if it heals completely and enough time has passed?
            if (!m_HasTeleportedAway && Hits < HitsMax / 2)
            {
                var map = Map;

                if (map != null)
                {
                    for (var i = 0; i < 10; ++i)
                    {
                        var x = X + Utility.RandomMinMax(5, 10) * (Utility.RandomBool() ? 1 : -1);
                        var y = Y + Utility.RandomMinMax(5, 10) * (Utility.RandomBool() ? 1 : -1);
                        var z = Z;

                        if (!map.CanFit(x, y, z, 16, false, false))
                        {
                            continue;
                        }

                        var from = Location;
                        var to = new Point3D(x, y, z);

                        if (!InLOS(to))
                        {
                            continue;
                        }

                        Location = to;
                        ProcessDelta();
                        Hidden = true;
                        Combatant = null;

                        Effects.SendLocationParticles(
                            EffectItem.Create(from, map, EffectItem.DefaultDuration),
                            0x3728,
                            10,
                            10,
                            2023
                        );
                        Effects.SendLocationParticles(
                            EffectItem.Create(to, map, EffectItem.DefaultDuration),
                            0x3728,
                            10,
                            10,
                            5023
                        );

                        Effects.PlaySound(to, map, 0x1FE);

                        m_HasTeleportedAway = true;
                        Timer.StartTimer(
                            TimeSpan.FromSeconds(5.0),
                            TimeSpan.FromSeconds(2.5),
                            SendTrackingSound,
                            out _soundTimerToken
                        );

                        Frozen = true;

                        break;
                    }
                }
            }

            base.OnThink();
        }
    }
}
