using ModernUO.Serialization;
using Server.Engines.Craft;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class ShrineOfWisdomAddon : BaseAddon
    {
        [Constructible]
        public ShrineOfWisdomAddon()
        {
            AddComponent(new ShrineOfWisdomComponent(0x14C3), 0, 0, 0);
            AddComponent(new ShrineOfWisdomComponent(0x14C6), 1, 0, 0);
            AddComponent(new ShrineOfWisdomComponent(0x14D4), 0, 1, 0);
            AddComponent(new ShrineOfWisdomComponent(0x14D5), 1, 1, 0);
            Hue = 0x47E;
        }
    }

    [Forge]
    [SerializationGenerator(0, false)]
    public partial class ShrineOfWisdomComponent : AddonComponent
    {
        [Constructible]
        public ShrineOfWisdomComponent(int itemID) : base(itemID)
        {
        }

        public override int LabelNumber => 1062046; // Shrine of Wisdom
    }
}
