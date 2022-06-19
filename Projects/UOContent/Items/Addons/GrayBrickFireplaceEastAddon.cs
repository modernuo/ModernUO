using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class GrayBrickFireplaceEastAddon : BaseAddon
    {
        [Constructible]
        public GrayBrickFireplaceEastAddon()
        {
            AddComponent(new AddonComponent(0x93D), 0, 0, 0);
            AddComponent(new AddonComponent(0x937), 0, 1, 0);
        }

        public override BaseAddonDeed Deed => new GrayBrickFireplaceEastDeed();
    }

    [SerializationGenerator(0, false)]
    public partial class GrayBrickFireplaceEastDeed : BaseAddonDeed
    {
        [Constructible]
        public GrayBrickFireplaceEastDeed()
        {
        }

        public override BaseAddon Addon => new GrayBrickFireplaceEastAddon();
        public override int LabelNumber => 1061846; // grey brick fireplace (east)
    }
}
