using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class JocklesQuicksword : Longsword
{
    [Constructible]
    public JocklesQuicksword()
    {
        LootType = LootType.Blessed;

        Attributes.AttackChance = 5;
        Attributes.WeaponSpeed = 10;
        Attributes.WeaponDamage = 25;
    }

    public override int LabelNumber => 1077666; // Jockles' Quicksword
}
