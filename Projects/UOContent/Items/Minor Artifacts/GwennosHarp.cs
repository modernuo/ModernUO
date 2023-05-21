using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class GwennosHarp : LapHarp
{
    [Constructible]
    public GwennosHarp()
    {
        Hue = 0x47E;
        Slayer = SlayerName.Repond;
        Slayer2 = SlayerName.ReptilianDeath;
    }

    public override int LabelNumber => 1063480;

    public override int InitMinUses => 1600;
    public override int InitMaxUses => 1600;
}
