using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class LunaLance : Lance
{
    [Constructible]
    public LunaLance()
    {
        Hue = 0x47E;
        SkillBonuses.SetValues(0, SkillName.Chivalry, 10.0);
        Attributes.BonusStr = 5;
        Attributes.WeaponSpeed = 20;
        Attributes.WeaponDamage = 35;
        WeaponAttributes.UseBestSkill = 1;
    }

    public override int LabelNumber => 1063469;

    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;
}
