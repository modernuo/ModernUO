using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class ValkyriesGlaive : SoulGlaive
{
    [Constructible]
    public ValkyriesGlaive()
    {
        Hue = 1651;
        Attributes.SpellChanneling = 1;
        Attributes.BonusStr = 5;
        Attributes.WeaponSpeed = 20;
        Attributes.WeaponDamage = 20;
        Slayer = SlayerName.Silver;
        WeaponAttributes.HitFireball = 40;
    }

    public override int LabelNumber => 1113531; // Valkyrie's Glaive

    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;
}
