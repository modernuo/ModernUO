using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class StoneOvenSouthAddon : BaseAddon
    {
        [Constructible]
        public StoneOvenSouthAddon()
        {
            AddComponent(new AddonComponent(0x931), -1, 0, 0);
            AddComponent(new AddonComponent(0x930), 0, 0, 0);
        }

        public override BaseAddonDeed Deed => new StoneOvenSouthDeed();
    }

    [SerializationGenerator(0, false)]
    public partial class StoneOvenSouthDeed : BaseAddonDeed
    {
        [Constructible]
        public StoneOvenSouthDeed()
        {
        }

        public override BaseAddon Addon => new StoneOvenSouthAddon();
        public override int LabelNumber => 1044346; // stone oven (south)
    }
}
