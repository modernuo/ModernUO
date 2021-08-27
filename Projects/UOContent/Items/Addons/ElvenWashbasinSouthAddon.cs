namespace Server.Items
{
    [Serializable(0)]
    public partial class ElvenWashBasinSouthAddon : BaseAddon
    {
        [Constructible]
        public ElvenWashBasinSouthAddon()
        {
            AddComponent(new AddonComponent(0x30E1), 0, 0, 0);
            AddComponent(new AddonComponent(0x30E2), 1, 0, 0);
        }

        public override BaseAddonDeed Deed => new ElvenWashBasinSouthDeed();
    }

    [Serializable(0)]
    public partial class ElvenWashBasinSouthDeed : BaseAddonDeed
    {
        [Constructible]
        public ElvenWashBasinSouthDeed()
        {
        }

        public override BaseAddon Addon => new ElvenWashBasinSouthAddon();
        public override int LabelNumber => 1072865; // elven wash basin (south)
    }
}
