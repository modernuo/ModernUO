using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class RighteousAnger : ElvenMachete
{
    [Constructible]
    public RighteousAnger()
    {
        Hue = 0x284;

        Attributes.AttackChance = 15;
        Attributes.DefendChance = 5;
        Attributes.WeaponSpeed = 35;
        Attributes.WeaponDamage = 40;
    }

    public override int LabelNumber => 1075049; // Righteous Anger

    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;
}
