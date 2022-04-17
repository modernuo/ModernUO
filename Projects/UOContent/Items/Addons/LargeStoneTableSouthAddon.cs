using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class LargeStoneTableSouthAddon : BaseAddon
    {
        [Constructible]
        public LargeStoneTableSouthAddon(int hue = 0)
        {
            AddComponent(new AddonComponent(0x1205), 0, 0, 0);
            AddComponent(new AddonComponent(0x1206), 1, 0, 0);
            AddComponent(new AddonComponent(0x1204), 2, 0, 0);
            Hue = hue;
        }

        public override BaseAddonDeed Deed => new LargeStoneTableSouthDeed();

        public override bool RetainDeedHue => true;
    }

    [SerializationGenerator(0, false)]
    public partial class LargeStoneTableSouthDeed : BaseAddonDeed
    {
        [Constructible]
        public LargeStoneTableSouthDeed()
        {
        }

        public override BaseAddon Addon => new LargeStoneTableSouthAddon(Hue);
        public override int LabelNumber => 1044512; // large stone table (South)
    }
}
