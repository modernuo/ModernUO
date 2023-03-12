using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class BrokenChair : Item
{
    [Constructible]
    public BrokenChair() : base(Utility.Random(2) + 0xC19)
    {
        Movable = true;
        Stackable = false;
    }
}
