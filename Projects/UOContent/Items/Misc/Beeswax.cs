using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class Beeswax : Item
{
    [Constructible]
    public Beeswax(int amount = 1) : base(0x1422)
    {
        Stackable = true;
        Amount = amount;
    }

    public override double DefaultWeight => 1.0;
}
