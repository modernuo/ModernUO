using ModernUO.Serialization;

namespace Server.Items;

[Flippable(0x104D, 0x104E)]
[SerializationGenerator(0, false)]
public partial class ClockFrame : Item
{
    [Constructible]
    public ClockFrame(int amount = 1) : base(0x104D)
    {
        Stackable = true;
        Amount = amount;
        Weight = 2.0;
    }
}
