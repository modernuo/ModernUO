using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class EmptyWoodenBowl : Item
{
    [Constructible]
    public EmptyWoodenBowl() : base(0x15F8)
    {
    }

    public override double DefaultWeight => 1.0;
}

[SerializationGenerator(0, false)]
public partial class EmptyPewterBowl : Item
{
    [Constructible]
    public EmptyPewterBowl() : base(0x15FD)
    {
    }

    public override double DefaultWeight => 1.0;
}

[SerializationGenerator(0, false)]
public partial class WoodenBowlOfCarrots : Food
{
    [Constructible]
    public WoodenBowlOfCarrots() : base(0x15F9)
    {
        Stackable = false;
        FillFactor = 2;
    }

    public override double DefaultWeight => 1.0;

    public override bool Eat(Mobile from)
    {
        if (!base.Eat(from))
        {
            return false;
        }

        from.AddToBackpack(new EmptyWoodenBowl());
        return true;
    }
}

[SerializationGenerator(0, false)]
public partial class WoodenBowlOfCorn : Food
{
    [Constructible]
    public WoodenBowlOfCorn() : base(0x15FA)
    {
        Stackable = false;
        FillFactor = 2;
    }

    public override double DefaultWeight => 1.0;

    public override bool Eat(Mobile from)
    {
        if (!base.Eat(from))
        {
            return false;
        }

        from.AddToBackpack(new EmptyWoodenBowl());
        return true;
    }
}

[SerializationGenerator(0, false)]
public partial class WoodenBowlOfLettuce : Food
{
    [Constructible]
    public WoodenBowlOfLettuce() : base(0x15FB)
    {
        Stackable = false;
        FillFactor = 2;
    }

    public override double DefaultWeight => 1.0;

    public override bool Eat(Mobile from)
    {
        if (!base.Eat(from))
        {
            return false;
        }

        from.AddToBackpack(new EmptyWoodenBowl());
        return true;
    }
}

[SerializationGenerator(0, false)]
public partial class WoodenBowlOfPeas : Food
{
    [Constructible]
    public WoodenBowlOfPeas() : base(0x15FC)
    {
        Stackable = false;
        FillFactor = 2;
    }

    public override double DefaultWeight => 1.0;

    public override bool Eat(Mobile from)
    {
        if (!base.Eat(from))
        {
            return false;
        }

        from.AddToBackpack(new EmptyWoodenBowl());
        return true;
    }
}

[SerializationGenerator(0, false)]
public partial class PewterBowlOfCarrots : Food
{
    [Constructible]
    public PewterBowlOfCarrots() : base(0x15FE)
    {
        Stackable = false;
        FillFactor = 2;
    }

    public override double DefaultWeight => 1.0;

    public override bool Eat(Mobile from)
    {
        if (!base.Eat(from))
        {
            return false;
        }

        from.AddToBackpack(new EmptyPewterBowl());
        return true;
    }
}

[SerializationGenerator(0, false)]
public partial class PewterBowlOfCorn : Food
{
    [Constructible]
    public PewterBowlOfCorn() : base(0x15FF)
    {
        Stackable = false;
        FillFactor = 2;
    }

    public override double DefaultWeight => 1.0;

    public override bool Eat(Mobile from)
    {
        if (!base.Eat(from))
        {
            return false;
        }

        from.AddToBackpack(new EmptyPewterBowl());
        return true;
    }
}

[SerializationGenerator(0, false)]
public partial class PewterBowlOfLettuce : Food
{
    [Constructible]
    public PewterBowlOfLettuce() : base(0x1600)
    {
        Stackable = false;
        FillFactor = 2;
    }

    public override double DefaultWeight => 1.0;

    public override bool Eat(Mobile from)
    {
        if (!base.Eat(from))
        {
            return false;
        }

        from.AddToBackpack(new EmptyPewterBowl());
        return true;
    }
}

[SerializationGenerator(0, false)]
public partial class PewterBowlOfPeas : Food
{
    [Constructible]
    public PewterBowlOfPeas() : base(0x1601)
    {
        Stackable = false;
        FillFactor = 2;
    }

    public override double DefaultWeight => 1.0;

    public override bool Eat(Mobile from)
    {
        if (!base.Eat(from))
        {
            return false;
        }

        from.AddToBackpack(new EmptyPewterBowl());
        return true;
    }
}

[SerializationGenerator(0, false)]
public partial class PewterBowlOfPotatos : Food
{
    [Constructible]
    public PewterBowlOfPotatos() : base(0x1602)
    {
        Stackable = false;
        FillFactor = 2;
    }

    public override double DefaultWeight => 1.0;

    public override bool Eat(Mobile from)
    {
        if (!base.Eat(from))
        {
            return false;
        }

        from.AddToBackpack(new EmptyPewterBowl());
        return true;
    }
}

[TypeAlias("Server.Items.EmptyLargeWoodenBowl")]
[SerializationGenerator(0, false)]
public partial class EmptyWoodenTub : Item
{
    [Constructible]
    public EmptyWoodenTub() : base(0x1605)
    {
    }

    public override double DefaultWeight => 2.0;
}

[TypeAlias("Server.Items.EmptyLargePewterBowl")]
[SerializationGenerator(0, false)]
public partial class EmptyPewterTub : Item
{
    [Constructible]
    public EmptyPewterTub() : base(0x1603)
    {
    }

    public override double DefaultWeight => 2.0;
}

[SerializationGenerator(0, false)]
public partial class WoodenBowlOfStew : Food
{
    [Constructible]
    public WoodenBowlOfStew() : base(0x1604)
    {
        Stackable = false;
        FillFactor = 2;
    }

    public override double DefaultWeight => 2.0;

    public override bool Eat(Mobile from)
    {
        if (!base.Eat(from))
        {
            return false;
        }

        from.AddToBackpack(new EmptyWoodenTub());
        return true;
    }
}

[SerializationGenerator(0, false)]
public partial class WoodenBowlOfTomatoSoup : Food
{
    [Constructible]
    public WoodenBowlOfTomatoSoup() : base(0x1606)
    {
        Stackable = false;
        FillFactor = 2;
    }

    public override double DefaultWeight => 2.0;

    public override bool Eat(Mobile from)
    {
        if (!base.Eat(from))
        {
            return false;
        }

        from.AddToBackpack(new EmptyWoodenTub());
        return true;
    }
}
