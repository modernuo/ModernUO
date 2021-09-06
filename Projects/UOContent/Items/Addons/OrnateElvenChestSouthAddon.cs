namespace Server.Items
{
    [Serializable(0)]
    public partial class OrnateElvenChestSouthAddon : BaseAddon
    {
        [Constructible]
        public OrnateElvenChestSouthAddon()
        {
            AddComponent(new AddonComponent(0x2DE9), 0, 0, 0);
        }

        public override BaseAddonDeed Deed => new OrnateElvenChestSouthDeed();
    }

    [Serializable(0)]
    public partial class OrnateElvenChestSouthDeed : BaseAddonDeed
    {
        [Constructible]
        public OrnateElvenChestSouthDeed()
        {
        }

        public override BaseAddon Addon => new OrnateElvenChestSouthAddon();
        public override int LabelNumber => 1072862; // ornate elven chest (south)
    }
}
