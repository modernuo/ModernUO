namespace Server.Items
{
    [Serializable(0)]
    public partial class OrnateElvenChestEastAddon : BaseAddon
    {
        [Constructible]
        public OrnateElvenChestEastAddon()
        {
            AddComponent(new AddonComponent(0x2DEA), 0, 0, 0);
        }

        public override BaseAddonDeed Deed => new OrnateElvenChestEastDeed();
    }

    [Serializable(0)]
    public partial class OrnateElvenChestEastDeed : BaseAddonDeed
    {
        [Constructible]
        public OrnateElvenChestEastDeed()
        {
        }

        public override BaseAddon Addon => new OrnateElvenChestEastAddon();
        public override int LabelNumber => 1073383; // ornate elven chest (east)
    }
}
