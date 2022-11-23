using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class QuiverOfIce : ElvenQuiver
{
    [Constructible]
    public QuiverOfIce() => Hue = 0x4ED;

    public override int LabelNumber => 1073110; // quiver of ice

    public override void AlterBowDamage(
        ref int phys, ref int fire, ref int cold, ref int pois, ref int nrgy,
        ref int chaos, ref int direct
    )
    {
        fire = pois = nrgy = chaos = direct = 0;
        phys = cold = 50;
    }
}
