using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator( 0 )]
[Flippable( 0x2B10, 0x2B11 )]
public partial class SpiritualityHelm : BaseArmor
{
    [Constructible]
    public SpiritualityHelm()
        : base( 0x2B10 )
    {
        LootType = LootType.Blessed;
        Weight = 6.0;
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

    public override int AosStrReq => 25;
    public override int LabelNumber => 1075237; // Helm of Spirituality (Virtue Armor Set)
    public override SetItem SetID => SetItem.Virtue;
    public override int Pieces => 8;
    public override int BasePhysicalResistance => 8;
    public override int BaseFireResistance => 8;
    public override int BaseColdResistance => 7;
    public override int BasePoisonResistance => 9;
    public override int BaseEnergyResistance => 8;
    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;
    public override ArmorMaterialType MaterialType => ArmorMaterialType.Plate;
}
