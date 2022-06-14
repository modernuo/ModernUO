using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class Citrine : Item
{
    [Constructible]
    public Citrine(int amount = 1) : base(0xF15)
    {
        Stackable = true;
        Amount = amount;
    }

    public override double DefaultWeight => 0.1;
}
