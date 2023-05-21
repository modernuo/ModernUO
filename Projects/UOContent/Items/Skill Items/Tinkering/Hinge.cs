using ModernUO.Serialization;

namespace Server.Items;

[Flippable(0x1055, 0x1056)]
[SerializationGenerator(0, false)]
public partial class Hinge : Item
{
    [Constructible]
    public Hinge(int amount = 1) : base(0x1055)
    {
        Stackable = true;
        Amount = amount;
        Weight = 1.0;
    }
}
