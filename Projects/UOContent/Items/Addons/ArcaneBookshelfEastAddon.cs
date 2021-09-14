namespace Server.Items
{
    [Serializable(0)]
    public partial class ArcaneBookshelfEastAddon : BaseAddon
    public partial class ArcaneBookshelfEastAddon : BaseAddonContainer
    {
        [Constructible]
        public ArcaneBookshelfEastAddon()
        public ArcaneBookshelfEastAddon() : base(0x3084)
        {
            AddComponent(new AddonComponent(0x3084), 0, 0, 0);
            AddComponent(new AddonComponent(0x3085), -1, 0, 0);
            AddComponent(new AddonContainerComponent(0x3085), -1, 0, 0);
        }

        public override BaseAddonDeed Deed => new ArcaneBookshelfEastDeed();
        public override BaseAddonContainerDeed Deed => new ArcaneBookshelfEastDeed();
        public override bool RetainDeedHue => true;
        public override int DefaultGumpID => 0x107;
        public override int DefaultDropSound => 0x42;
    }

    [Serializable(0)]
    public partial class ArcaneBookshelfEastDeed : BaseAddonDeed
    public partial class ArcaneBookshelfEastDeed : BaseAddonContainerDeed
    {
        [Constructible]
        public ArcaneBookshelfEastDeed()
        {
        }

        public override BaseAddon Addon => new ArcaneBookshelfEastAddon();
        public override BaseAddonContainer Addon => new ArcaneBookshelfEastAddon();
        public override int LabelNumber => 1073371; // arcane bookshelf (east)
    }
}
