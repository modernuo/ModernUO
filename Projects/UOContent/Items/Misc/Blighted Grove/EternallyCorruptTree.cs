using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class EternallyCorruptTree : Item
{
    [Constructible]
    public EternallyCorruptTree() : base(0x20FA) => Hue = Utility.RandomMinMax(0x899, 0x8B0);

    public override int LabelNumber => 1072093; // Eternally Corrupt Tree
}
