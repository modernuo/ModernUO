using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0)]
    public partial class ElvenLoveseatEastAddon : BaseAddon
    {
        [Constructible]
        public ElvenLoveseatEastAddon()
        {
            AddComponent(new AddonComponent(0x308A), 0, 0, 0);
            AddComponent(new AddonComponent(0x308B), 0, -1, 0);
        }

        public override BaseAddonDeed Deed => new ElvenLoveseatEastDeed();
    }

    [SerializationGenerator(0)]
    public partial class ElvenLoveseatEastDeed : BaseAddonDeed
    {
        [Constructible]
        public ElvenLoveseatEastDeed()
        {
        }

        public override BaseAddon Addon => new ElvenLoveseatEastAddon();
        public override int LabelNumber => 1073372; // elven loveseat (east)
    }
}
