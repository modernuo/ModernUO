using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class ResolvesBridle : Item
{
    [Constructible]
    public ResolvesBridle() : base(0x1374)
    {
    }

    public override int LabelNumber => 1074761; // Resolve's Bridle
}
