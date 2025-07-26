using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class FertileDirt : Item
{
    [Constructible]
    public FertileDirt(int amount = 1) : base(0xF81)
    {
        Stackable = true;
        Amount = amount;
    }

    public override double DefaultWeight => 1.0;
}
