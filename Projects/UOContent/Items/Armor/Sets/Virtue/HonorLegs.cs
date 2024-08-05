using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator( 0 )]
[Flippable( 0x2B06, 0x2B07 )]
public partial class HonorLegs : BaseArmor
{
    [Constructible]
    public HonorLegs()
        : base( 0x2B06 )
    {
        LootType = LootType.Blessed;
        Weight = 9.0;
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

    public override int LabelNumber => 1075193; // Legs of Honor (Virtue Armor Set)
    public override SetItem SetID => SetItem.Virtue;
    public override int Pieces => 8;
    public override int BasePhysicalResistance => 8;
    public override int BaseFireResistance => 7;
    public override int BaseColdResistance => 10;
    public override int BasePoisonResistance => 7;
    public override int BaseEnergyResistance => 8;
    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;
    public override ArmorMaterialType MaterialType => ArmorMaterialType.Plate;
    public override int AosStrReq => 70;
}
