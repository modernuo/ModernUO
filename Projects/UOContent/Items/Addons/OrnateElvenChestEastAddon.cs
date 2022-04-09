using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0)]
    public partial class OrnateElvenChestEastAddon : BaseAddonContainer
    {
        [Constructible]
        public OrnateElvenChestEastAddon() : base(0x309A)
        {
            AddComponent(new AddonContainerComponent(0x309B), 0, -1, 0);
        }

        public override BaseAddonContainerDeed Deed => new OrnateElvenChestEastDeed();
        public override bool RetainDeedHue => true;
        public override int DefaultGumpID => 0x10C;
        public override int DefaultDropSound => 0x42;
    }

    [SerializationGenerator(0)]
    public partial class OrnateElvenChestEastDeed : BaseAddonContainerDeed
    {
        [Constructible]
        public OrnateElvenChestEastDeed()
        {
        }

        public override BaseAddonContainer Addon => new OrnateElvenChestEastAddon();
        public override int LabelNumber => 1073383; // ornate elven chest (east)
    }
}
