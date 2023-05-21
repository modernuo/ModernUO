using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class LeggingsOfEmbers : PlateLegs
{
    [Constructible]
    public LeggingsOfEmbers()
    {
        Hue = 0x2C;
        LootType = LootType.Blessed;

        ArmorAttributes.SelfRepair = 10;
        ArmorAttributes.MageArmor = 1;
        ArmorAttributes.LowerStatReq = 100;
    }

    public override int LabelNumber => 1062911; // Royal Leggings of Embers

    public override int BasePhysicalResistance => 15;
    public override int BaseFireResistance => 25;
    public override int BaseColdResistance => 0;
    public override int BasePoisonResistance => 15;
    public override int BaseEnergyResistance => 15;

    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;
}
