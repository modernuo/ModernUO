using ModernUO.Serialization;

namespace Server.Items;

// It isn't flippable
[SerializationGenerator(0)]
public partial class Globe : Item
{
    [Constructible]
    public Globe() : base(0x1047)
    {
    }

    public override double DefaultWeight => 3.0;
}
