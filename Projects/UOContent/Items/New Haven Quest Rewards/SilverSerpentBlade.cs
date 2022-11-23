using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class SilverSerpentBlade : Kryss
{
    [Constructible]
    public SilverSerpentBlade()
    {
        LootType = LootType.Blessed;

        Attributes.AttackChance = 5;
        Attributes.WeaponSpeed = 10;
        Attributes.WeaponDamage = 25;
    }

    public override int LabelNumber => 1078163; // Silver Serpent Blade
}
