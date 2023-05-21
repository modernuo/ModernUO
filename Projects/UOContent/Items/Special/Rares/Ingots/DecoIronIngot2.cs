using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DecoIronIngot2 : Item
{
    [Constructible]
    public DecoIronIngot2() : base(0x1BEF)
    {
        Movable = true;
        Stackable = false;
    }
}
