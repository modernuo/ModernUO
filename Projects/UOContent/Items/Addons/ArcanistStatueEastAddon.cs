using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0)]
    public partial class ArcanistStatueEastAddon : BaseAddon
    {
        [Constructible]
        public ArcanistStatueEastAddon()
        {
            AddComponent(new AddonComponent(0x2D0E), 0, 0, 0);
        }

        public override BaseAddonDeed Deed => new ArcanistStatueEastDeed();
    }

    [SerializationGenerator(0)]
    public partial class ArcanistStatueEastDeed : BaseAddonDeed
    {
        [Constructible]
        public ArcanistStatueEastDeed()
        {
        }

        public override BaseAddon Addon => new ArcanistStatueEastAddon();
        public override int LabelNumber => 1072886; // arcanist statue (east)
    }
}
