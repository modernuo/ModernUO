using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class GamanHorns : Item
{
    [Constructible]
    public GamanHorns(int amount = 1) : base(0x1084)
    {
        LootType = LootType.Blessed;
        Stackable = true;
        Amount = amount;
        Hue = 0x395;
    }

    public override int LabelNumber => 1074557; // Gaman Horns
}
