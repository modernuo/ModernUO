using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class LargeBedEastAddon : BaseAddon
    {
        [Constructible]
        public LargeBedEastAddon()
        {
            AddComponent(new AddonComponent(0xA7D), 0, 0, 0);
            AddComponent(new AddonComponent(0xA7C), 0, 1, 0);
            AddComponent(new AddonComponent(0xA79), 1, 0, 0);
            AddComponent(new AddonComponent(0xA78), 1, 1, 0);
        }

        public override BaseAddonDeed Deed => new LargeBedEastDeed();
    }

    [SerializationGenerator(0, false)]
    public partial class LargeBedEastDeed : BaseAddonDeed
    {
        [Constructible]
        public LargeBedEastDeed()
        {
        }

        public override BaseAddon Addon => new LargeBedEastAddon();
        public override int LabelNumber => 1044324; // large bed (east)
    }
}
