using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class CavortingClub : Club
{
    [Constructible]
    public CavortingClub()
    {
        Hue = 0x593;
        WeaponAttributes.SelfRepair = 3;
        Attributes.WeaponSpeed = 25;
        Attributes.WeaponDamage = 35;
        WeaponAttributes.ResistFireBonus = 8;
        WeaponAttributes.ResistColdBonus = 8;
        WeaponAttributes.ResistPoisonBonus = 8;
        WeaponAttributes.ResistEnergyBonus = 8;
    }

    public override int LabelNumber => 1063472;

    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;
}
