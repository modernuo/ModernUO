using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class BrightsightLenses : ElvenGlasses
{
    [Constructible]
    public BrightsightLenses()
    {
        Hue = 0x501;

        Attributes.NightSight = 1;
        Attributes.RegenMana = 3;

        ArmorAttributes.SelfRepair = 3;
    }

    public override int LabelNumber => 1075039; // Brightsight Lenses

    public override int BasePhysicalResistance => 9;
    public override int BaseFireResistance => 29;
    public override int BaseColdResistance => 7;
    public override int BasePoisonResistance => 8;
    public override int BaseEnergyResistance => 7;

    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;
}
