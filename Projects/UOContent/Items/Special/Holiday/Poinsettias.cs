using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class RedPoinsettia : Item
{
    [Constructible]
    public RedPoinsettia() : base(0x2330)
    {
        Weight = 1.0;
        LootType = LootType.Blessed;
    }
}

[SerializationGenerator(0, false)]
public partial class WhitePoinsettia : Item
{
    [Constructible]
    public WhitePoinsettia() : base(0x2331)
    {
        Weight = 1.0;
        LootType = LootType.Blessed;
    }
}
