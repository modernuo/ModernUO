using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class HoveringWisp : Item
{
    [Constructible]
    public HoveringWisp() : base(0x2100)
    {
    }

    public override int LabelNumber => 1072881; // hovering wisp
}
