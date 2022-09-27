using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class IolosLute : Lute
{
    [Constructible]
    public IolosLute()
    {
        Hue = 0x47E;
        Slayer = SlayerName.Silver;
        Slayer2 = SlayerName.Exorcism;
    }

    public override int LabelNumber => 1063479;

    public override int InitMinUses => 1600;
    public override int InitMaxUses => 1600;
}
