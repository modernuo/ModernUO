using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class LargeBedSouthAddon : BaseAddon
    {
        [Constructible]
        public LargeBedSouthAddon()
        {
            AddComponent(new AddonComponent(0xA83), 0, 0, 0);
            AddComponent(new AddonComponent(0xA7F), 0, 1, 0);
            AddComponent(new AddonComponent(0xA82), 1, 0, 0);
            AddComponent(new AddonComponent(0xA7E), 1, 1, 0);
        }

        public override BaseAddonDeed Deed => new LargeBedSouthDeed();
    }

    [SerializationGenerator(0, false)]
    public partial class LargeBedSouthDeed : BaseAddonDeed
    {
        [Constructible]
        public LargeBedSouthDeed()
        {
        }

        public override BaseAddon Addon => new LargeBedSouthAddon();
        public override int LabelNumber => 1044323; // large bed (south)
    }
}
