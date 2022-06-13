using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class MediumStoneTableSouthAddon : BaseAddon
    {
        [Constructible]
        public MediumStoneTableSouthAddon(int hue = 0)
        {
            AddComponent(new AddonComponent(0x1205), 0, 0, 0);
            AddComponent(new AddonComponent(0x1204), 1, 0, 0);
            Hue = hue;
        }

        public override BaseAddonDeed Deed => new MediumStoneTableSouthDeed();

        public override bool RetainDeedHue => true;
    }

    [SerializationGenerator(0, false)]
    public partial class MediumStoneTableSouthDeed : BaseAddonDeed
    {
        [Constructible]
        public MediumStoneTableSouthDeed()
        {
        }

        public override BaseAddon Addon => new MediumStoneTableSouthAddon(Hue);
        public override int LabelNumber => 1044509; // stone table (South)
    }
}
