using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class MetalShield : BaseShield
{
    [Constructible]
    public MetalShield() : base(0x1B7B) => Weight = 6.0;

    public override int BasePhysicalResistance => 0;
    public override int BaseFireResistance => 1;
    public override int BaseColdResistance => 0;
    public override int BasePoisonResistance => 0;
    public override int BaseEnergyResistance => 0;

    public override int InitMinHits => 50;
    public override int InitMaxHits => 65;

    public override int AosStrReq => 45;

    public override int ArmorBase => 11;
}
