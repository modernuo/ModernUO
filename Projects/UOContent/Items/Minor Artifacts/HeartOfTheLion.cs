using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class HeartOfTheLion : PlateChest
{
    [Constructible]
    public HeartOfTheLion()
    {
        Hue = 0x501;
        Attributes.Luck = 95;
        Attributes.DefendChance = 15;
        ArmorAttributes.LowerStatReq = 100;
        ArmorAttributes.MageArmor = 1;
    }

    public override int LabelNumber => 1070817; // Heart of the Lion

    public override int BasePhysicalResistance => 15;
    public override int BaseFireResistance => 10;
    public override int BaseColdResistance => 10;
    public override int BasePoisonResistance => 10;
    public override int BaseEnergyResistance => 10;

    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;
}
