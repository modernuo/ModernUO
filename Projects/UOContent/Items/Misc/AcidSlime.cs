using System;
using System.Collections.Generic;
using Server.Mobiles;

namespace Server.Items
{
    public class AcidSlime : Item
    {
        private readonly DateTime m_Created;
        private readonly TimeSpan m_Duration;
        private readonly int m_MaxDamage;
        private readonly int m_MinDamage;
        private readonly Timer m_Timer;
        private bool m_Drying;

        [Constructible]
        public AcidSlime() : this(TimeSpan.FromSeconds(10.0), 5, 10)
        {
        }

        [Constructible]
        public AcidSlime(TimeSpan duration, int minDamage, int maxDamage)
            : base(0x122A)
        {
            Hue = 0x3F;
            Movable = false;
            m_MinDamage = minDamage;
            m_MaxDamage = maxDamage;
            m_Created = Core.Now;
            m_Duration = duration;
            m_Timer = Timer.DelayCall(TimeSpan.Zero, TimeSpan.FromSeconds(1), OnTick);
        }

        public AcidSlime(Serial serial) : base(serial)
        {
        }

        public override string DefaultName => "slime";

        public override void OnAfterDelete()
        {
            m_Timer?.Stop();
        }

        private void OnTick()
        {
            var now = Core.Now;
            var age = now - m_Created;

            if (age > m_Duration)
            {
                Delete();
            }
            else
            {
                if (!m_Drying && age > m_Duration - age)
                {
                    m_Drying = true;
                    ItemID = 0x122B;
                }

                var toDamage = new List<Mobile>();

                foreach (var m in GetMobilesInRange(0))
                {
                    if (m.Alive && !m.IsDeadBondedPet && (!(m is BaseCreature bc) || bc.Controlled || bc.Summoned))
                    {
                        toDamage.Add(m);
                    }
                }

                for (var i = 0; i < toDamage.Count; i++)
                {
                    Damage(toDamage[i]);
                }
            }
        }

        public override bool OnMoveOver(Mobile m)
        {
            Damage(m);
            return true;
        }

        public void Damage(Mobile m)
        {
            var damage = Utility.RandomMinMax(m_MinDamage, m_MaxDamage);
            if (Core.AOS)
            {
                AOS.Damage(m, damage, 0, 0, 0, 100, 0);
            }
            else
            {
                m.Damage(damage);
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
        }

        public override void Deserialize(IGenericReader reader)
        {
        }
    }
}
