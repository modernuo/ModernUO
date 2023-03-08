using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class ShaminoCrossbow : RepeatingCrossbow
{
    [Constructible]
    public ShaminoCrossbow()
    {
        Hue = 0x504;
        LootType = LootType.Blessed;

        Attributes.AttackChance = 15;
        Attributes.WeaponDamage = 40;
        WeaponAttributes.SelfRepair = 10;
        WeaponAttributes.LowerStatReq = 100;
    }

    public override int LabelNumber => 1062915; // Shamino's Best Crossbow

    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;
}
