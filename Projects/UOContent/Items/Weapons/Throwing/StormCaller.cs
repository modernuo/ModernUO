using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class StormCaller : Boomerang
{
    [Constructible]
    public StormCaller()
    {
        Hue = 456;
        //TODO Implement BattleLust
        //ExtendedWeaponAttributes.BattleLust = 1;
        WeaponAttributes.HitLightning = 40;
        WeaponAttributes.HitLowerDefend = 30;
        Attributes.BonusStr = 5;
        Attributes.WeaponSpeed = 30;
        Attributes.WeaponDamage = 40;
        AosElementDamages.Physical = 20;
        AosElementDamages.Fire = 20;
        AosElementDamages.Cold = 20;
        AosElementDamages.Poison = 20;
        AosElementDamages.Energy = 20;
    }

    public override int LabelNumber => 1113530; // Storm Caller

    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;
}
