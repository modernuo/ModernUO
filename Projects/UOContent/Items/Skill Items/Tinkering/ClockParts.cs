using ModernUO.Serialization;

namespace Server.Items;

[Flippable(0x104F, 0x1050)]
[SerializationGenerator(0, false)]
public partial class ClockParts : Item
{
    [Constructible]
    public ClockParts(int amount = 1) : base(0x104F)
    {
        Stackable = true;
        Amount = amount;
        Weight = 1.0;
    }
}
