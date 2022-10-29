using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class CrystallineFragments : Item
{
    [Constructible]
    public CrystallineFragments() : base(0x223B)
    {
        LootType = LootType.Blessed;
        Hue = 0x47E;
    }

    public override int LabelNumber => 1073160; // Crystalline Fragments
}
