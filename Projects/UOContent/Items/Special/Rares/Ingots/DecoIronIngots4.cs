using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DecoIronIngots4 : Item
{
    [Constructible]
    public DecoIronIngots4() : base(0x1BF1)
    {
        Movable = true;
        Stackable = false;
    }
}
