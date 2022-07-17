using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class PendantOfTheMagi : GoldNecklace
{
    [Constructible]
    public PendantOfTheMagi()
    {
        Hue = 0x48D;
        Attributes.BonusInt = 10;
        Attributes.RegenMana = 3;
        Attributes.SpellDamage = 5;
        Attributes.LowerManaCost = 10;
        Attributes.LowerRegCost = 30;
    }

    public override int LabelNumber => 1072937; // Pendant of the Magi
}
