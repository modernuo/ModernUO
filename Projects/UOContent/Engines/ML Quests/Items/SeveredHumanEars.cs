using ModernUO.Serialization;

namespace Server.Items;

[Flippable(0x312F, 0x3130)]
[SerializationGenerator(0, false)]
public partial class SeveredHumanEars : Item
{
    [Constructible]
    public SeveredHumanEars(int amount = 1) : base(Utility.RandomList(0x312F, 0x3130))
    {
        Stackable = true;
        Amount = amount;
    }
}
