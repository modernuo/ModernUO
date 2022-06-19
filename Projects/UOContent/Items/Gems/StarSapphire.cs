using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class StarSapphire : Item
{
    [Constructible]
    public StarSapphire(int amount = 1) : base(0xF21)
    {
        Stackable = true;
        Amount = amount;
    }

    public override double DefaultWeight => 0.1;
}
