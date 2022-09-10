using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class SeahorseStatuette : MonsterStatuette
{
    private static int[] _hues = { 0, 0x482, 0x489, 0x495, 0x4F2 };
    [Constructible]
    public SeahorseStatuette() : base(MonsterStatuetteType.Seahorse)
    {
        LootType = LootType.Regular;
        Hue = _hues.RandomElement();
    }
}
