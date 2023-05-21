using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class ClosedBarrel : TrappableContainer
{
    [Constructible]
    public ClosedBarrel() : base(0x0FAE)
    {
    }

    public override int DefaultGumpID => 0x3e;
}
