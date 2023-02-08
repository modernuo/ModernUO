using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class OrcishVisage : OrcHelm
{
    [Constructible]
    public OrcishVisage()
    {
        Hue = 0x592;
        ArmorAttributes.SelfRepair = 3;
        Attributes.BonusStr = 10;
        Attributes.BonusStam = 5;
    }

    public override int LabelNumber => 1070691;

    public override int BasePhysicalResistance => 8;
    public override int BaseFireResistance => 5;
    public override int BaseColdResistance => 3;
    public override int BasePoisonResistance => 3;
    public override int BaseEnergyResistance => 5;

    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;
}
