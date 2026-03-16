using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class RaptorClaw : Boomerang
{
    [Constructible]
    public RaptorClaw()
    {
        Hue = 53;
        Attributes.AttackChance = 12;
        Attributes.WeaponSpeed = 30;
        Attributes.WeaponDamage = 35;
        Slayer = SlayerName.Silver;
        WeaponAttributes.HitLeechStam = 40;
    }

    public override int LabelNumber => 1112394; // Raptor Claw

    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;
}
