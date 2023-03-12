using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DecoRock : Item
{
    [Constructible]
    public DecoRock() : base(0x1778)
    {
        Movable = true;
        Stackable = false;
    }
}
