using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DecoIronIngots3 : Item
{
    [Constructible]
    public DecoIronIngots3() : base(0x1BF0)
    {
        Movable = true;
        Stackable = false;
    }
}
