using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class LargeForgeEastAddon : BaseAddon
    {
        [Constructible]
        public LargeForgeEastAddon()
        {
            AddComponent(new ForgeComponent(0x1986), 0, 0, 0);
            AddComponent(new ForgeComponent(0x198A), 0, 1, 0);
            AddComponent(new ForgeComponent(0x1996), 0, 2, 0);
            AddComponent(new ForgeComponent(0x1992), 0, 3, 0);
        }

        public override BaseAddonDeed Deed => new LargeForgeEastDeed();
    }

    [SerializationGenerator(0, false)]
    public partial class LargeForgeEastDeed : BaseAddonDeed
    {
        [Constructible]
        public LargeForgeEastDeed()
        {
        }

        public override BaseAddon Addon => new LargeForgeEastAddon();
        public override int LabelNumber => 1044331; // large forge (east)
    }
}
