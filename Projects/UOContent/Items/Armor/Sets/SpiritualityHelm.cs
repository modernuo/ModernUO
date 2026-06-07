using ModernUO.Serialization;

namespace Server.Items;

[Flippable(0x2B10, 0x2B11)]
[SerializationGenerator(0)]
public partial class SpiritualityHelm : BaseArmor
{
    [Constructible]
    public SpiritualityHelm() : base(0x2B10)
    {
        LootType = LootType.Blessed;
        Weight = 6.0;
        Hue = 0x226;

        ArmorAttributes.SelfRepair = 5;
    }

    public override int LabelNumber => 1075237; // Helm of Spirituality (Virtue Armor Set)
    public override int BasePhysicalResistance => 13;
    public override int BaseFireResistance => 13;
    public override int BaseColdResistance => 12;
    public override int BasePoisonResistance => 14;
    public override int BaseEnergyResistance => 13;

    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;
    public override int AosStrReq => 25;

    public override ArmorMaterialType MaterialType => ArmorMaterialType.Plate;
}
