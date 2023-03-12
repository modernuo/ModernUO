using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DecoGarlic2 : Item
{
    [Constructible]
    public DecoGarlic2() : base(0x18E2)
    {
        Movable = true;
        Stackable = false;
    }
}
