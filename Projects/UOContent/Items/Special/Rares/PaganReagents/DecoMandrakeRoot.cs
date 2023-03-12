using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DecoMandrakeRoot : Item
{
    [Constructible]
    public DecoMandrakeRoot() : base(0x18DE)
    {
        Movable = true;
        Stackable = false;
    }
}
