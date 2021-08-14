namespace Server.Items
{
    [Serializable(0)]
    public partial class AlchemistTableEastAddon : BaseAddon
    {
        [Constructible]
        public AlchemistTableEastAddon()
        {
            AddComponent(new AddonComponent(0x2DD3), 0, 0, 0);
        }

        public override BaseAddonDeed Deed => new AlchemistTableEastDeed();
    }

    [Serializable(0)]
    public partial class AlchemistTableEastDeed : BaseAddonDeed
    {
        [Constructible]
        public AlchemistTableEastDeed()
        {
        }

        public override BaseAddon Addon => new AlchemistTableEastAddon();
        public override int LabelNumber => 1073397; // alchemist table (east)
    }
}
