using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class PolarBearMask : BearMask
{
    [Constructible]
    public PolarBearMask()
    {
        Hue = 0x481;

        ClothingAttributes.SelfRepair = 3;

        Attributes.RegenHits = 2;
        Attributes.NightSight = 1;
    }

    public override int LabelNumber => 1070637;

    public override int BasePhysicalResistance => 15;
    public override int BaseColdResistance => 21;

    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;
}
