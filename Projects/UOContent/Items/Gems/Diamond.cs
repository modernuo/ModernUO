using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class Diamond : Item
{
    [Constructible]
    public Diamond(int amount = 1) : base(0xF26)
    {
        Stackable = true;
        Amount = amount;
    }

    public override double DefaultWeight => 0.1;
}
