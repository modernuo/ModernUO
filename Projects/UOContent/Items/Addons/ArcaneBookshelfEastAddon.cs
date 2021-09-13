namespace Server.Items
{
    [Serializable(0)]
    public partial class ArcaneBookshelfEastAddon : BaseAddonContainer
    {
        [Constructible]
        public ArcaneBookshelfEastAddon() : base(0x2DF0)
        {
            AddComponent(new LocalizedContainerComponent(0x21A4, 1031760), 0, -1, 0);
        }

        public override BaseAddonContainerDeed Deed => new ArcaneBookshelfEastDeed();
        public override bool RetainDeedHue => true;
        public override int DefaultGumpID => 0x107;
        public override int DefaultDropSound => 0x42;
        public override int LabelNumber => 1032420; // arcane bookshelf
    }

    [Serializable(0)]
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
