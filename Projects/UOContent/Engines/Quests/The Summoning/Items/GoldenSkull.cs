using ModernUO.Serialization;

namespace Server.Engines.Quests.Doom;

[SerializationGenerator(0, false)]
public partial class GoldenSkull : Item
{
    [Constructible]
    public GoldenSkull() : base(Utility.Random(0x1AE2, 3))
    {
        Weight = 1.0;
        Hue = 0x8A5;
        LootType = LootType.Blessed;
    }

    public override int LabelNumber => 1061619; // a golden skull
}
