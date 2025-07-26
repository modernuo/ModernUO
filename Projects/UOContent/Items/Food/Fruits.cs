using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class FruitBasket : Food
{
    [Constructible]
    public FruitBasket() : base(0x993)
    {
        FillFactor = 5;
        Stackable = false;
    }

    public override double DefaultWeight => 2.0;

    public override bool Eat(Mobile from)
    {
        if (!base.Eat(from))
        {
            return false;
        }

        from.AddToBackpack(new Basket());
        return true;
    }
}

[Flippable(0x171f, 0x1720)]
[SerializationGenerator(0, false)]
public partial class Banana : Food
{
    [Constructible]
    public Banana(int amount = 1) : base(0x171f, amount)
    {
        FillFactor = 1;
    }

    public override double DefaultWeight => 1.0;
}

[Flippable(0x1721, 0x1722)]
[SerializationGenerator(0, false)]
public partial class Bananas : Food
{
    [Constructible]
    public Bananas(int amount = 1) : base(0x1721, amount)
    {
        FillFactor = 1;
    }

    public override double DefaultWeight => 1.0;
}

[SerializationGenerator(0, false)]
public partial class SplitCoconut : Food
{
    [Constructible]
    public SplitCoconut(int amount = 1) : base(0x1725, amount)
    {
        FillFactor = 1;
    }

    public override double DefaultWeight => 1.0;
}

[SerializationGenerator(0, false)]
public partial class Lemon : Food
{
    [Constructible]
    public Lemon(int amount = 1) : base(0x1728, amount)
    {
        FillFactor = 1;
    }

    public override double DefaultWeight => 1.0;
}

[SerializationGenerator(0, false)]
public partial class Lemons : Food
{
    [Constructible]
    public Lemons(int amount = 1) : base(0x1729, amount)
    {
        FillFactor = 1;
    }

    public override double DefaultWeight => 1.0;
}

[SerializationGenerator(0, false)]
public partial class Lime : Food
{
    [Constructible]
    public Lime(int amount = 1) : base(0x172a, amount)
    {
        FillFactor = 1;
    }

    public override double DefaultWeight => 1.0;
}

[SerializationGenerator(0, false)]
public partial class Limes : Food
{
    [Constructible]
    public Limes(int amount = 1) : base(0x172B, amount)
    {
        FillFactor = 1;
    }

    public override double DefaultWeight => 1.0;
}

[SerializationGenerator(0, false)]
public partial class Coconut : Food
{
    [Constructible]
    public Coconut(int amount = 1) : base(0x1726, amount)
    {
        FillFactor = 1;
    }

    public override double DefaultWeight => 1.0;
}

[SerializationGenerator(0, false)]
public partial class OpenCoconut : Food
{
    [Constructible]
    public OpenCoconut(int amount = 1) : base(0x1723, amount)
    {
        FillFactor = 1;
    }

    public override double DefaultWeight => 1.0;
}

[SerializationGenerator(0, false)]
public partial class Dates : Food
{
    [Constructible]
    public Dates(int amount = 1) : base(0x1727, amount)
    {
        FillFactor = 1;
    }

    public override double DefaultWeight => 1.0;
}

[SerializationGenerator(0, false)]
public partial class Grapes : Food
{
    [Constructible]
    public Grapes(int amount = 1) : base(0x9D1, amount)
    {
        FillFactor = 1;
    }

    public override double DefaultWeight => 1.0;
}

[SerializationGenerator(0, false)]
public partial class Peach : Food
{
    [Constructible]
    public Peach(int amount = 1) : base(0x9D2, amount)
    {
        FillFactor = 1;
    }

    public override double DefaultWeight => 1.0;
}

[SerializationGenerator(0, false)]
public partial class Pear : Food
{
    [Constructible]
    public Pear(int amount = 1) : base(0x994, amount)
    {
        FillFactor = 1;
    }

    public override double DefaultWeight => 1.0;
}

[SerializationGenerator(0, false)]
public partial class Apple : Food
{
    [Constructible]
    public Apple(int amount = 1) : base(0x9D0, amount)
    {
        FillFactor = 1;
    }

    public override double DefaultWeight => 1.0;
}

[SerializationGenerator(0, false)]
public partial class Watermelon : Food
{
    [Constructible]
    public Watermelon(int amount = 1) : base(0xC5C, amount)
    {
        FillFactor = 5;
    }

    public override double DefaultWeight => 5.0;
}

[SerializationGenerator(0, false)]
public partial class SmallWatermelon : Food
{
    [Constructible]
    public SmallWatermelon(int amount = 1) : base(0xC5D, amount)
    {
        FillFactor = 5;
    }

    public override double DefaultWeight => 5.0;
}

[Flippable(0xc72, 0xc73)]
[SerializationGenerator(0, false)]
public partial class Squash : Food
{
    [Constructible]
    public Squash(int amount = 1) : base(0xc72, amount)
    {
        FillFactor = 1;
    }

    public override double DefaultWeight => 1.0;
}

[Flippable(0xc79, 0xc7a)]
[SerializationGenerator(0, false)]
public partial class Cantaloupe : Food
{
    [Constructible]
    public Cantaloupe(int amount = 1) : base(0xc79, amount)
    {
        FillFactor = 1;
    }

    public override double DefaultWeight => 1.0;
}
