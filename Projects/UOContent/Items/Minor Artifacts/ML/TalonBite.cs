using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class TalonBite : OrnateAxe
{
    [Constructible]
    public TalonBite()
    {
        ItemID = 0x2D34;
        Hue = 0x47E;

        SkillBonuses.SetValues(0, SkillName.Tactics, 10.0);

        Attributes.BonusDex = 8;
        Attributes.WeaponSpeed = 20;
        Attributes.WeaponDamage = 35;

        WeaponAttributes.HitHarm = 33;
        WeaponAttributes.UseBestSkill = 1;
    }

    public override int LabelNumber => 1075029; // Talon Bite

    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;
}
