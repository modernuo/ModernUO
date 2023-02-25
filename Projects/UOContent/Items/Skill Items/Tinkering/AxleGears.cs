using ModernUO.Serialization;

namespace Server.Items;

[Flippable(0x1051, 0x1052)]
[SerializationGenerator(0, false)]
public partial class AxleGears : Item
{
    [Constructible]
    public AxleGears(int amount = 1) : base(0x1051)
    {
        Stackable = true;
        Amount = amount;
        Weight = 1.0;
    }
}
