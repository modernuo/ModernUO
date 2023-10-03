using ModernUO.Serialization;

namespace Server.Items;

[Flippable(0x312D, 0x312E)]
[SerializationGenerator(0, false)]
public partial class SeveredElfEars : Item
{
    [Constructible]
    public SeveredElfEars(int amount = 1) : base(Utility.RandomList(0x312D, 0x312E))
    {
        Stackable = true;
        Amount = amount;
    }
}
