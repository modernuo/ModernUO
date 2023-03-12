using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DecoGarlic : Item
{
    [Constructible]
    public DecoGarlic() : base(0x18E1)
    {
        Movable = true;
        Stackable = false;
    }
}
