using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class SalivasFeather : Item
{
    [Constructible]
    public SalivasFeather() : base(0x1020)
    {
        LootType = LootType.Blessed;
        Hue = 0x5C;
    }

    public override int LabelNumber => 1074234; // Saliva's Feather
}
