using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DecoMandrakeRoot2 : Item
{
    [Constructible]
    public DecoMandrakeRoot2() : base(0x18DD)
    {
        Movable = true;
        Stackable = false;
    }
}
