using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class RedPoinsettia : Item
{
    [Constructible]
    public RedPoinsettia() : base(0x2330)
    {
        LootType = LootType.Blessed;
    }

    public override double DefaultWeight => 1.0;
}

[SerializationGenerator(0, false)]
public partial class WhitePoinsettia : Item
{
    [Constructible]
    public WhitePoinsettia() : base(0x2331)
    {
        LootType = LootType.Blessed;
    }

    public override double DefaultWeight => 1.0;
}
