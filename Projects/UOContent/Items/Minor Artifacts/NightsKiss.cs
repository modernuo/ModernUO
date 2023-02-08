using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class NightsKiss : Dagger
{
    [Constructible]
    public NightsKiss()
    {
        ItemID = 0xF51;
        Hue = 0x455;
        WeaponAttributes.HitLeechHits = 40;
        Slayer = SlayerName.Repond;
        Attributes.WeaponSpeed = 30;
        Attributes.WeaponDamage = 35;
    }

    public override int LabelNumber => 1063475;

    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;
}
