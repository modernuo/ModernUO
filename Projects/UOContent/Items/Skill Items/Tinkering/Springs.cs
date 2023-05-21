using ModernUO.Serialization;

namespace Server.Items;

[Flippable(0x105D, 0x105E)]
[SerializationGenerator(0, false)]
public partial class Springs : Item
{
    [Constructible]
    public Springs(int amount = 1) : base(0x105D)
    {
        Stackable = true;
        Amount = amount;
        Weight = 1.0;
    }
}
