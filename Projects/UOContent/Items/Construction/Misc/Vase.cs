using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class Vase : Item
{
    [Constructible]
    public Vase() : base(0xB46)
    {
    }

    public override double DefaultWeight => 10;
}

[SerializationGenerator(0, false)]
public partial class LargeVase : Item
{
    [Constructible]
    public LargeVase() : base(0xB45)
    {
    }

    public override double DefaultWeight => 15;
}

[SerializationGenerator(0, false)]
public partial class SmallUrn : Item
{
    [Constructible]
    public SmallUrn() : base(0x241C)
    {
    }

    public override double DefaultWeight => 20.0;
}
