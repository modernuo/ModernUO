using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0)]
    public partial class OrnateElvenChestSouthAddon : BaseAddonContainer
    {
        [Constructible]
        public OrnateElvenChestSouthAddon() : base(0x3098)
        {
            AddComponent(new AddonContainerComponent(0x3099), -1, 0, 0);
        }

        public override BaseAddonContainerDeed Deed => new OrnateElvenChestSouthDeed();
        public override bool RetainDeedHue => true;
        public override int DefaultGumpID => 0x10C;
        public override int DefaultDropSound => 0x42;
    }

    [SerializationGenerator(0)]
    public partial class OrnateElvenChestSouthDeed : BaseAddonContainerDeed
    {
        [Constructible]
        public OrnateElvenChestSouthDeed()
        {
        }

        public override BaseAddonContainer Addon => new OrnateElvenChestSouthAddon();
        public override int LabelNumber => 1072862; // ornate elven chest (south)
    }
}
