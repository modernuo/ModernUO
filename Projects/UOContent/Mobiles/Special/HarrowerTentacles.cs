using System;

namespace Server.Mobiles
{
    public class HarrowerTentacles : BaseCreature
    {
        private DrainTimer m_Timer;

        [Constructible]
        public HarrowerTentacles(Mobile harrower = null) : base(AIType.AI_Melee)
        {
            Harrower = harrower;
            Body = 129;

            SetStr(901, 1000);
            SetDex(126, 140);
            SetInt(1001, 1200);

            SetHits(541, 600);

            SetDamage(13, 20);

            SetDamageType(ResistanceType.Physical, 20);
            SetDamageType(ResistanceType.Fire, 20);
            SetDamageType(ResistanceType.Cold, 20);
            SetDamageType(ResistanceType.Poison, 20);
            SetDamageType(ResistanceType.Energy, 20);

            SetResistance(ResistanceType.Physical, 55, 65);
            SetResistance(ResistanceType.Fire, 35, 45);
            SetResistance(ResistanceType.Cold, 35, 45);
            SetResistance(ResistanceType.Poison, 35, 45);
            SetResistance(ResistanceType.Energy, 35, 45);

            SetSkill(SkillName.Meditation, 100.0);
            SetSkill(SkillName.MagicResist, 120.1, 140.0);
            SetSkill(SkillName.Swords, 90.1, 100.0);
            SetSkill(SkillName.Tactics, 90.1, 100.0);
            SetSkill(SkillName.Wrestling, 90.1, 100.0);

            Fame = 15000;
            Karma = -15000;

            VirtualArmor = 60;

            m_Timer = new DrainTimer(this);
            m_Timer.Start();

            PackReg(50);
            PackNecroReg(15, 75);
        }

        public HarrowerTentacles(Serial serial) : base(serial)
        {
        }

        public override string CorpseName => "a tentacles corpse";

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Harrower { get; set; }

        public override string DefaultName => "tentacles of the harrower";

        public override bool AutoDispel => true;
        public override bool Unprovokable => true;
        public override Poison PoisonImmune => Poison.Lethal;
        public override bool DisallowAllMoves => true;

        public override void CheckReflect(Mobile caster, ref bool reflect)
        {
            reflect = true;
        }

        public override int GetIdleSound() => 0x101;

        public override int GetAngerSound() => 0x5E;

        public override int GetDeathSound() => 0x1C2;

        public override int GetAttackSound() => -1;

        public override int GetHurtSound() => 0x289;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.FilthyRich, 2);
            AddLoot(LootPack.MedScrolls, 3);
            AddLoot(LootPack.HighScrolls, 2);
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version

            writer.Write(Harrower);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        Harrower = reader.ReadEntity<Mobile>();

                        m_Timer = new DrainTimer(this);
                        m_Timer.Start();

                        break;
                    }
            }
        }

        public override void OnAfterDelete()
        {
            m_Timer?.Stop();
            m_Timer = null;

            base.OnAfterDelete();
        }

        private class DrainTimer : Timer
        {
            private readonly HarrowerTentacles m_Owner;

            public DrainTimer(HarrowerTentacles owner) : base(TimeSpan.FromSeconds(5.0), TimeSpan.FromSeconds(5.0))
            {
                m_Owner = owner;
            }

            protected override void OnTick()
            {
                if (m_Owner.Deleted)
                {
                    Stop();
                    return;
                }

                var eable = m_Owner.GetMobilesInRange<Mobile>(9);

                foreach (var m in eable)
                {
                    if (m == m_Owner || !m_Owner.CanBeHarmful(m))
                    {
                        continue;
                    }

                    if (m.Player && !m.Alive)
                    {
                        continue;
                    }

                    if (m is BaseCreature bc && !(bc.Controlled || bc.IsAnimatedDead || bc.Summoned || bc.Team != m_Owner.Team))
                    {
                        continue;
                    }

                    m_Owner.DoHarmful(m);

                    m.FixedParticles(0x374A, 10, 15, 5013, 0x455, 0, EffectLayer.Waist);
                    m.PlaySound(0x1F1);

                    var drain = Utility.RandomMinMax(14, 30);

                    m_Owner.Hits += drain;

                    if (m_Owner.Harrower != null)
                    {
                        m_Owner.Harrower.Hits += drain;
                    }

                    m.Damage(drain, m_Owner);
                }

                eable.Free();
            }
        }
    }
}
