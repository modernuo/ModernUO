using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DecoIronIngots2 : Item
{
    [Constructible]
    public DecoIronIngots2() : base(0x1BF0)
    {
        Movable = true;
        Stackable = false;
    }
}
