using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
[Flippable(0x2B04, 0x2B05)]
public partial class HumilityCloak : BaseClothing
{
    public override bool IsArtifact => true;

    [Constructible]
    public HumilityCloak()
        : base(0x2B04, Layer.Cloak)
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

    public override int LabelNumber => 1075195; // Cloak of Humility (Virtue Armor Set)

    public override SetItem SetID => SetItem.Virtue;
    public override int Pieces => 8;

    public override int InitMinHits => 0;
    public override int InitMaxHits => 0;

    public override int AosStrReq => 10;
}
