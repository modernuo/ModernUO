using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class GreaterCurePotion : BaseCurePotion
{
    private static readonly CureLevelInfo[] _oldLevelInfo =
    {
        new(Poison.Lesser, 1.00),  // 100% chance to cure lesser poison
        new(Poison.Regular, 1.00), // 100% chance to cure regular poison
        new(Poison.Greater, 1.00), // 100% chance to cure greater poison
        new(Poison.Deadly, 0.75),  //  75% chance to cure deadly poison
        new(Poison.Lethal, 0.25)   //  25% chance to cure lethal poison
    };

    private static readonly CureLevelInfo[] _aosLevelInfo =
    {
        new(Poison.Lesser, 1.00),
        new(Poison.Regular, 1.00),
        new(Poison.Greater, 1.00),
        new(Poison.Deadly, 0.95),
        new(Poison.Lethal, 0.75)
    };

    [Constructible]
    public GreaterCurePotion() : base(PotionEffect.CureGreater)
    {
    }

    public override CureLevelInfo[] LevelInfo => Core.AOS ? _aosLevelInfo : _oldLevelInfo;
}
