using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class TambourineTassel : BaseInstrument
{
    [Constructible]
    public TambourineTassel() : base(0xE9E, 0x52, 0x53)
    {
    }

    public override double DefaultWeight => 1.0;
}
