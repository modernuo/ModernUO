using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0)]
    public partial class TallElvenBedEastAddon : BaseAddon
    {
        [Constructible]
        public TallElvenBedEastAddon()
        {
            AddComponent(new AddonComponent(0x3054), 0, 0, 0);
            AddComponent(new AddonComponent(0x3053), 1, 0, 0);
            AddComponent(new AddonComponent(0x3055), 2, -1, 0);
            AddComponent(new AddonComponent(0x3052), 2, 0, 0);
        }

        public override BaseAddonDeed Deed => new TallElvenBedEastDeed();
    }

    [SerializationGenerator(0)]
    public partial class TallElvenBedEastDeed : BaseAddonDeed
    {
        [Constructible]
        public TallElvenBedEastDeed()
        {
        }

        public override BaseAddon Addon => new TallElvenBedEastAddon();
        public override int LabelNumber => 1072859; // tall elven bed (east)
    }
}
