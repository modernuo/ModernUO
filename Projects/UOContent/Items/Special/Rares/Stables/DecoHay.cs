using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DecoHay : Item
{
    [Constructible]
    public DecoHay() : base(0xF35)
    {
        Movable = true;
        Stackable = false;
    }
}
