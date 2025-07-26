using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class BlueSnowflake : Item
{
    [Constructible]
    public BlueSnowflake() : base(0x232E)
    {
        LootType = LootType.Blessed;
    }

    public override double DefaultWeight => 1.0;
}

[SerializationGenerator(0, false)]
public partial class WhiteSnowflake : Item
{
    [Constructible]
    public WhiteSnowflake() : base(0x232F)
    {
        LootType = LootType.Blessed;
    }

    public override double DefaultWeight => 1.0;
}
