using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class MirrorOfPurification : Item
{
    [Constructible]
    public MirrorOfPurification() : base(0x1008)
    {
        LootType = LootType.Blessed;
        Hue = 0x530;
    }

    public override int LabelNumber => 1075304; // Mirror of Purification
}
