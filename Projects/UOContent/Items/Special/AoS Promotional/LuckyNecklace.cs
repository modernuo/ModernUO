using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class LuckyNecklace : BaseJewel
{
    [Constructible]
    public LuckyNecklace() : base(0x1088, Layer.Neck)
    {
        Attributes.Luck = 200;
        LootType = LootType.Blessed;
    }

    public override int Hue => 1150;
    public override int LabelNumber => 1075239; // Lucky Necklace	1075239
}
