using System;

namespace Server.Engines.Harvest
{
	public class HarvestTimer : Timer
	{
		private Mobile m_From;
		private Item m_Tool;
		private HarvestSystem m_System;
		private HarvestDefinition m_Definition;
		private object m_ToHarvest, m_Locked;
		private int m_Index, m_Count;

		public HarvestTimer( Mobile from, Item tool, HarvestSystem system, HarvestDefinition def, object toHarvest, object locked ) : base( TimeSpan.Zero, def.EffectDelay )
		{
			m_From = from;
			m_Tool = tool;
			m_System = system;
			m_Definition = def;
			m_ToHarvest = toHarvest;
			m_Locked = locked;
			m_Count = Utility.RandomList( def.EffectCounts );
		}

		protected override void OnTick()
		{
			if ( !m_System.OnHarvesting( m_From, m_Tool, m_Definition, m_ToHarvest, m_Locked, ++m_Index == m_Count ) )
				Stop();
		}
	}
}