using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class MelisandesCorrodedHatchet : Hatchet
{
    [Constructible]
    public MelisandesCorrodedHatchet()
    {
        Hue = 0x494;

        SkillBonuses.SetValues(0, SkillName.Lumberjacking, 5.0);

        Attributes.SpellChanneling = 1;
        Attributes.WeaponSpeed = 15;
        Attributes.WeaponDamage = -50;

        WeaponAttributes.SelfRepair = 4;
    }

    public override int LabelNumber => 1072115; // Melisande's Corroded Hatchet
}
