using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0)]
    public partial class ElvenDresserEastAddon : BaseAddonContainer
    {
        [Constructible]
        public ElvenDresserEastAddon() : base(0x30E4)
        {
            AddComponent(new AddonContainerComponent(0x30E3), 0, -1, 0);
        }

        public override BaseAddonContainerDeed Deed => new ElvenDresserEastDeed();
        public override bool RetainDeedHue => true;
        public override int DefaultGumpID => 0x51;
        public override int DefaultDropSound => 0x42;
    }

    [SerializationGenerator(0)]
    public partial class ElvenDresserEastDeed : BaseAddonContainerDeed
    {
        [Constructible]
        public ElvenDresserEastDeed()
        {
        }

        public override BaseAddonContainer Addon => new ElvenDresserEastAddon();
        public override int LabelNumber => 1073388; // elven dresser (east)
    }
}
