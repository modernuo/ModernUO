using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0)]
    public partial class ElvenWashBasinEastAddon : BaseAddonContainer
    {
        [Constructible]
        public ElvenWashBasinEastAddon() : base(0x30E0)
        {
            AddComponent(new AddonContainerComponent(0x30DF), 0, -1, 0);
        }

        public override BaseAddonContainerDeed Deed => new ElvenWashBasinEastDeed();
        public override bool RetainDeedHue => true;
        public override int DefaultGumpID => 0x0104;
        public override int DefaultDropSound => 0x0042;
    }

    [SerializationGenerator(0)]
    public partial class ElvenWashBasinEastDeed : BaseAddonContainerDeed
    {
        [Constructible]
        public ElvenWashBasinEastDeed()
        {
        }

        public override BaseAddonContainer Addon => new ElvenWashBasinEastAddon();
        public override int LabelNumber => 1073387; // elven wash basin (east)
    }
}
