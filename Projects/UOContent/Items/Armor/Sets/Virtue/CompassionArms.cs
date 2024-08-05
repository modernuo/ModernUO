using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator( 0 )]
[Flippable( 0x2B0A, 0x2B0B )]
public partial class CompassionArms : BaseArmor
{
    [Constructible]
    public CompassionArms()
        : base( 0x2B0A )
    {
        LootType = LootType.Blessed;
        Weight = 3.0;
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

    public override int AosStrReq => 60;
    public override int LabelNumber => 1075191; // Arms of Compassion (Virtue Armor Set)
    public override SetItem SetID => SetItem.Virtue;
    public override int Pieces => 8;
    public override int BasePhysicalResistance => 8;
    public override int BaseFireResistance => 11;
    public override int BaseColdResistance => 6;
    public override int BasePoisonResistance => 8;
    public override int BaseEnergyResistance => 7;
    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;
    public override ArmorMaterialType MaterialType => ArmorMaterialType.Plate;
}
