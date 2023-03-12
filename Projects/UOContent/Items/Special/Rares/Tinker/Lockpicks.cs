using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class Lockpicks : Item
{
    [Constructible]
    public Lockpicks() : base(Utility.Random(2) + 0x14FD)
    {
        Movable = true;
        Stackable = false;
    }
}
