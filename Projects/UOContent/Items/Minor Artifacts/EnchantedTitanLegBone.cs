using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class EnchantedTitanLegBone : ShortSpear
{
    [Constructible]
    public EnchantedTitanLegBone()
    {
        Hue = 0x8A5;
        WeaponAttributes.HitLowerDefend = 40;
        WeaponAttributes.HitLightning = 40;
        Attributes.AttackChance = 10;
        Attributes.WeaponDamage = 20;
        WeaponAttributes.ResistPhysicalBonus = 10;
    }

    public override int LabelNumber => 1063482;

    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;
}
