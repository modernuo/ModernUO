using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0)]
    public partial class ElvenBedSouthAddon : BaseAddon
    {
        [Constructible]
        public ElvenBedSouthAddon()
        {
            AddComponent(new AddonComponent(0x3050), 0, 0, 0);
            AddComponent(new AddonComponent(0x3051), 0, -1, 0);
        }

        public override BaseAddonDeed Deed => new ElvenBedSouthDeed();
    }

    [SerializationGenerator(0)]
    public partial class ElvenBedSouthDeed : BaseAddonDeed
    {
        [Constructible]
        public ElvenBedSouthDeed()
        {
        }

        public override BaseAddon Addon => new ElvenBedSouthAddon();
        public override int LabelNumber => 1072860; // elven bed (south)
    }
}
