using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator( 0 )]
[Flippable( 0x2B0C, 0x2B0D )]
public partial class ValorGauntlets : BaseArmor
{
    [Constructible]
    public ValorGauntlets()
        : base( 0x2B0C )
    {
        LootType = LootType.Blessed;
        Weight = 4.0;
        SetHue = 0;
        Hue = 0x226;

        SetSelfRepair = 5;

        SetPhysicalBonus = 5;
        SetFireBonus = 5;
        SetColdBonus = 5;
        SetPoisonBonus = 5;
        SetEnergyBonus = 5;
    }

    public override bool IsArtifact => true;

    public override int LabelNumber => 1075238; // Gauntlets of Valor (Virtue Armor Set)
    public override SetItem SetID => SetItem.Virtue;
    public override int Pieces => 8;
    public override int BasePhysicalResistance => 6;
    public override int BaseFireResistance => 6;
    public override int BaseColdResistance => 8;
    public override int BasePoisonResistance => 9;
    public override int BaseEnergyResistance => 6;
    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;
    public override int AosStrReq => 50;
    public override ArmorMaterialType MaterialType => ArmorMaterialType.Plate;
}
