using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class PottedTree : Item
{
    [Constructible]
    public PottedTree() : base(0x11C8) => Weight = 100;
}

[SerializationGenerator(0, false)]
public partial class PottedTree1 : Item
{
    [Constructible]
    public PottedTree1() : base(0x11C9) => Weight = 100;
}
