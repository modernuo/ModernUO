using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0)]
    public partial class AlchemistTableSouthAddon : BaseAddon
    {
        [Constructible]
        public AlchemistTableSouthAddon()
        {
            AddComponent(new AddonComponent(0x2DD4), 0, 0, 0);
        }

        public override BaseAddonDeed Deed => new AlchemistTableSouthDeed();
    }

    [SerializationGenerator(0)]
    public partial class AlchemistTableSouthDeed : BaseAddonDeed
    {
        [Constructible]
        public AlchemistTableSouthDeed()
        {
        }

        public override BaseAddon Addon => new AlchemistTableSouthAddon();
        public override int LabelNumber => 1073396; // alchemist table (south)
    }
}
