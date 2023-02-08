using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class Buckler : BaseShield
{
    [Constructible]
    public Buckler() : base(0x1B73) => Weight = 5.0;

    public override int BasePhysicalResistance => 0;
    public override int BaseFireResistance => 0;
    public override int BaseColdResistance => 0;
    public override int BasePoisonResistance => 1;
    public override int BaseEnergyResistance => 0;

    public override int InitMinHits => 40;
    public override int InitMaxHits => 50;

    public override int AosStrReq => 20;

    public override int ArmorBase => 7;
}
