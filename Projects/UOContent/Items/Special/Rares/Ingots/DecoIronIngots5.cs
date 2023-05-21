using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DecoIronIngots5 : Item
{
    [Constructible]
    public DecoIronIngots5() : base(0x1BF3)
    {
        Movable = true;
        Stackable = false;
    }
}
