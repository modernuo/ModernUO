using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0)]
    public partial class OrnateElvenTableSouthAddon : BaseAddon
    {
        [Constructible]
        public OrnateElvenTableSouthAddon()
        {
            AddComponent(new AddonComponent(0x308F), 0, 1, 0);
            AddComponent(new AddonComponent(0x3090), 0, 0, 0);
            AddComponent(new AddonComponent(0x3091), 0, -1, 0);
        }

        public override BaseAddonDeed Deed => new OrnateElvenTableSouthDeed();
    }

    [SerializationGenerator(0)]
    public partial class OrnateElvenTableSouthDeed : BaseAddonDeed
    {
        [Constructible]
        public OrnateElvenTableSouthDeed()
        {
        }

        public override BaseAddon Addon => new OrnateElvenTableSouthAddon();
        public override int LabelNumber => 1072869; // ornate table (south)
    }
}
