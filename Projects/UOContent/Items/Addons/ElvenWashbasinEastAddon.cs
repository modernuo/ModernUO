namespace Server.Items
{
    [Serializable(0)]
    public partial class ElvenWashBasinEastAddon : BaseAddon
    {
        [Constructible]
        public ElvenWashBasinEastAddon()
        {
            AddComponent(new AddonComponent(0x30DF), 0, 0, 0);
            AddComponent(new AddonComponent(0x30E0), 0, 1, 0);
        }

        public override BaseAddonDeed Deed => new ElvenWashBasinEastDeed();
    }

    [Serializable(0)]
    public partial class ElvenWashBasinEastDeed : BaseAddonDeed
    {
        [Constructible]
        public ElvenWashBasinEastDeed()
        {
        }

        public override BaseAddon Addon => new ElvenWashBasinEastAddon();
        public override int LabelNumber => 1073387; // elven wash basin (east)
    }
}
