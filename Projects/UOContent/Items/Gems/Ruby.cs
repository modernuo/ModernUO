using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class Ruby : Item
{
    [Constructible]
    public Ruby(int amount = 1) : base(0xF13)
    {
        Stackable = true;
        Amount = amount;
    }

    public override double DefaultWeight => 0.1;
}
