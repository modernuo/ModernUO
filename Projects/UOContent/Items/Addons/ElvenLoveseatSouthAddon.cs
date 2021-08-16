namespace Server.Items
{
    [Serializable(0)]
    public partial class ElvenLoveseatSouthAddon : BaseAddon
    {
        [Constructible]
        public ElvenLoveseatSouthAddon()
        {
            AddComponent(new AddonComponent(0x308A), 0, 0, 0);
            AddComponent(new AddonComponent(0x308B), 0, -1, 0);
        }

        public override BaseAddonDeed Deed => new ElvenLoveseatSouthDeed();
    }

    [Serializable(0)]
    public partial class ElvenLoveseatSouthDeed : BaseAddonDeed
    {
        [Constructible]
        public ElvenLoveseatSouthDeed()
        {
        }

        public override BaseAddon Addon => new ElvenLoveseatSouthAddon();
        public override int LabelNumber => 1072867; // elven loveseat (south)
    }
}
