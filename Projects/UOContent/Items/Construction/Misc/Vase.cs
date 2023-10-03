using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class Vase : Item
{
    [Constructible]
    public Vase() : base(0xB46) => Weight = 10;
}

[SerializationGenerator(0, false)]
public partial class LargeVase : Item
{
    [Constructible]
    public LargeVase() : base(0xB45) => Weight = 15;
}

[SerializationGenerator(0, false)]
public partial class SmallUrn : Item
{
    [Constructible]
    public SmallUrn() : base(0x241C) => Weight = 20.0;
}
