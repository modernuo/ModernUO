using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class QuiverOfElements : BaseQuiver
{
    [Constructible]
    public QuiverOfElements()
    {
        Hue = 0xEB;
        WeightReduction = 50;
    }

    public override int LabelNumber => 1075040; // Quiver of the Elements

    public override void AlterBowDamage(
        ref int phys, ref int fire, ref int cold, ref int pois, ref int nrgy,
        ref int chaos, ref int direct
    )
    {
        phys = fire = cold = pois = nrgy = direct = 0;
        chaos = 100;
    }
}
