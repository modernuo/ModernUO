using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class RunedPrism : Item
{
    [Constructible]
    public RunedPrism() : base(0x2F57) => Weight = 1.0;

    public override int LabelNumber => 1073465; // runed prism
}
