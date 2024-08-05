using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator( 0 )]
[Flippable( 0x2B0E, 0x2B0F )]
public partial class HonestyGorget : BaseArmor
{
    [Constructible]
    public HonestyGorget()
        : base( 0x2B0E )
    {
        LootType = LootType.Blessed;
        Weight = 2.0;
        SetHue = 0;
        Hue = 0x226;

        SetSelfRepair = 5;

        SetPhysicalBonus = 5;
        SetFireBonus = 5;
        SetColdBonus = 5;
        SetPoisonBonus = 5;
        SetEnergyBonus = 5;

        StrRequirement = 45;
    }

    public HonestyGorget( Serial serial )
        : base( serial )
    {
    }

    public override bool IsArtifact => true;

    public override int LabelNumber => 1075189; // Gorget of Honesty (Virtue Armor Set)
    public override SetItem SetID => SetItem.Virtue;
    public override int Pieces => 8;
    public override int BasePhysicalResistance => 7;
    public override int BaseFireResistance => 7;
    public override int BaseColdResistance => 9;
    public override int BasePoisonResistance => 5;
    public override int BaseEnergyResistance => 7;
    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;
    public override ArmorMaterialType MaterialType => ArmorMaterialType.Plate;
}
