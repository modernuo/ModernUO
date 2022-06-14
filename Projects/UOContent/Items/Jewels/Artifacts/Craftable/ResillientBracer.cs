using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class ResilientBracer : GoldBracelet
{
    [Constructible]
    public ResilientBracer()
    {
        Hue = 0x488;

        SkillBonuses.SetValues(0, SkillName.MagicResist, 15.0);

        Attributes.BonusHits = 5;
        Attributes.RegenHits = 2;
        Attributes.DefendChance = 10;
    }

    public override int LabelNumber => 1072933; // Resillient Bracer

    public override int PhysicalResistance => 20;
}
