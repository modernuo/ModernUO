using System;
using Server.Mobiles;

namespace Server.Spells
{
	class UnsummonTimer : Timer
	{
		private BaseCreature m_Creature;
		private Mobile m_Caster;

		public UnsummonTimer( Mobile caster, BaseCreature creature, TimeSpan delay ) : base( delay )
		{
			m_Caster = caster;
			m_Creature = creature;
			Priority = TimerPriority.OneSecond;
		}

		protected override void OnTick()
		{
			if ( !m_Creature.Deleted )
				m_Creature.Delete();
		}
	}
}