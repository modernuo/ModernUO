using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class GlovesOfSafeguarding : LeatherGloves
{
    [Constructible]
    public GlovesOfSafeguarding()
    {
        LootType = LootType.Blessed;

        Attributes.BonusStam = 3;
        Attributes.RegenHits = 1;
    }

    public override int LabelNumber => 1077614; // Gloves of Safeguarding

    public override int BasePhysicalResistance => 6;
    public override int BaseFireResistance => 6;
    public override int BaseColdResistance => 5;
    public override int BasePoisonResistance => 5;
    public override int BaseEnergyResistance => 5;
}
