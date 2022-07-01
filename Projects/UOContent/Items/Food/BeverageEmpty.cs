using ModernUO.Serialization;

namespace Server.Items;

[Flippable(0x1f81, 0x1f82, 0x1f83, 0x1f84)]
[SerializationGenerator(0, false)]
public partial class Glass : Item
{
    [Constructible]
    public Glass() : base(0x1f81) => Weight = 0.1;
}

[SerializationGenerator(0, false)]
public partial class GlassBottle : Item
{
    [Constructible]
    public GlassBottle() : base(0xe2b) => Weight = 0.3;
}
