using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0)]
    public partial class TallElvenBedSouthAddon : BaseAddon
    {
        [Constructible]
        public TallElvenBedSouthAddon()
        {
            AddComponent(new AddonComponent(0x3058), 0, 0, 0);  // angolo alto sx
            AddComponent(new AddonComponent(0x3057), -1, 1, 0); // angolo basso sx
            AddComponent(new AddonComponent(0x3059), 0, -1, 0); // angolo alto dx
            AddComponent(new AddonComponent(0x3056), 0, 1, 0);  // angolo basso dx
        }

        public override BaseAddonDeed Deed => new TallElvenBedSouthDeed();
    }

    [SerializationGenerator(0)]
    public partial class TallElvenBedSouthDeed : BaseAddonDeed
    {
        [Constructible]
        public TallElvenBedSouthDeed()
        {
        }

        public override BaseAddon Addon => new TallElvenBedSouthAddon();
        public override int LabelNumber => 1072858; // tall elven bed (south)
    }
}
