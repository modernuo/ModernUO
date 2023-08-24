using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class HollowPrism : Item
{
    [Constructible]
    public HollowPrism() : base(0x2F5D) => Weight = 1.0;

    public override int LabelNumber => 1072895; // hollow prism
}
