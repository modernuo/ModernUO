namespace Server.Engines.Harvest
{
	public class HarvestVein
	{
		public double VeinChance { get; set; }

		public double ChanceToFallback { get; set; }

		public HarvestResource PrimaryResource { get; set; }

		public HarvestResource FallbackResource { get; set; }

		public HarvestVein( double veinChance, double chanceToFallback, HarvestResource primaryResource, HarvestResource fallbackResource )
		{
			VeinChance = veinChance;
			ChanceToFallback = chanceToFallback;
			PrimaryResource = primaryResource;
			FallbackResource = fallbackResource;
		}
	}
}