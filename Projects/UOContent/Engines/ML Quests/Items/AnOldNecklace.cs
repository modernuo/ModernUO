using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class AnOldNecklace : Necklace
{
    [Constructible]
    public AnOldNecklace() => Hue = 0x222;

    public override int LabelNumber => 1075525; // an old necklace
}
