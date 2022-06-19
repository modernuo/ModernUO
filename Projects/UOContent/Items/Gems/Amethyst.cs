using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class Amethyst : Item
{
    [Constructible]
    public Amethyst(int amount = 1) : base(0xF16)
    {
        Stackable = true;
        Amount = amount;
    }

    public override double DefaultWeight => 0.1;
}
