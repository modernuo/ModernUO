using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class EmptyToolKit2 : Item
{
    [Constructible]
    public EmptyToolKit2() : base(0x1EB7)
    {
        Movable = true;
        Stackable = false;
    }
}
