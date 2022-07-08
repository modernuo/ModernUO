using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0)]
    public partial class ElvenLoveseatSouthAddon : BaseAddon
    {
        [Constructible]
        public ElvenLoveseatSouthAddon()
        {
            AddComponent(new AddonComponent(0x3088), 0, 0, 0);
            AddComponent(new AddonComponent(0x3089), -1, 0, 0);
        }

        public override BaseAddonDeed Deed => new ElvenLoveseatSouthDeed();
    }

    [SerializationGenerator(0)]
    public partial class ElvenLoveseatSouthDeed : BaseAddonDeed
    {
        [Constructible]
        public ElvenLoveseatSouthDeed()
        {
        }

        public override BaseAddon Addon => new ElvenLoveseatSouthAddon();
        public override int LabelNumber => 1072867; // elven loveseat (south)
    }
}
