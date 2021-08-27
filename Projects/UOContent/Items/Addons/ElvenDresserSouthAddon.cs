namespace Server.Items
{
    [Serializable(0)]
    public partial class ElvenDresserSouthAddon : BaseAddon
    {
        [Constructible]
        public ElvenDresserSouthAddon()
        {
            AddComponent(new AddonComponent(0x30E5), 0, 0, 0);
            AddComponent(new AddonComponent(0x30E6), 1, 0, 0);
        }

        public override BaseAddonDeed Deed => new ElvenDresserSouthDeed();
    }

    [Serializable(0)]
    public partial class ElvenDresserSouthDeed : BaseAddonDeed
    {
        [Constructible]
        public ElvenDresserSouthDeed()
        {
        }

        public override BaseAddon Addon => new ElvenDresserSouthAddon();
        public override int LabelNumber => 1072864; // elven dresser (south)
    }
}
