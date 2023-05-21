using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class LesserCurePotion : BaseCurePotion
{
    private static readonly CureLevelInfo[] _oldLevelInfo =
    {
        new(Poison.Lesser, 0.75),  // 75% chance to cure lesser poison
        new(Poison.Regular, 0.50), // 50% chance to cure regular poison
        new(Poison.Greater, 0.15)  // 15% chance to cure greater poison
    };

    private static readonly CureLevelInfo[] _aosLevelInfo =
    {
        new(Poison.Lesser, 0.75),
        new(Poison.Regular, 0.50),
        new(Poison.Greater, 0.25)
    };

    [Constructible]
    public LesserCurePotion() : base(PotionEffect.CureLesser)
    {
    }

    public override CureLevelInfo[] LevelInfo => Core.AOS ? _aosLevelInfo : _oldLevelInfo;
}
