using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class PhilosophersHat : WizardsHat
{
    [Constructible]
    public PhilosophersHat()
    {
        LootType = LootType.Blessed;

        Attributes.RegenMana = 1;
        Attributes.LowerRegCost = 7;
    }

    public override int LabelNumber => 1077602; // Philosopher's Hat

    public override int BasePhysicalResistance => 5;
    public override int BaseFireResistance => 5;
    public override int BaseColdResistance => 9;
    public override int BasePoisonResistance => 5;
    public override int BaseEnergyResistance => 5;
}
