using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class GlobOfMonstreousInterredGrizzle : Item
{
    [Constructible]
    public GlobOfMonstreousInterredGrizzle() : base(0x2F3)
    {
    }

    public override int LabelNumber => 1072117; // Glob of Monsterous Interred Grizzle
}
