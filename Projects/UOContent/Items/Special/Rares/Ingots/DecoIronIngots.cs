using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DecoIronIngots : Item
{
    [Constructible]
    public DecoIronIngots() : base(0x1BF1)
    {
        Movable = true;
        Stackable = false;
    }
}
