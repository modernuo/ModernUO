using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class Tambourine : BaseInstrument
{
    [Constructible]
    public Tambourine() : base(0xE9D, 0x52, 0x53)
    {
    }

    public override double DefaultWeight => 1.0;
}
