using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class Emerald : Item
{
    [Constructible]
    public Emerald(int amount = 1) : base(0xF10)
    {
        Stackable = true;
        Amount = amount;
    }

    public override double DefaultWeight => 0.1;
}
