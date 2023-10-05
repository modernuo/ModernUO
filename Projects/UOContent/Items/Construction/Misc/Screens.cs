using ModernUO.Serialization;

namespace Server.Items;

[Furniture]
[Flippable(0x24D0, 0x24D1, 0x24D2, 0x24D3, 0x24D4)]
[SerializationGenerator(0, false)]
public partial class BambooScreen : Item
{
    [Constructible]
    public BambooScreen() : base(0x24D0) => Weight = 20.0;
}

[Furniture]
[Flippable(0x24CB, 0x24CC, 0x24CD, 0x24CE, 0x24CF)]
[SerializationGenerator(0, false)]
public partial class ShojiScreen : Item
{
    [Constructible]
    public ShojiScreen() : base(0x24CB) => Weight = 20.0;
}
