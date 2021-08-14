namespace Server.Items
{
    [Serializable(0)]
    public partial class ArcaneBookshelfEastAddon : BaseAddon
    {
        [Constructible]
        public ArcaneBookshelfEastAddon()
        {
            AddComponent(new AddonComponent(0x3084), 0, 0, 0);
            AddComponent(new AddonComponent(0x3085), -1, 0, 0);
        }

        public override BaseAddonDeed Deed => new ArcaneBookshelfEastDeed();
    }

    [Serializable(0)]
    public partial class ArcaneBookshelfEastDeed : BaseAddonDeed
    {
        [Constructible]
        public ArcaneBookshelfEastDeed()
        {
        }

        public override BaseAddon Addon => new ArcaneBookshelfEastAddon();
        public override int LabelNumber => 1073371; // arcane bookshelf (east)
    }
}
