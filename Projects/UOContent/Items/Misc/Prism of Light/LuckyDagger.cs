using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class LuckyDagger : Item
{
    [Constructible]
    public LuckyDagger() : base(0xF52) => Hue = 0x8A5;

    public override int LabelNumber => 1151983; // Lucky Dagger
}
