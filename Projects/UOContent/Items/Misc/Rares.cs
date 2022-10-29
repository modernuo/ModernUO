using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class Rope : Item
{
    [Constructible]
    public Rope(int amount = 1) : base(0x14F8)
    {
        Stackable = true;
        Weight = 1.0;
        Amount = amount;
    }
}

[SerializationGenerator(0, false)]
public partial class IronWire : Item
{
    [Constructible]
    public IronWire(int amount = 1) : base(0x1876)
    {
        Stackable = true;
        Weight = 5.0;
        Amount = amount;
    }
}

[SerializationGenerator(0, false)]
public partial class SilverWire : Item
{
    [Constructible]
    public SilverWire(int amount = 1) : base(0x1877)
    {
        Stackable = true;
        Weight = 5.0;
        Amount = amount;
    }
}

[SerializationGenerator(0, false)]
public partial class GoldWire : Item
{
    [Constructible]
    public GoldWire(int amount = 1) : base(0x1878)
    {
        Stackable = true;
        Weight = 5.0;
        Amount = amount;
    }
}

[SerializationGenerator(0, false)]
public partial class CopperWire : Item
{
    [Constructible]
    public CopperWire(int amount = 1) : base(0x1879)
    {
        Stackable = true;
        Weight = 5.0;
        Amount = amount;
    }
}

[SerializationGenerator(0, false)]
public partial class WhiteDriedFlowers : Item
{
    [Constructible]
    public WhiteDriedFlowers(int amount = 1) : base(0xC3C)
    {
        Stackable = true;
        Weight = 1.0;
        Amount = amount;
    }
}

[SerializationGenerator(0, false)]
public partial class GreenDriedFlowers : Item
{
    [Constructible]
    public GreenDriedFlowers(int amount = 1) : base(0xC3E)
    {
        Stackable = true;
        Weight = 1.0;
        Amount = amount;
    }
}

[SerializationGenerator(0, false)]
public partial class DriedOnions : Item
{
    [Constructible]
    public DriedOnions(int amount = 1) : base(0xC40)
    {
        Stackable = true;
        Weight = 1.0;
        Amount = amount;
    }
}

[SerializationGenerator(0, false)]
public partial class DriedHerbs : Item
{
    [Constructible]
    public DriedHerbs(int amount = 1) : base(0xC42)
    {
        Stackable = true;
        Weight = 1.0;
        Amount = amount;
    }
}

[SerializationGenerator(0, false)]
public partial class HorseShoes : Item
{
    [Constructible]
    public HorseShoes() : base(0xFB6) => Weight = 3.0;
}

[SerializationGenerator(0, false)]
public partial class ForgedMetal : Item
{
    [Constructible]
    public ForgedMetal() : base(0xFB8) => Weight = 5.0;
}

[SerializationGenerator(0, false)]
public partial class Whip : Item
{
    [Constructible]
    public Whip() : base(0x166E) => Weight = 1.0;
}

[SerializationGenerator(0, false)]
public partial class PaintsAndBrush : Item
{
    [Constructible]
    public PaintsAndBrush() : base(0xFC1) => Weight = 1.0;
}

[SerializationGenerator(0, false)]
public partial class PenAndInk : Item
{
    [Constructible]
    public PenAndInk() : base(0xFBF) => Weight = 1.0;
}

[SerializationGenerator(0)]
public partial class ChiselsNorth : Item
{
    [Constructible]
    public ChiselsNorth() : base(0x1026) => Weight = 1.0;
}

[SerializationGenerator(0)]
public partial class ChiselsWest : Item
{
    [Constructible]
    public ChiselsWest() : base(0x1027) => Weight = 1.0;
}

[SerializationGenerator(0)]
public partial class DirtyPan : Item
{
    [Constructible]
    public DirtyPan() : base(0x9E8) => Weight = 1.0;
}

[SerializationGenerator(0)]
public partial class DirtySmallRoundPot : Item
{
    [Constructible]
    public DirtySmallRoundPot() : base(0x9E7) => Weight = 1.0;
}

[SerializationGenerator(0)]
public partial class DirtyPot : Item
{
    [Constructible]
    public DirtyPot() : base(0x9E6) => Weight = 1.0;
}

[SerializationGenerator(0)]
public partial class DirtyRoundPot : Item
{
    [Constructible]
    public DirtyRoundPot() : base(0x9DF) => Weight = 1.0;
}

[SerializationGenerator(0)]
public partial class DirtyFrypan : Item
{
    [Constructible]
    public DirtyFrypan() : base(0x9DE) => Weight = 1.0;
}

[SerializationGenerator(0)]
public partial class DirtySmallPot : Item
{
    [Constructible]
    public DirtySmallPot() : base(0x9DD) => Weight = 1.0;
}

[SerializationGenerator(0)]
public partial class DirtyKettle : Item
{
    [Constructible]
    public DirtyKettle() : base(0x9DC) => Weight = 1.0;
}
