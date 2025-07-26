using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class PottedPlant : Item
{
    [Constructible]
    public PottedPlant() : base(0x11CA)
    {
    }

    public override double DefaultWeight => 100;
}

[SerializationGenerator(0, false)]
public partial class PottedPlant1 : Item
{
    [Constructible]
    public PottedPlant1() : base(0x11CB)
    {
    }

    public override double DefaultWeight => 100;
}

[SerializationGenerator(0, false)]
public partial class PottedPlant2 : Item
{
    [Constructible]
    public PottedPlant2() : base(0x11CC)
    {
    }

    public override double DefaultWeight => 100;
}
