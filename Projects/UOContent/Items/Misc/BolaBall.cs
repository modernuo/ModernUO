using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class BolaBall : Item
{
    [Constructible]
    public BolaBall(int amount = 1) : base(0xE73)
    {
        Weight = 4.0;
        Stackable = true;
        Amount = amount;
        Hue = 0x8AC;
    }
}
