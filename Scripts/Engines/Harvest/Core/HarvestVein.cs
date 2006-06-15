using System;

namespace Server.Engines.Harvest
{
	public class HarvestVein
	{
		private double m_VeinChance;
		private double m_ChanceToFallback;
		private HarvestResource m_PrimaryResource;
		private HarvestResource m_FallbackResource;

		public double VeinChance{ get{ return m_VeinChance; } set{ m_VeinChance = value; } }
		public double ChanceToFallback{ get{ return m_ChanceToFallback; } set{ m_ChanceToFallback = value; } }
		public HarvestResource PrimaryResource{ get{ return m_PrimaryResource; } set{ m_PrimaryResource = value; } }
		public HarvestResource FallbackResource{ get{ return m_FallbackResource; } set{ m_FallbackResource = value; } }

		public HarvestVein( double veinChance, double chanceToFallback, HarvestResource primaryResource, HarvestResource fallbackResource )
		{
			m_VeinChance = veinChance;
			m_ChanceToFallback = chanceToFallback;
			m_PrimaryResource = primaryResource;
			m_FallbackResource = fallbackResource;
		}
	}
}