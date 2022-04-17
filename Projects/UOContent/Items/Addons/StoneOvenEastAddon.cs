using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class StoneOvenEastAddon : BaseAddon
    {
        [Constructible]
        public StoneOvenEastAddon()
        {
            AddComponent(new AddonComponent(0x92C), 0, 0, 0);
            AddComponent(new AddonComponent(0x92B), 0, 1, 0);
        }

        public override BaseAddonDeed Deed => new StoneOvenEastDeed();
    }

    [SerializationGenerator(0, false)]
    public partial class StoneOvenEastDeed : BaseAddonDeed
    {
        [Constructible]
        public StoneOvenEastDeed()
        {
        }

        public override BaseAddon Addon => new StoneOvenEastAddon();
        public override int LabelNumber => 1044345; // stone oven (east)
    }
}
