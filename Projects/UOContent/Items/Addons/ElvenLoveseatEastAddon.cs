namespace Server.Items
{
    [Serializable(0)]
    public partial class ElvenLoveseatEastAddon : BaseAddon
    {
        [Constructible]
        public ElvenLoveseatEastAddon()
        {
            AddComponent(new AddonComponent(0x3089), 0, 0, 0);
            AddComponent(new AddonComponent(0x3088), 1, 0, 0);
        }

        public override BaseAddonDeed Deed => new ElvenLoveseatEastDeed();
    }

    [Serializable(0)]
    public partial class ElvenLoveseatEastDeed : BaseAddonDeed
    {
        [Constructible]
        public ElvenLoveseatEastDeed()
        {
        }

        public override BaseAddon Addon => new ElvenLoveseatEastAddon();
        public override int LabelNumber => 1073372; // elven loveseat (east)
    }
}
