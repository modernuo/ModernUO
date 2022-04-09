using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class LargeStoneTableEastAddon : BaseAddon
    {
        [Constructible]
        public LargeStoneTableEastAddon(int hue = 0)
        {
            AddComponent(new AddonComponent(0x1202), 0, 0, 0);
            AddComponent(new AddonComponent(0x1203), 0, 1, 0);
            AddComponent(new AddonComponent(0x1201), 0, 2, 0);
            Hue = hue;
        }
        public override BaseAddonDeed Deed => new LargeStoneTableEastDeed();

        public override bool RetainDeedHue => true;
    }

    [SerializationGenerator(0, false)]
    public partial class LargeStoneTableEastDeed : BaseAddonDeed
    {
        [Constructible]
        public LargeStoneTableEastDeed()
        {
        }

        public override BaseAddon Addon => new LargeStoneTableEastAddon(Hue);
        public override int LabelNumber => 1044511; // large stone table (east)
    }
}
