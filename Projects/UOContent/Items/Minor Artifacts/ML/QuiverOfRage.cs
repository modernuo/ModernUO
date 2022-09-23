using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class QuiverOfRage : BaseQuiver
{
    [Constructible]
    public QuiverOfRage()
    {
        Hue = 0x24C;

        WeightReduction = 25;
        DamageIncrease = 10;
    }

    public override int LabelNumber => 1075038; // Quiver of Rage

    public override void AlterBowDamage(
        ref int phys, ref int fire, ref int cold, ref int pois, ref int nrgy,
        ref int chaos, ref int direct
    )
    {
        chaos = direct = 0;
        phys = fire = cold = pois = nrgy = 20;
    }
}
