using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator( 0 )]
[Flippable( 0x2B08, 0x2B09 )]
public partial class JusticeBreastplate : BaseArmor
{
    [Constructible]
    public JusticeBreastplate()
        : base( 0x2B08 )
    {
        LootType = LootType.Blessed;
        Weight = 7.0;
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

    public override int LabelNumber => 1075190; // Breastplate of Justice (Virtue Armor Set)
    public override SetItem SetID => SetItem.Virtue;
    public override int Pieces => 8;
    public override int BasePhysicalResistance => 10;
    public override int BaseFireResistance => 7;
    public override int BaseColdResistance => 8;
    public override int BasePoisonResistance => 7;
    public override int BaseEnergyResistance => 8;
    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;
    public override int AosStrReq => 65;
    public override ArmorMaterialType MaterialType => ArmorMaterialType.Plate;
}
