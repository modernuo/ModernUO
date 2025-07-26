using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class LapHarp : BaseInstrument
{
    [Constructible]
    public LapHarp() : base(0xEB2, 0x45, 0x46)
    {
    }

    public override double DefaultWeight => 10.0;
}
