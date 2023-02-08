using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class QuiverOfFire : ElvenQuiver
{
    [Constructible]
    public QuiverOfFire() => Hue = 0x4E7;

    public override int LabelNumber => 1073109; // quiver of fire

    public override void AlterBowDamage(
        ref int phys, ref int fire, ref int cold, ref int pois, ref int nrgy,
        ref int chaos, ref int direct
    )
    {
        cold = pois = nrgy = chaos = direct = 0;
        phys = fire = 50;
    }
}
