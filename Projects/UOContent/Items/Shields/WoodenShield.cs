using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class WoodenShield : BaseShield
{
    [Constructible]
    public WoodenShield() : base(0x1B7A) => Weight = 5.0;

    public override int BasePhysicalResistance => 0;
    public override int BaseFireResistance => 0;
    public override int BaseColdResistance => 0;
    public override int BasePoisonResistance => 0;
    public override int BaseEnergyResistance => 1;

    public override int InitMinHits => 20;
    public override int InitMaxHits => 25;

    public override int AosStrReq => 20;

    public override int ArmorBase => 8;
}
