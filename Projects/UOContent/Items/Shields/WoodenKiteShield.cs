using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class WoodenKiteShield : BaseShield
{
    [Constructible]
    public WoodenKiteShield() : base(0x1B79)
    {
    }

    public override double DefaultWeight => 5.0;

    public override int BasePhysicalResistance => 0;
    public override int BaseFireResistance => 0;
    public override int BaseColdResistance => 0;
    public override int BasePoisonResistance => 0;
    public override int BaseEnergyResistance => 1;

    public override int InitMinHits => 50;
    public override int InitMaxHits => 65;

    public override int AosStrReq => 20;

    public override int ArmorBase => 12;

    public override ArmorMaterialType MaterialType => ArmorMaterialType.Wood;
}
