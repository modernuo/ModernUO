using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class BulwarkLeggings : RingmailLegs
{
    [Constructible]
    public BulwarkLeggings()
    {
        LootType = LootType.Blessed;

        Attributes.RegenStam = 1;
        Attributes.RegenMana = 1;
    }

    public override int LabelNumber => 1077727; // Bulwark Leggings

    public override int BasePhysicalResistance => 9;
    public override int BaseFireResistance => 5;
    public override int BaseColdResistance => 5;
    public override int BasePoisonResistance => 5;
    public override int BaseEnergyResistance => 3;
}
