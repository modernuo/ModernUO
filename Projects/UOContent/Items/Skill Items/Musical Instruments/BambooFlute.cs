using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class BambooFlute : BaseInstrument
{
    [Constructible]
    public BambooFlute() : base(0x2805, 0x504, 0x503)
    {
    }

    public override double DefaultWeight => 2.0;
}
