using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class Obelisk : Item
{
    [Constructible]
    public Obelisk() : base(0x1184) => Movable = false;

    public override int LabelNumber => 1016474; // an obelisk
}
