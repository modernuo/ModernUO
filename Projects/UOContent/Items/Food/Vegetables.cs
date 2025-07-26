using ModernUO.Serialization;

namespace Server.Items;

[Flippable(0xc77, 0xc78)]
[SerializationGenerator(0, false)]
public partial class Carrot : Food
{
    [Constructible]
    public Carrot(int amount = 1) : base(0xc78, amount) => FillFactor = 1;

    public override double DefaultWeight => 1.0;
}

[Flippable(0xc7b, 0xc7c)]
[SerializationGenerator(0, false)]
public partial class Cabbage : Food
{
    [Constructible]
    public Cabbage(int amount = 1) : base(0xc7b, amount) => FillFactor = 1;

    public override double DefaultWeight => 1.0;
}

[Flippable(0xc6d, 0xc6e)]
[SerializationGenerator(0, false)]
public partial class Onion : Food
{
    [Constructible]
    public Onion(int amount = 1) : base(0xc6d, amount) => FillFactor = 1;

    public override double DefaultWeight => 1.0;
}

[Flippable(0xc70, 0xc71)]
[SerializationGenerator(0, false)]
public partial class Lettuce : Food
{
    [Constructible]
    public Lettuce(int amount = 1) : base(0xc70, amount) => FillFactor = 1;

    public override double DefaultWeight => 1.0;
}

[Flippable(0xC6A, 0xC6B)]
[SerializationGenerator(0, false)]
public partial class Pumpkin : Food
{
    [Constructible]
    public Pumpkin(int amount = 1) : base(0xC6A, amount) => FillFactor = 8;

    public override double DefaultWeight => 1.0;
}

[SerializationGenerator(0, false)]
public partial class SmallPumpkin : Food
{
    [Constructible]
    public SmallPumpkin(int amount = 1) : base(0xC6C, amount) => FillFactor = 8;

    public override double DefaultWeight => 1.0;
}
