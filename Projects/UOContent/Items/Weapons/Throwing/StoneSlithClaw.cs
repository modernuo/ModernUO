using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class StoneSlithClaw : Cyclone
{
    [Constructible]
    public StoneSlithClaw()
    {
        Hue = 1150;
        Attributes.WeaponSpeed = 25;
        Attributes.WeaponDamage = 45;
        Slayer = SlayerName.DaemonDismissal;
        WeaponAttributes.HitHarm = 40;
        WeaponAttributes.HitLowerDefend = 40;
    }

    public override int LabelNumber => 1112393; // Stone Slith Claw

    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;
}
