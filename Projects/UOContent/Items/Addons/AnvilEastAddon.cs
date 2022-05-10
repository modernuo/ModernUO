using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class AnvilEastAddon : BaseAddon
    {
        [Constructible]
        public AnvilEastAddon()
        {
            AddComponent(new AnvilComponent(0xFAF), 0, 0, 0);
        }

        public override BaseAddonDeed Deed => new AnvilEastDeed();
    }

    [SerializationGenerator(0, false)]
    public partial class AnvilEastDeed : BaseAddonDeed
    {
        [Constructible]
        public AnvilEastDeed()
        {
        }

        public override BaseAddon Addon => new AnvilEastAddon();
        public override int LabelNumber => 1044333; // anvil (east)
    }
}
