using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class SmallEmptyPot : Item
{
    [Constructible]
    public SmallEmptyPot() : base(0x11C6) => Weight = 100;
}

[SerializationGenerator(0, false)]
public partial class LargeEmptyPot : Item
{
    [Constructible]
    public LargeEmptyPot() : base(0x11C7) => Weight = 6;
}
