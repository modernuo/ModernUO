using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class AnvilSouthAddon : BaseAddon
    {
        [Constructible]
        public AnvilSouthAddon()
        {
            AddComponent(new AnvilComponent(0xFB0), 0, 0, 0);
        }

        public override BaseAddonDeed Deed => new AnvilSouthDeed();
    }

    [SerializationGenerator(0, false)]
    public partial class AnvilSouthDeed : BaseAddonDeed
    {
        [Constructible]
        public AnvilSouthDeed()
        {
        }

        public override BaseAddon Addon => new AnvilSouthAddon();
        public override int LabelNumber => 1044334; // anvil (south)
    }
}
