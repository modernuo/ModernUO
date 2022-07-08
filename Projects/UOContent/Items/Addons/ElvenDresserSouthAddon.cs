using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0)]
    public partial class ElvenDresserSouthAddon : BaseAddonContainer
    {
        [Constructible]
        public ElvenDresserSouthAddon() : base(0x30E6)
        {
            AddComponent(new AddonContainerComponent(0x30E5), -1, 0, 0);
        }

        public override BaseAddonContainerDeed Deed => new ElvenDresserSouthDeed();
        public override bool RetainDeedHue => true;
        public override int DefaultGumpID => 0x51;
        public override int DefaultDropSound => 0x42;
    }

    [SerializationGenerator(0)]
    public partial class ElvenDresserSouthDeed : BaseAddonContainerDeed
    {
        [Constructible]
        public ElvenDresserSouthDeed()
        {
        }

        public override BaseAddonContainer Addon => new ElvenDresserSouthAddon();
        public override int LabelNumber => 1072864; // elven dresser (south)
    }
}
