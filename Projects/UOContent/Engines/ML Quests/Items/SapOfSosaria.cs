using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class SapOfSosaria : Item
{
    [Constructible]
    public SapOfSosaria(int amount = 1) : base(0x1848)
    {
        LootType = LootType.Blessed;
        Stackable = true;
        Amount = amount;
    }

    public override int LabelNumber => 1074178; // Sap of Sosaria
}
