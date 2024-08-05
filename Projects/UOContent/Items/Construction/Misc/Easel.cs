using ModernUO.Serialization;

namespace Server.Items;

[Furniture]
[Flippable(0xF65, 0xF67, 0xF69)]
[TypeAlias("Server.Items.Easle")]
[SerializationGenerator(0, false)]
public partial class Easel : Item
{
    [Constructible]
    public Easel() : base(0xF65) => Weight = 25.0;
}

[Furniture]
[Flippable(0xF65, 0xF67, 0xF69)]
[TypeAlias("Server.Items.Easle")]
[SerializationGenerator(0, false)]
public partial class EaselEast : Item
{
    [Constructible]
    public EaselEast() : base(0xF67) => Weight = 25.0;
}

[Furniture]
[Flippable(0xF65, 0xF67, 0xF69)]
[TypeAlias("Server.Items.Easle")]
[SerializationGenerator(0, false)]
public partial class EaselNorth : Item
{
    [Constructible]
    public EaselNorth() : base(0xF69) => Weight = 25.0;
}
