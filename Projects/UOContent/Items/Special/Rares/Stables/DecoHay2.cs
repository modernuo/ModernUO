using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DecoHay2 : Item
{
    [Constructible]
    public DecoHay2() : base(0xF34)
    {
        Movable = true;
        Stackable = false;
    }
}
