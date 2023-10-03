using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class UnicornRibs : Item
{
    [Constructible]
    public UnicornRibs(int amount = 1) : base(0x9F1)
    {
        LootType = LootType.Blessed;
        Hue = 0x14B;
        Stackable = true;
        Amount = amount;
    }

    public override int LabelNumber => 1074611; // Unicorn Ribs
}
