namespace Server.Items
{
    [Serializable(0)]
    public partial class ElvenDresserEastAddon : BaseAddon
    {
        [Constructible]
        public ElvenDresserEastAddon()
        {
            AddComponent(new AddonComponent(0x30E4), 0, 0, 0);
            AddComponent(new AddonComponent(0x30E3), 0, -1, 0);
        }

        public override BaseAddonDeed Deed => new ElvenDresserEastDeed();
    }

    [Serializable(0)]
    public partial class ElvenDresserEastDeed : BaseAddonDeed
    {
        [Constructible]
        public ElvenDresserEastDeed()
        {
        }

        public override BaseAddon Addon => new ElvenDresserEastAddon();
        public override int LabelNumber => 1073388; // elven dresser (east)
    }
}
