using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DecoBrimstone : Item
{
    [Constructible]
    public DecoBrimstone() : base(0xF7F)
    {
        Movable = true;
        Stackable = false;
    }
}
