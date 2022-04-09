using ModernUO.Serialization;

namespace Server.Items;

[Flippable(0xe43, 0xe42)]
[SerializationGenerator(0, false)]
public partial class WoodenTreasureChest : BaseTreasureChest
{
    [Constructible]
    public WoodenTreasureChest() : base(0xE43)
    {
    }
}

[Flippable(0xe41, 0xe40)]
[SerializationGenerator(0, false)]
public partial class MetalGoldenTreasureChest : BaseTreasureChest
{
    [Constructible]
    public MetalGoldenTreasureChest() : base(0xE41)
    {
    }
}

[Flippable(0x9ab, 0xe7c)]
[SerializationGenerator(0, false)]
public partial class MetalTreasureChest : BaseTreasureChest
{
    [Constructible]
    public MetalTreasureChest() : base(0x9AB)
    {
    }
}
