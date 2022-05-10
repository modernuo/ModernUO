using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0)]
    public partial class ArcaneBookshelfSouthAddon : BaseAddonContainer
    {
        [Constructible]
        public ArcaneBookshelfSouthAddon() : base(0x2DEF)
        {
            AddComponent(new LocalizedContainerComponent(0x21A4, 1031760), -1, 0, 0);
        }

        public override BaseAddonContainerDeed Deed => new ArcaneBookshelfSouthDeed();
        public override bool RetainDeedHue => true;
        public override int DefaultGumpID => 0x107;
        public override int DefaultDropSound => 0x42;
        public override int LabelNumber => 1032420; // arcane bookshelf
    }

    [SerializationGenerator(0)]
    public partial class ArcaneBookshelfSouthDeed : BaseAddonContainerDeed
    {
        [Constructible]
        public ArcaneBookshelfSouthDeed()
        {
        }

        public override BaseAddonContainer Addon => new ArcaneBookshelfSouthAddon();
        public override int LabelNumber => 1072871; // arcane bookshelf (south)
    }
}
