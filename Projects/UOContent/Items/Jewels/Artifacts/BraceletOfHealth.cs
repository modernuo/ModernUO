using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class BraceletOfHealth : GoldBracelet
{
    [Constructible]
    public BraceletOfHealth()
    {
        Hue = 0x21;
        Attributes.BonusHits = 5;
        Attributes.RegenHits = 10;
    }

    public override int LabelNumber => 1061103; // Bracelet of Health
    public override int ArtifactRarity => 11;
}
