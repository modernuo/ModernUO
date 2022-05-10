using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0)]
    public partial class FancyElvenTableEastAddon : BaseAddon
    {
        [Constructible]
        public FancyElvenTableEastAddon()
        {
            AddComponent(new AddonComponent(0x3094), -1, 0, 0);
            AddComponent(new AddonComponent(0x3093), 0, 0, 0);
            AddComponent(new AddonComponent(0x3092), 1, 0, 0);
        }

        public override BaseAddonDeed Deed => new FancyElvenTableEastDeed();
    }

    [SerializationGenerator(0)]
    public partial class FancyElvenTableEastDeed : BaseAddonDeed
    {
        [Constructible]
        public FancyElvenTableEastDeed()
        {
        }

        public override BaseAddon Addon => new FancyElvenTableEastAddon();
        public override int LabelNumber => 1073386; // hardwood table (east)
    }
}
