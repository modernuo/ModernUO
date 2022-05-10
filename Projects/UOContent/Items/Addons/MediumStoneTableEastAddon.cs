using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class MediumStoneTableEastAddon : BaseAddon
    {
        [Constructible]
        public MediumStoneTableEastAddon(int hue = 0)
        {
            AddComponent(new AddonComponent(0x1202), 0, 0, 0);
            AddComponent(new AddonComponent(0x1201), 0, 1, 0);
            Hue = hue;
        }

        public override BaseAddonDeed Deed => new MediumStoneTableEastDeed();

        public override bool RetainDeedHue => true;
    }

    [SerializationGenerator(0, false)]
    public partial class MediumStoneTableEastDeed : BaseAddonDeed
    {
        [Constructible]
        public MediumStoneTableEastDeed()
        {
        }

        public override BaseAddon Addon => new MediumStoneTableEastAddon(Hue);
        public override int LabelNumber => 1044508; // stone table (east)
    }
}
