using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class Sapphire : Item
{
    [Constructible]
    public Sapphire(int amount = 1) : base(0xF19)
    {
        Stackable = true;
        Amount = amount;
    }

    public override double DefaultWeight => 0.1;
}
