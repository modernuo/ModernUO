using ModernUO.Serialization;

namespace Server.Items;

[Flippable(0x2B0C, 0x2B0D)]
[SerializationGenerator(0)]
public partial class ValorGauntlets : BaseArmor
{
    [Constructible]
    public ValorGauntlets() : base(0x2B0C)
    {
        LootType = LootType.Blessed;
        Weight = 4.0;
        Hue = 0x226;

        ArmorAttributes.SelfRepair = 5;
    }

    public override int LabelNumber => 1075238; // Gauntlets of Valor (Virtue Armor Set)

    public override int BasePhysicalResistance => 11;

    public override int BaseFireResistance => 11;

    public override int BaseColdResistance => 13;

    public override int BasePoisonResistance => 14;

    public override int BaseEnergyResistance => 10;

    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;
    public override int AosStrReq => 50;

    public override ArmorMaterialType MaterialType => ArmorMaterialType.Plate;
}
