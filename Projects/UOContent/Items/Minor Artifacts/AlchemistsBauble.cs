using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class AlchemistsBauble : GoldBracelet
{
    [Constructible]
    public AlchemistsBauble()
    {
        Hue = 0x290;
        SkillBonuses.SetValues(0, SkillName.Magery, 10.0);
        Attributes.EnhancePotions = 30;
        Attributes.LowerRegCost = 20;
        Resistances.Poison = 10;
    }

    public override int LabelNumber => 1070638;
}
