using System;
using System.Collections.Generic;
using Server.Mobiles;

namespace Server.Items
{
    [TypeAlias("Server.Items.AcidSlime")]
    public class PoolOfAcid : Item
    {
        private readonly DateTime m_Created;
        private readonly TimeSpan m_Duration;
        private readonly int m_MaxDamage;
        private readonly int m_MinDamage;
        private TimerExecutionToken _timerToken;
        private bool m_Drying;

        [Constructible]
        public PoolOfAcid() : this(TimeSpan.FromSeconds(10.0), 2, 5)
        {
        }

        [Constructible]
        public PoolOfAcid(TimeSpan duration, int minDamage, int maxDamage)
            : base(0x122A)
        {
            Hue = 0x3F;
            Movable = false;

            m_MinDamage = minDamage;
            m_MaxDamage = maxDamage;
            m_Created = Core.Now;
            m_Duration = duration;

            Timer.StartTimer(TimeSpan.Zero, TimeSpan.FromSeconds(1), OnTick, out _timerToken);
        }

        public PoolOfAcid(Serial serial) : base(serial)
        {
        }

        public override string DefaultName => "a pool of acid";

        public override void OnDelete()
        {
            _timerToken.Cancel();
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
            m.Damage(Utility.RandomMinMax(m_MinDamage, m_MaxDamage));
        }

        public override void Serialize(IGenericWriter writer)
        {
            // Don't serialize these
        }

        public override void Deserialize(IGenericReader reader)
        {
        }
    }
}
