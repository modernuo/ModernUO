using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class OrnamentOfTheMagician : GoldBracelet
{
    [Constructible]
    public OrnamentOfTheMagician()
    {
        Hue = 0x554;
        Attributes.CastRecovery = 3;
        Attributes.CastSpeed = 2;
        Attributes.LowerManaCost = 10;
        Attributes.LowerRegCost = 20;
        Resistances.Energy = 15;
    }

    public override int LabelNumber => 1061105; // Ornament of the Magician
    public override int ArtifactRarity => 11;
}
