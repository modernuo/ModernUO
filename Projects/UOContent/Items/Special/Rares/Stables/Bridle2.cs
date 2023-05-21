using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DecoBridle2 : Item
{
    [Constructible]
    public DecoBridle2() : base(0x1375)
    {
        Movable = true;
        Stackable = false;
    }
}
