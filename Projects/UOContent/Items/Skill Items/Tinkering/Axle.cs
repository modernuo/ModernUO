using ModernUO.Serialization;

namespace Server.Items;

[Flippable(0x105B, 0x105C)]
[SerializationGenerator(0, false)]
public partial class Axle : Item
{
    [Constructible]
    public Axle(int amount = 1) : base(0x105B)
    {
        Stackable = true;
        Amount = amount;
        Weight = 1.0;
    }
}
