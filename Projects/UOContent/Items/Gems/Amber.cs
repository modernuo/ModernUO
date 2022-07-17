using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class Amber : Item
{
    [Constructible]
    public Amber(int amount = 1) : base(0xF25)
    {
        Stackable = true;
        Amount = amount;
    }

    public override double DefaultWeight => 0.1;
}
