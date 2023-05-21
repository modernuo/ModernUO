using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DecoMandrake3 : Item
{
    [Constructible]
    public DecoMandrake3() : base(0x18DF)
    {
        Movable = true;
        Stackable = false;
    }
}
