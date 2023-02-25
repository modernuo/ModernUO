using ModernUO.Serialization;

namespace Server.Items;

[Flippable(0x9F4, 0x9F5, 0x9A3, 0x9A4)]
[SerializationGenerator(0, false)]
public partial class Fork : Item
{
    [Constructible]
    public Fork() : base(0x9F4) => Weight = 1.0;
}

[SerializationGenerator(0, false)]
public partial class ForkLeft : Item
{
    [Constructible]
    public ForkLeft() : base(0x9F4) => Weight = 1.0;
}

[SerializationGenerator(0, false)]
public partial class ForkRight : Item
{
    [Constructible]
    public ForkRight() : base(0x9F5) => Weight = 1.0;
}

[Flippable(0x9F8, 0x9F9, 0x9C2, 0x9C3)]
[SerializationGenerator(0, false)]
public partial class Spoon : Item
{
    [Constructible]
    public Spoon() : base(0x9F8) => Weight = 1.0;
}

[SerializationGenerator(0, false)]
public partial class SpoonLeft : Item
{
    [Constructible]
    public SpoonLeft() : base(0x9F8) => Weight = 1.0;
}

[SerializationGenerator(0, false)]
public partial class SpoonRight : Item
{
    [Constructible]
    public SpoonRight() : base(0x9F9) => Weight = 1.0;
}

[Flippable(0x9F6, 0x9F7, 0x9A5, 0x9A6)]
[SerializationGenerator(0, false)]
public partial class Knife : Item
{
    [Constructible]
    public Knife() : base(0x9F6) => Weight = 1.0;
}

[SerializationGenerator(0, false)]
public partial class KnifeLeft : Item
{
    [Constructible]
    public KnifeLeft() : base(0x9F6) => Weight = 1.0;
}

[SerializationGenerator(0, false)]
public partial class KnifeRight : Item
{
    [Constructible]
    public KnifeRight() : base(0x9F7) => Weight = 1.0;
}

[SerializationGenerator(0, false)]
public partial class Plate : Item
{
    [Constructible]
    public Plate() : base(0x9D7) => Weight = 1.0;
}
