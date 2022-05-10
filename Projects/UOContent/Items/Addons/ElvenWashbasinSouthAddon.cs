using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0)]
    public partial class ElvenWashBasinSouthAddon : BaseAddonContainer
    {
        [Constructible]
        public ElvenWashBasinSouthAddon() : base(0x30E2)
        {
            AddComponent(new AddonContainerComponent(0x30E1), -1, 0, 0);
        }

        public override BaseAddonContainerDeed Deed => new ElvenWashBasinSouthDeed();
        public override bool RetainDeedHue => true;
        public override int DefaultGumpID => 0x0104;
        public override int DefaultDropSound => 0x0042;
    }

    [SerializationGenerator(0)]
    public partial class ElvenWashBasinSouthDeed : BaseAddonContainerDeed
    {
        [Constructible]
        public ElvenWashBasinSouthDeed()
        {
        }

        public override BaseAddonContainer Addon => new ElvenWashBasinSouthAddon();
        public override int LabelNumber => 1072865; // elven wash basin (south)
    }
}
