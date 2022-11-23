using ModernUO.Serialization;

namespace Server.Items;

[Flippable(0x2FB7, 0x3171)]
[SerializationGenerator(0)]
public partial class ElvenQuiver : BaseQuiver
{
    [Constructible]
    public ElvenQuiver() => WeightReduction = 30;

    public override int LabelNumber => 1032657; // elven quiver
}
