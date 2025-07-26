using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class ExecutionersCap : Item
{
    [Constructible]
    public ExecutionersCap() : base(0xF83)
    {
    }

    public override double DefaultWeight => 1.0;
}
