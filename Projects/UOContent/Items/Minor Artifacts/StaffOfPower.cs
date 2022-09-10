using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class StaffOfPower : BlackStaff
{
    [Constructible]
    public StaffOfPower()
    {
        Hue = 0x4F2;
        WeaponAttributes.MageWeapon = 15;
        Attributes.SpellChanneling = 1;
        Attributes.SpellDamage = 5;
        Attributes.CastRecovery = 2;
        Attributes.LowerManaCost = 5;
    }

    public override int LabelNumber => 1070692;

    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;
}
