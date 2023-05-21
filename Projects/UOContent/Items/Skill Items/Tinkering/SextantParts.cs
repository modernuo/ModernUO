using ModernUO.Serialization;

namespace Server.Items;

[Flippable(0x1059, 0x105A)]
[SerializationGenerator(0, false)]
public partial class SextantParts : Item
{
    [Constructible]
    public SextantParts(int amount = 1) : base(0x1059)
    {
        Stackable = true;
        Amount = amount;
        Weight = 2.0;
    }
}
