using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class Windsong : MagicalShortbow
{
    [Constructible]
    public Windsong()
    {
        Hue = 0xF7;

        Attributes.WeaponDamage = 35;
        WeaponAttributes.SelfRepair = 3;

        Velocity = 25;
    }

    public override int LabelNumber => 1075031; // Windsong

    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;
}
