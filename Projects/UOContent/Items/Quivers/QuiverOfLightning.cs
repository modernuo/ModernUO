using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class QuiverOfLightning : ElvenQuiver
{
    [Constructible]
    public QuiverOfLightning() => Hue = 0x4F9;

    public override int LabelNumber => 1073112; // Quiver of Lightning

    public override void AlterBowDamage(
        ref int phys, ref int fire, ref int cold, ref int pois, ref int nrgy,
        ref int chaos, ref int direct
    )
    {
        fire = cold = pois = chaos = direct = 0;
        phys = nrgy = 50;
    }
}
