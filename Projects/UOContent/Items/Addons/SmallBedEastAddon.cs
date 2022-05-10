using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class SmallBedEastAddon : BaseAddon
    {
        [Constructible]
        public SmallBedEastAddon()
        {
            AddComponent(new AddonComponent(0xA5D), 0, 0, 0);
            AddComponent(new AddonComponent(0xA62), 1, 0, 0);
        }

        public override BaseAddonDeed Deed => new SmallBedEastDeed();
    }

    [SerializationGenerator(0, false)]
    public partial class SmallBedEastDeed : BaseAddonDeed
    {
        [Constructible]
        public SmallBedEastDeed()
        {
        }

        public override BaseAddon Addon => new SmallBedEastAddon();
        public override int LabelNumber => 1044322; // small bed (east)
    }
}
