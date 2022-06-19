using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0)]
    public partial class OrnateElvenTableEastAddon : BaseAddon
    {
        [Constructible]
        public OrnateElvenTableEastAddon()
        {
            AddComponent(new AddonComponent(0x308E), -1, 0, 0);
            AddComponent(new AddonComponent(0x308D), 0, 0, 0);
            AddComponent(new AddonComponent(0x308C), 1, 0, 0);
        }

        public override BaseAddonDeed Deed => new OrnateElvenTableEastDeed();
    }

    [SerializationGenerator(0)]
    public partial class OrnateElvenTableEastDeed : BaseAddonDeed
    {
        [Constructible]
        public OrnateElvenTableEastDeed()
        {
        }

        public override BaseAddon Addon => new OrnateElvenTableEastAddon();
        public override int LabelNumber => 1073384; // ornate table (east)
    }
}
