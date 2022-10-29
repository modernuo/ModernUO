using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class TaintedMushroom : Item
{
    [Constructible]
    public TaintedMushroom() : base(Utility.RandomMinMax(0x222E, 0x2231))
    {
    }

    public override int LabelNumber => 1075088; // Dread Horn Tainted Mushroom
    public override bool ForceShowProperties => true;
}
