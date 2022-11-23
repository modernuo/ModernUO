using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class Heartseeker : CompositeBow
{
    [Constructible]
    public Heartseeker()
    {
        LootType = LootType.Blessed;

        Attributes.AttackChance = 5;
        Attributes.WeaponSpeed = 10;
        Attributes.WeaponDamage = 25;
        WeaponAttributes.LowerStatReq = 70;
    }

    public override int LabelNumber => 1078210; // Heartseeker
}
