using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DecoBridle : Item
{
    [Constructible]
    public DecoBridle() : base(0x1374)
    {
        Movable = true;
        Stackable = false;
    }
}
