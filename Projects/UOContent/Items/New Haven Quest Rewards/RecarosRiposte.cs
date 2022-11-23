using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class RecarosRiposte : WarFork
{
    [Constructible]
    public RecarosRiposte()
    {
        LootType = LootType.Blessed;

        Attributes.AttackChance = 5;
        Attributes.WeaponSpeed = 10;
        Attributes.WeaponDamage = 25;
    }

    public override int LabelNumber => 1078195; // Recaro's Riposte
}
