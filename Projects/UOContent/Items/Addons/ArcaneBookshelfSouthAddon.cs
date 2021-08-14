namespace Server.Items
{
    [Serializable(0)]
    public partial class ArcaneBookshelfSouthAddon : BaseAddon
    {
        [Constructible]
        public ArcaneBookshelfSouthAddon()
        {
            AddComponent(new AddonComponent(0x3087), 0, 0, 0);
            AddComponent(new AddonComponent(0x3086), 0, 1, 0);
        }

        public override BaseAddonDeed Deed => new ArcaneBookshelfSouthDeed();
    }

    [Serializable(0)]
    public partial class ArcaneBookshelfSouthDeed : BaseAddonDeed
    {
        [Constructible]
        public ArcaneBookshelfSouthDeed()
        {
        }

        public override BaseAddon Addon => new ArcaneBookshelfSouthAddon();
        public override int LabelNumber => 1072871; // arcane bookshelf (south)
    }
}
