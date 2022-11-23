using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class WalkersLeggings : LeatherNinjaPants
{
    [Constructible]
    public WalkersLeggings() => LootType = LootType.Blessed;

    public override int LabelNumber => 1078222; // Walker's Leggings

    public override int BasePhysicalResistance => 10;
    public override int BaseFireResistance => 6;
    public override int BaseColdResistance => 6;
    public override int BasePoisonResistance => 3;
    public override int BaseEnergyResistance => 3;
}
