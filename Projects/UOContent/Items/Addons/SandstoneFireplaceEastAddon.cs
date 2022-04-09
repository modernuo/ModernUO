using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class SandstoneFireplaceEastAddon : BaseAddon
    {
        [Constructible]
        public SandstoneFireplaceEastAddon()
        {
            AddComponent(new AddonComponent(0x489), 0, 0, 0);
            AddComponent(new AddonComponent(0x475), 0, 1, 0);
        }

        public override BaseAddonDeed Deed => new SandstoneFireplaceEastDeed();
    }

    [SerializationGenerator(0, false)]
    public partial class SandstoneFireplaceEastDeed : BaseAddonDeed
    {
        [Constructible]
        public SandstoneFireplaceEastDeed()
        {
        }

        public override BaseAddon Addon => new SandstoneFireplaceEastAddon();
        public override int LabelNumber => 1061844; // sandstone fireplace (east)
    }
}
