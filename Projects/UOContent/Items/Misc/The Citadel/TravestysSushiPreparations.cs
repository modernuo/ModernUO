using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class TravestysSushiPreparations : Item
{
    [Constructible]
    public TravestysSushiPreparations() : base(Utility.Random(0x1E15, 2))
    {
    }

    public override int LabelNumber => 1075093; // Travesty's Sushi Preparations
}
