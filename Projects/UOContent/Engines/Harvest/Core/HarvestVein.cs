namespace Server.Engines.Harvest
{
    public class HarvestVein
    {
        public HarvestVein(
            uint veinChance, double chanceToFallback, HarvestResource primaryResource,
            HarvestResource fallbackResource
        )
        {
            VeinChance = veinChance;
            ChanceToFallback = chanceToFallback;
            PrimaryResource = primaryResource;
            FallbackResource = fallbackResource;
        }

        public uint VeinChance { get; set; }

        public double ChanceToFallback { get; set; }

        public HarvestResource PrimaryResource { get; set; }

        public HarvestResource FallbackResource { get; set; }
    }
}
