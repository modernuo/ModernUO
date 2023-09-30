using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class TransparentHeart : GoldEarrings
{
    [Constructible]
    public TransparentHeart()
    {
        LootType = LootType.Blessed;
        Hue = 0x4AB;
    }

    public override int LabelNumber => 1075400; // Transparent Heart
}
