using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class AnOldRing : GoldRing
{
    [Constructible]
    public AnOldRing() => Hue = 0x222;

    public override int LabelNumber => 1075524; // an old ring
}
