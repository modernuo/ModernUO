using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class Harp : BaseInstrument
{
    [Constructible]
    public Harp() : base(0xEB1, 0x43, 0x44) => Weight = 35.0;
}
