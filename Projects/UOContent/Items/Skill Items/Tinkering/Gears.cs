using ModernUO.Serialization;

namespace Server.Items;

[Flippable(0x1053, 0x1054)]
[SerializationGenerator(0, false)]
public partial class Gears : Item
{
    [Constructible]
    public Gears(int amount = 1) : base(0x1053)
    {
        Stackable = true;
        Amount = amount;
    }

    public override double DefaultWeight => 1.0;
}
