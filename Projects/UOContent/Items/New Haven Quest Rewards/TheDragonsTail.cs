using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class TheDragonsTail : NoDachi
{
    [Constructible]
    public TheDragonsTail()
    {
        LootType = LootType.Blessed;

        WeaponAttributes.HitLeechStam = 16;
        Attributes.WeaponSpeed = 10;
        Attributes.WeaponDamage = 25;
    }

    public override int LabelNumber => 1078015; // The Dragon's Tail

    public override int InitMinHits => 80;
    public override int InitMaxHits => 80;
}
