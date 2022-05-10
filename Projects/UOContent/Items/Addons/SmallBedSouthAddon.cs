using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class SmallBedSouthAddon : BaseAddon
    {
        [Constructible]
        public SmallBedSouthAddon()
        {
            AddComponent(new AddonComponent(0xA63), 0, 0, 0);
            AddComponent(new AddonComponent(0xA5C), 0, 1, 0);
        }

        public override BaseAddonDeed Deed => new SmallBedSouthDeed();
    }

    [SerializationGenerator(0, false)]
    public partial class SmallBedSouthDeed : BaseAddonDeed
    {
        [Constructible]
        public SmallBedSouthDeed()
        {
        }

        public override BaseAddon Addon => new SmallBedSouthAddon();
        public override int LabelNumber => 1044321; // small bed (south)
    }
}
