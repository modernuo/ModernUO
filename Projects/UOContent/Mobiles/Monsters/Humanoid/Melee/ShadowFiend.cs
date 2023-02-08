using System;
using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class ShadowFiend : BaseCreature
    {
        private UnhideTimer m_Timer;

        [Constructible]
        public ShadowFiend() : base(AIType.AI_Melee)
        {
            Body = 0xA8;

            // this to allow shadow fiend to loot from corpses
            var backpack = new Backpack();
            backpack.Movable = false;
            AddItem(backpack);

            SetStr(46, 55);
            SetDex(121, 130);
            SetInt(46, 55);

            SetHits(28, 33);
            SetStam(46, 55);

            SetDamage(10, 22);

            SetDamageType(ResistanceType.Physical, 20);
            SetDamageType(ResistanceType.Cold, 80);

            SetResistance(ResistanceType.Physical, 20, 25);
            SetResistance(ResistanceType.Fire, 20, 25);
            SetResistance(ResistanceType.Cold, 40, 45);
            SetResistance(ResistanceType.Poison, 60, 70);
            SetResistance(ResistanceType.Energy, 5, 10);

            SetSkill(SkillName.MagicResist, 20.1, 30.0);
            SetSkill(SkillName.Tactics, 20.1, 30.0);
            SetSkill(SkillName.Wrestling, 20.1, 30.0);

            Fame = 1000;
            Karma = -1000;

            m_Timer = new UnhideTimer(this);
            m_Timer.Start();
        }

        public override bool DeleteCorpseOnDeath => true;

        public override string DefaultName => "a shadow fiend";

        public override bool CanRummageCorpses => true;

        public override int GetIdleSound() => 0x37A;

        public override int GetAngerSound() => 0x379;

        public override int GetDeathSound() => 0x381;

        public override int GetAttackSound() => 0x37F;

        public override int GetHurtSound() => 0x380;

        public override bool OnBeforeDeath()
        {
            Backpack?.Destroy();

            Effects.SendLocationEffect(Location, Map, 0x376A, 10, 1);
            return true;
        }

        [AfterDeserialization]
        private void AfterDeserialization()
        {
            m_Timer = new UnhideTimer(this);
            m_Timer.Start();
        }

        public override void OnAfterDelete()
        {
            m_Timer?.Stop();

            m_Timer = null;

            base.OnAfterDelete();
        }

        private class UnhideTimer : Timer
        {
            private readonly ShadowFiend m_Owner;

            public UnhideTimer(ShadowFiend owner) : base(TimeSpan.FromSeconds(30.0))
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

                foreach (var m in m_Owner.GetMobilesInRange(3))
                {
                    if (m != m_Owner && m.Player && m.Hidden && m_Owner.CanBeHarmful(m) &&
                        m.AccessLevel == AccessLevel.Player)
                    {
                        m.Hidden = false;
                    }
                }
            }
        }
    }
}
