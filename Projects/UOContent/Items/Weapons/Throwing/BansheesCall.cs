using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class BansheesCall : Cyclone
{
    [Constructible]
    public BansheesCall()
    {
        Hue = 1266;
        Attributes.BonusStr = 5;
        Attributes.WeaponSpeed = 30;
        Attributes.WeaponDamage = 50;
        WeaponAttributes.HitHarm = 40;
        WeaponAttributes.HitLeechHits = 45;
        Velocity = 35;
        AosElementDamages.Cold = 100;
    }

    public override int LabelNumber => 1113529; // Banshee's Call

    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;
}
