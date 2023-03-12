using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class EmptyToolKit : Item
{
    [Constructible]
    public EmptyToolKit() : base(0x1EB6)
    {
        Movable = true;
        Stackable = false;
    }
}
