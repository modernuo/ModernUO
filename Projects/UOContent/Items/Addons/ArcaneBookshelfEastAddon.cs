using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0)]
    public partial class ArcaneBookshelfEastAddon : BaseAddonContainer
    {
        [Constructible]
        public ArcaneBookshelfEastAddon() : base(0x3084)
        {
            AddComponent(new AddonContainerComponent(0x3085), -1, 0, 0);
        }

        public override BaseAddonContainerDeed Deed => new ArcaneBookshelfEastDeed();
        public override bool RetainDeedHue => true;
        public override int DefaultGumpID => 0x107;
        public override int DefaultDropSound => 0x42;
    }

    [SerializationGenerator(0)]
    public partial class ArcaneBookshelfEastDeed : BaseAddonContainerDeed
    {
        [Constructible]
        public ArcaneBookshelfEastDeed()
        {
        }

        public override BaseAddonContainer Addon => new ArcaneBookshelfEastAddon();
        public override int LabelNumber => 1073371; // arcane bookshelf (east)
    }
}
