using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
[Flippable(0x2B12, 0x2B13)]
public partial class SacrificeSollerets : BaseClothing
{
    [Constructible]
    public SacrificeSollerets()
        : base(0x2B13, Layer.Shoes)
    {
        LootType = LootType.Blessed;
        Weight = 1.0;
        Hue = 0x226;

        SetSelfRepair = 5;
        SetPhysicalBonus = 5;
        SetFireBonus = 5;
        SetColdBonus = 5;
        SetPoisonBonus = 5;
        SetEnergyBonus = 5;
    }

    public override bool IsArtifact => true;

    public override int LabelNumber => 1075194; // Sollerets of Sacrifice (Virtue Armor Set)

    public override SetItem SetID => SetItem.Virtue;
    public override int Pieces => 8;

    public override int InitMinHits => 0;
    public override int InitMaxHits => 0;
    public override int AosStrReq => 10;
}
