using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DecoCards5 : Item
{
    [Constructible]
    public DecoCards5() : base(0xE18)
    {
        Movable = true;
        Stackable = false;
    }
}
