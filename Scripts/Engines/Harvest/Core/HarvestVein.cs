namespace Server.Engines.Harvest
{
	public class HarvestVein
	{
		private double m_VeinChance;
		private double m_ChanceToFallback;
		private HarvestResource m_PrimaryResource;
		private HarvestResource m_FallbackResource;

		public double VeinChance{ get => m_VeinChance;
			set => m_VeinChance = value;
		}
		public double ChanceToFallback{ get => m_ChanceToFallback;
			set => m_ChanceToFallback = value;
		}
		public HarvestResource PrimaryResource{ get => m_PrimaryResource;
			set => m_PrimaryResource = value;
		}
		public HarvestResource FallbackResource{ get => m_FallbackResource;
			set => m_FallbackResource = value;
		}

		public HarvestVein( double veinChance, double chanceToFallback, HarvestResource primaryResource, HarvestResource fallbackResource )
		{
			m_VeinChance = veinChance;
			m_ChanceToFallback = chanceToFallback;
			m_PrimaryResource = primaryResource;
			m_FallbackResource = fallbackResource;
		}
	}
}