using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class Wasabi : Food
{
    [Constructible]
    public Wasabi() : base(0x24E8)
    {
    }

    public override double DefaultWeight => 1.0;
}

[SerializationGenerator(0, false)]
public partial class WasabiClumps : Food
{
    [Constructible]
    public WasabiClumps() : base(0x24EB)
    {
        Stackable = false;
        FillFactor = 2;
    }

    public override double DefaultWeight => 1.0;
}

[SerializationGenerator(0, false)]
public partial class EmptyBentoBox : Item
{
    [Constructible]
    public EmptyBentoBox() : base(0x2834)
    {
    }

    public override double DefaultWeight => 5.0;
}

[SerializationGenerator(0, false)]
public partial class BentoBox : Food
{
    [Constructible]
    public BentoBox() : base(0x2836)
    {
        Stackable = false;
        FillFactor = 2;
    }

    public override double DefaultWeight => 5.0;
}

[SerializationGenerator(0, false)]
public partial class SushiRolls : Food
{
    [Constructible]
    public SushiRolls() : base(0x283E)
    {
        Stackable = false;
        FillFactor = 2;
    }

    public override double DefaultWeight => 3.0;
}

[SerializationGenerator(0, false)]
public partial class SushiPlatter : Food
{
    [Constructible]
    public SushiPlatter() : base(0x2840)
    {
        Stackable = Core.ML;
        FillFactor = 2;
    }

    public override double DefaultWeight => 3.0;
}

[SerializationGenerator(0, false)]
public partial class GreenTeaBasket : Item
{
    [Constructible]
    public GreenTeaBasket() : base(0x284B)
    {
    }

    public override double DefaultWeight => 10.0;
}

[SerializationGenerator(0, false)]
public partial class GreenTea : Food
{
    [Constructible]
    public GreenTea() : base(0x284C)
    {
        Stackable = false;
        FillFactor = 2;
    }

    public override double DefaultWeight => 4.0;
}

[SerializationGenerator(0, false)]
public partial class MisoSoup : Food
{
    [Constructible]
    public MisoSoup() : base(0x284D)
    {
        Stackable = false;
        FillFactor = 2;
    }

    public override double DefaultWeight => 4.0;
}

[SerializationGenerator(0, false)]
public partial class WhiteMisoSoup : Food
{
    [Constructible]
    public WhiteMisoSoup() : base(0x284E)
    {
        Stackable = false;
        FillFactor = 2;
    }

    public override double DefaultWeight => 4.0;
}

[SerializationGenerator(0, false)]
public partial class RedMisoSoup : Food
{
    [Constructible]
    public RedMisoSoup() : base(0x284F)
    {
        Stackable = false;
        FillFactor = 2;
    }

    public override double DefaultWeight => 4.0;
}

[SerializationGenerator(0, false)]
public partial class AwaseMisoSoup : Food
{
    [Constructible]
    public AwaseMisoSoup() : base(0x2850)
    {
        Stackable = false;
        FillFactor = 2;
    }

    public override double DefaultWeight => 4.0;
}
