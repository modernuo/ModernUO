using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0)]
    public partial class ElvenBedEastAddon : BaseAddon
    {
        [Constructible]
        public ElvenBedEastAddon()
        {
            AddComponent(new AddonComponent(0x304D), 0, 0, 0);
            AddComponent(new AddonComponent(0x304C), 1, 0, 0);
        }

        public override BaseAddonDeed Deed => new ElvenBedEastDeed();
    }

    [SerializationGenerator(0)]
    public partial class ElvenBedEastDeed : BaseAddonDeed
    {
        [Constructible]
        public ElvenBedEastDeed()
        {
        }

        public override BaseAddon Addon => new ElvenBedEastAddon();
        public override int LabelNumber => 1072861; // elven bed (east)
    }
}
