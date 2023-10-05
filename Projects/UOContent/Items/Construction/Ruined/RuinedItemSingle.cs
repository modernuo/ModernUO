using ModernUO.Serialization;

namespace Server.Items;

[Flippable(0xC10, 0xC11)]
[SerializationGenerator(0, false)]
public partial class RuinedFallenChairA : Item
{
    [Constructible]
    public RuinedFallenChairA() : base(0xC10) => Movable = false;
}

[Flippable(0xC13, 0xC12)]
[SerializationGenerator(0, false)]
public partial class RuinedArmoire : Item
{
    [Constructible]
    public RuinedArmoire() : base(0xC13) => Movable = false;
}

[Flippable(0xC14, 0xC15)]
[SerializationGenerator(0, false)]
public partial class RuinedBookcase : Item
{
    [Constructible]
    public RuinedBookcase() : base(0xC14) => Movable = false;
}

[SerializationGenerator(0, false)]
public partial class RuinedBooks : Item
{
    [Constructible]
    public RuinedBooks() : base(0xC16) => Movable = false;
}

[Flippable(0xC17, 0xC18)]
[SerializationGenerator(0, false)]
public partial class CoveredChair : Item
{
    [Constructible]
    public CoveredChair() : base(0xC17) => Movable = false;
}

[Flippable(0xC19, 0xC1A)]
[SerializationGenerator(0, false)]
public partial class RuinedFallenChairB : Item
{
    [Constructible]
    public RuinedFallenChairB() : base(0xC19) => Movable = false;
}

[Flippable(0xC1B, 0xC1C, 0xC1E, 0xC1D)]
[SerializationGenerator(0, false)]
public partial class RuinedChair : Item
{
    [Constructible]
    public RuinedChair() : base(0xC1B) => Movable = false;
}

[SerializationGenerator(0, false)]
public partial class RuinedClock : Item
{
    [Constructible]
    public RuinedClock() : base(0xC1F) => Movable = false;
}

[Flippable(0xC24, 0xC25)]
[SerializationGenerator(0, false)]
public partial class RuinedDrawers : Item
{
    [Constructible]
    public RuinedDrawers() : base(0xC24) => Movable = false;
}

[SerializationGenerator(0, false)]
public partial class RuinedPainting : Item
{
    [Constructible]
    public RuinedPainting() : base(0xC2C) => Movable = false;
}

[Flippable(0xC2D, 0xC2F, 0xC2E, 0xC30)]
[SerializationGenerator(0, false)]
public partial class WoodDebris : Item
{
    [Constructible]
    public WoodDebris() : base(0xC2D) => Movable = false;
}
