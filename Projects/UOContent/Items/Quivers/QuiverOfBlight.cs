using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class QuiverOfBlight : ElvenQuiver
{
    [Constructible]
    public QuiverOfBlight() => Hue = 0x4F3;

    public override int LabelNumber => 1073111; // Quiver of Blight

    public override void AlterBowDamage(
        ref int phys, ref int fire, ref int cold, ref int pois, ref int nrgy,
        ref int chaos, ref int direct
    )
    {
        phys = fire = nrgy = chaos = direct = 0;
        cold = pois = 50;
    }
}
