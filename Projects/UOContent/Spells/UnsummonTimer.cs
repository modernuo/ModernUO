using System;
using Server.Mobiles;

namespace Server.Spells
{
    internal class UnsummonTimer : Timer
    {
        private readonly BaseCreature m_Creature;

        public UnsummonTimer(BaseCreature creature, TimeSpan delay) : base(delay)
        {
            m_Creature = creature;
        }

        protected override void OnTick()
        {
            if (!m_Creature.Deleted)
            {
                m_Creature.Delete();
            }
        }
    }
}
