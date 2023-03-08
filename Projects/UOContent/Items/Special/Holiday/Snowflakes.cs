using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class BlueSnowflake : Item
{
    [Constructible]
    public BlueSnowflake() : base(0x232E)
    {
        Weight = 1.0;
        LootType = LootType.Blessed;
    }
}

[SerializationGenerator(0, false)]
public partial class WhiteSnowflake : Item
{
    [Constructible]
    public WhiteSnowflake() : base(0x232F)
    {
        Weight = 1.0;
        LootType = LootType.Blessed;
    }
}
