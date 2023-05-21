using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class Aegis : HeaterShield
{
    [Constructible]
    public Aegis()
    {
        Hue = 0x47E;
        ArmorAttributes.SelfRepair = 5;
        Attributes.ReflectPhysical = 15;
        Attributes.DefendChance = 15;
        Attributes.LowerManaCost = 8;
    }

    public override int LabelNumber => 1061602; // Ægis
    public override int ArtifactRarity => 11;

    public override int BasePhysicalResistance => 15;

    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;
}
