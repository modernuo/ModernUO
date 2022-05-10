using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class StoneFireplaceEastAddon : BaseAddon
    {
        [Constructible]
        public StoneFireplaceEastAddon()
        {
            AddComponent(new AddonComponent(0x959), 0, 0, 0);
            AddComponent(new AddonComponent(0x953), 0, 1, 0);
        }

        public override BaseAddonDeed Deed => new StoneFireplaceEastDeed();
    }

    [SerializationGenerator(0, false)]
    public partial class StoneFireplaceEastDeed : BaseAddonDeed
    {
        [Constructible]
        public StoneFireplaceEastDeed()
        {
        }

        public override BaseAddon Addon => new StoneFireplaceEastAddon();
        public override int LabelNumber => 1061848; // stone fireplace (east)
    }
}
