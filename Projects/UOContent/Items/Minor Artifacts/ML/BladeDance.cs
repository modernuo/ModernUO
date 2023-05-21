using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class BladeDance : RuneBlade
{
    [Constructible]
    public BladeDance()
    {
        Hue = 0x66C;

        Attributes.BonusMana = 8;
        Attributes.SpellChanneling = 1;
        Attributes.WeaponDamage = 30;
        WeaponAttributes.HitLeechMana = 20;
        WeaponAttributes.UseBestSkill = 1;
    }

    public override int LabelNumber => 1075033; // Blade Dance

    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;
}
