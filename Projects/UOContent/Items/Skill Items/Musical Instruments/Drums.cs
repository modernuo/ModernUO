using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class Drums : BaseInstrument
{
    [Constructible]
    public Drums() : base(0xE9C, 0x38, 0x39) => Weight = 4.0;
}
