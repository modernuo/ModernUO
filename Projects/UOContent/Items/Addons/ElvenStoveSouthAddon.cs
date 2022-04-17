using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0)]
    public partial class ElvenStoveSouthAddon : BaseAddon
    {
        [Constructible]
        public ElvenStoveSouthAddon()
        {
            AddComponent(new AddonComponent(0x2DDC), 0, 0, 0);
        }

        public override BaseAddonDeed Deed => new ElvenStoveSouthDeed();
    }

    [SerializationGenerator(0)]
    public partial class ElvenStoveSouthDeed : BaseAddonDeed
    {
        [Constructible]
        public ElvenStoveSouthDeed()
        {
        }

        public override BaseAddon Addon => new ElvenStoveSouthAddon();
        public override int LabelNumber => 1073394; // elven oven (south)
    }
}
