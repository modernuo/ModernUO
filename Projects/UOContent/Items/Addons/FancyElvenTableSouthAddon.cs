using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0)]
    public partial class FancyElvenTableSouthAddon : BaseAddon
    {
        [Constructible]
        public FancyElvenTableSouthAddon()
        {
            AddComponent(new AddonComponent(0x3095), 0, 1, 0);
            AddComponent(new AddonComponent(0x3096), 0, 0, 0);
            AddComponent(new AddonComponent(0x3097), 0, -1, 0);
        }

        public override BaseAddonDeed Deed => new FancyElvenTableSouthDeed();
    }

    [SerializationGenerator(0)]
    public partial class FancyElvenTableSouthDeed : BaseAddonDeed
    {
        [Constructible]
        public FancyElvenTableSouthDeed()
        {
        }

        public override BaseAddon Addon => new FancyElvenTableSouthAddon();
        public override int LabelNumber => 1073385; // hardwood table (south)
    }
}
