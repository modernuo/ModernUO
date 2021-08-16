namespace Server.Items
{
    [Serializable(0)]
    public partial class ElvenStoveEastAddon : BaseAddon
    {
        [Constructible]
        public ElvenStoveEastAddon()
        {
            AddComponent(new AddonComponent(0x2DDB), 0, 0, 0);
        }

        public override BaseAddonDeed Deed => new ElvenStoveEastDeed();
    }

    [Serializable(0)]
    public partial class ElvenStoveEastDeed : BaseAddonDeed
    {
        [Constructible]
        public ElvenStoveEastDeed()
        {
        }

        public override BaseAddon Addon => new ElvenStoveEastAddon();
        public override int LabelNumber => 1073395; // elven oven (east)
    }
}
