using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class HeaterShield : BaseShield
{
    [Constructible]
    public HeaterShield() : base(0x1B76) => Weight = 8.0;

    public override int BasePhysicalResistance => 0;
    public override int BaseFireResistance => 1;
    public override int BaseColdResistance => 0;
    public override int BasePoisonResistance => 0;
    public override int BaseEnergyResistance => 0;

    public override int InitMinHits => 50;
    public override int InitMaxHits => 65;

    public override int AosStrReq => 90;

    public override int ArmorBase => 23;
}
