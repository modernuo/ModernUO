using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class WindOfCorruption : Cyclone
{
    [Constructible]
    public WindOfCorruption()
    {
        Hue = 1171;
        Attributes.WeaponSpeed = 30;
        Attributes.WeaponDamage = 50;
        Slayer = SlayerName.Fey;
        WeaponAttributes.HitLeechStam = 40;
        WeaponAttributes.HitLowerDefend = 40;
        AosElementDamages.Chaos = 100;
    }

    public override int LabelNumber => 1150358; // Wind of Corruption

    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;
}
