using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class BronzeShield : BaseShield
{
    [Constructible]
    public BronzeShield() : base(0x1B72) => Weight = 6.0;

    public override int BasePhysicalResistance => 0;
    public override int BaseFireResistance => 0;
    public override int BaseColdResistance => 1;
    public override int BasePoisonResistance => 0;
    public override int BaseEnergyResistance => 0;

    public override int InitMinHits => 25;
    public override int InitMaxHits => 30;

    public override int AosStrReq => 35;

    public override int ArmorBase => 10;
}
