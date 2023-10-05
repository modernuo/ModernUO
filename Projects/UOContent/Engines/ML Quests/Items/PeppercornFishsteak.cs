using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class PeppercornFishsteak : FishSteak
{
    [Constructible]
    public PeppercornFishsteak() => Hue = 0x222;

    public override int LabelNumber => 1075557; // peppercorn fishsteak
}
