using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class EssenceOfBattle : GoldRing
{
    [Constructible]
    public EssenceOfBattle()
    {
        Hue = 0x550;
        Attributes.BonusDex = 7;
        Attributes.BonusStr = 7;
        Attributes.WeaponDamage = 30;
    }

    public override int LabelNumber => 1072935; // Essence of Battle
}
