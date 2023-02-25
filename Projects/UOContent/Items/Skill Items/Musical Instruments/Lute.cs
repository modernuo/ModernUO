using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class Lute : BaseInstrument
{
    [Constructible]
    public Lute() : base(0xEB3, 0x4C, 0x4D) => Weight = 5.0;
}
