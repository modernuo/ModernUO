using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class TwilightJacket : LeatherNinjaJacket
{
    [Constructible]
    public TwilightJacket()
    {
        LootType = LootType.Blessed;

        Attributes.ReflectPhysical = 5;
    }

    public override int LabelNumber => 1078183; // Twilight Jacket

    public override int BasePhysicalResistance => 6;
    public override int BaseFireResistance => 12;
    public override int BaseColdResistance => 3;
    public override int BasePoisonResistance => 3;
    public override int BaseEnergyResistance => 3;
}
