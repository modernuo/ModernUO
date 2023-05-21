using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DecoMandrake2 : Item
{
    [Constructible]
    public DecoMandrake2() : base(0x18E0)
    {
        Movable = true;
        Stackable = false;
    }
}
