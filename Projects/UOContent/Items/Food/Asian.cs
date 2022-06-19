using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class Wasabi : Food
{
    [Constructible]
    public Wasabi() : base(0x24E8) => Weight = 1.0;
}

[SerializationGenerator(0, false)]
public partial class WasabiClumps : Food
{
    [Constructible]
    public WasabiClumps() : base(0x24EB)
    {
        Stackable = false;
        Weight = 1.0;
        FillFactor = 2;
    }
}

[SerializationGenerator(0, false)]
public partial class EmptyBentoBox : Item
{
    [Constructible]
    public EmptyBentoBox() : base(0x2834) => Weight = 5.0;

}

[SerializationGenerator(0, false)]
public partial class BentoBox : Food
{
    [Constructible]
    public BentoBox() : base(0x2836)
    {
        Stackable = false;
        Weight = 5.0;
        FillFactor = 2;
    }

    public override bool Eat(Mobile from)
    {
        if (!base.Eat(from))
        {
            return false;
        }

        from.AddToBackpack(new EmptyBentoBox());
        return true;
    }
}

[SerializationGenerator(0, false)]
public partial class SushiRolls : Food
{
    [Constructible]
    public SushiRolls() : base(0x283E)
    {
        Stackable = false;
        Weight = 3.0;
        FillFactor = 2;
    }
}

[SerializationGenerator(0, false)]
public partial class SushiPlatter : Food
{
    [Constructible]
    public SushiPlatter() : base(0x2840)
    {
        Stackable = Core.ML;
        Weight = 3.0;
        FillFactor = 2;
    }
}

[SerializationGenerator(0, false)]
public partial class GreenTeaBasket : Item
{
    [Constructible]
    public GreenTeaBasket() : base(0x284B) => Weight = 10.0;
}

[SerializationGenerator(0, false)]
public partial class GreenTea : Food
{
    [Constructible]
    public GreenTea() : base(0x284C)
    {
        Stackable = false;
        Weight = 4.0;
        FillFactor = 2;
    }
}

[SerializationGenerator(0, false)]
public partial class MisoSoup : Food
{
    [Constructible]
    public MisoSoup() : base(0x284D)
    {
        Stackable = false;
        Weight = 4.0;
        FillFactor = 2;
    }
}

[SerializationGenerator(0, false)]
public partial class WhiteMisoSoup : Food
{
    [Constructible]
    public WhiteMisoSoup() : base(0x284E)
    {
        Stackable = false;
        Weight = 4.0;
        FillFactor = 2;
    }
}

[SerializationGenerator(0, false)]
public partial class RedMisoSoup : Food
{
    [Constructible]
    public RedMisoSoup() : base(0x284F)
    {
        Stackable = false;
        Weight = 4.0;
        FillFactor = 2;
    }
}

[SerializationGenerator(0, false)]
public partial class AwaseMisoSoup : Food
{
    [Constructible]
    public AwaseMisoSoup() : base(0x2850)
    {
        Stackable = false;
        Weight = 4.0;
        FillFactor = 2;
    }
}
