using ModernUO.Serialization;

namespace Server.Items;

[Furniture]
[Flippable(0x2bd9, 0x2bda)]
[SerializationGenerator(0)]
public partial class GreenStocking : BaseContainer
{
    [Constructible]
    public GreenStocking() : base(Utility.Random(0x2BD9, 2))
    {
    }

    public override int DefaultGumpID => 0x103;
    public override int DefaultDropSound => 0x42;
}

[Furniture]
[Flippable(0x2bdb, 0x2bdc)]
[SerializationGenerator(0)]
public partial class RedStocking : BaseContainer
{
    [Constructible]
    public RedStocking() : base(Utility.Random(0x2BDB, 2))
    {
    }

    public override int DefaultGumpID => 0x103;
    public override int DefaultDropSound => 0x42;
}
