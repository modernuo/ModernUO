using System;

namespace Server.Engines.Harvest
{
	public class HarvestSoundTimer : Timer
	{
		private Mobile m_From;
		private Item m_Tool;
		private HarvestSystem m_System;
		private HarvestDefinition m_Definition;
		private object m_ToHarvest, m_Locked;
		private bool m_Last;

		public HarvestSoundTimer( Mobile from, Item tool, HarvestSystem system, HarvestDefinition def, object toHarvest, object locked, bool last ) : base( def.EffectSoundDelay )
		{
			m_From = from;
			m_Tool = tool;
			m_System = system;
			m_Definition = def;
			m_ToHarvest = toHarvest;
			m_Locked = locked;
			m_Last = last;
		}

		protected override void OnTick()
		{
			m_System.DoHarvestingSound( m_From, m_Tool, m_Definition, m_ToHarvest );

			if ( m_Last )
				m_System.FinishHarvesting( m_From, m_Tool, m_Definition, m_ToHarvest, m_Locked );
		}
	}
}