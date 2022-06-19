using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class Tourmaline : Item
{
    [Constructible]
    public Tourmaline(int amount = 1) : base(0xF2D)
    {
        Stackable = true;
        Amount = amount;
    }

    public override double DefaultWeight => 0.1;
}
