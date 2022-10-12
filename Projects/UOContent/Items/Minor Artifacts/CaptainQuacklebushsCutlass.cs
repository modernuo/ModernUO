using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class CaptainQuacklebushsCutlass : Cutlass
{
    [Constructible]
    public CaptainQuacklebushsCutlass()
    {
        Hue = 0x66C;
        Attributes.BonusDex = 5;
        Attributes.AttackChance = 10;
        Attributes.WeaponSpeed = 20;
        Attributes.WeaponDamage = 50;
        WeaponAttributes.UseBestSkill = 1;
    }

    public override int LabelNumber => 1063474;

    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;
}
