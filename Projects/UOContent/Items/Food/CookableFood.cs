using ModernUO.Serialization;
using Server.Targeting;

namespace Server.Items;

[SerializationGenerator(0, false)]
public abstract partial class CookableFood : Item
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _cookingLevel;

    public CookableFood(int itemID, int cookingLevel) : base(itemID) => _cookingLevel = cookingLevel;

    public abstract Food Cook();

    public static bool IsHeatSource(object targeted)
    {
        int itemID;

        if (targeted is Item item)
        {
            itemID = item.ItemID;
        }
        else if (targeted is StaticTarget target)
        {
            itemID = target.ItemID;
        }
        else
        {
            return false;
        }

        if (itemID >= 0xDE3 && itemID <= 0xDE9)
        {
            return true; // Campfire
        }

        if (itemID >= 0x461 && itemID <= 0x48E)
        {
            return true; // Sandstone oven/fireplace
        }

        if (itemID >= 0x92B && itemID <= 0x96C)
        {
            return true; // Stone oven/fireplace
        }

        if (itemID == 0xFAC)
        {
            return true; // Firepit
        }

        if (itemID >= 0x184A && itemID <= 0x184C)
        {
            return true; // Heating stand (left)
        }

        if (itemID >= 0x184E && itemID <= 0x1850)
        {
            return true; // Heating stand (right)
        }

        if (itemID >= 0x398C && itemID <= 0x399F)
        {
            return true; // Fire field
        }

        return false;
    }
}

[SerializationGenerator(0, false)]
public partial class RawRibs : CookableFood
{
    [Constructible]
    public RawRibs(int amount = 1) : base(0x9F1, 10)
    {
        Weight = 1.0;
        Stackable = true;
        Amount = amount;
    }

    public override Food Cook() => new Ribs();
}

[SerializationGenerator(0, false)]
public partial class RawLambLeg : CookableFood
{
    [Constructible]
    public RawLambLeg(int amount = 1) : base(0x1609, 10)
    {
        Stackable = true;
        Amount = amount;
    }

    public override Food Cook() => new LambLeg();
}

[SerializationGenerator(0, false)]
public partial class RawChickenLeg : CookableFood
{
    [Constructible]
    public RawChickenLeg() : base(0x1607, 10)
    {
        Weight = 1.0;
        Stackable = true;
    }

    public override Food Cook() => new ChickenLeg();
}

[SerializationGenerator(0, false)]
public partial class RawBird : CookableFood
{
    [Constructible]
    public RawBird(int amount = 1) : base(0x9B9, 10)
    {
        Weight = 1.0;
        Stackable = true;
        Amount = amount;
    }

    public override Food Cook() => new CookedBird();
}

[SerializationGenerator(0, false)]
public partial class UnbakedPeachCobbler : CookableFood
{
    [Constructible]
    public UnbakedPeachCobbler() : base(0x1042, 25) => Weight = 1.0;

    public override int LabelNumber => 1041335; // unbaked peach cobbler

    public override Food Cook() => new PeachCobbler();
}

[SerializationGenerator(0, false)]
public partial class UnbakedFruitPie : CookableFood
{
    [Constructible]
    public UnbakedFruitPie() : base(0x1042, 25) => Weight = 1.0;

    public override int LabelNumber => 1041334; // unbaked fruit pie

    public override Food Cook() => new FruitPie();
}

[SerializationGenerator(0, false)]
public partial class UnbakedMeatPie : CookableFood
{
    [Constructible]
    public UnbakedMeatPie() : base(0x1042, 25) => Weight = 1.0;

    public override int LabelNumber => 1041338; // unbaked meat pie

    public override Food Cook() => new MeatPie();
}

[SerializationGenerator(0, false)]
public partial class UnbakedPumpkinPie : CookableFood
{
    [Constructible]
    public UnbakedPumpkinPie() : base(0x1042, 25) => Weight = 1.0;

    public override int LabelNumber => 1041342; // unbaked pumpkin pie

    public override Food Cook() => new PumpkinPie();
}

[SerializationGenerator(0, false)]
public partial class UnbakedApplePie : CookableFood
{
    [Constructible]
    public UnbakedApplePie() : base(0x1042, 25) => Weight = 1.0;

    public override int LabelNumber => 1041336; // unbaked apple pie

    public override Food Cook() => new ApplePie();
}

[TypeAlias("Server.Items.UncookedPizza")]
[SerializationGenerator(0, false)]
public partial class UncookedCheesePizza : CookableFood
{
    [Constructible]
    public UncookedCheesePizza() : base(0x1083, 20) => Weight = 1.0;

    public override int LabelNumber => 1041341; // uncooked cheese pizza

    public override Food Cook() => new CheesePizza();
}

[SerializationGenerator(0, false)]
public partial class UncookedSausagePizza : CookableFood
{
    [Constructible]
    public UncookedSausagePizza() : base(0x1083, 20) => Weight = 1.0;

    public override int LabelNumber => 1041337; // uncooked sausage pizza

    public override Food Cook() => new SausagePizza();
}

[SerializationGenerator(0, false)]
public partial class UnbakedQuiche : CookableFood
{
    [Constructible]
    public UnbakedQuiche() : base(0x1042, 25) => Weight = 1.0;

    public override int LabelNumber => 1041339; // unbaked quiche

    public override Food Cook() => new Quiche();
}

[SerializationGenerator(0, false)]
public partial class Eggs : CookableFood
{
    [Constructible]
    public Eggs(int amount = 1) : base(0x9B5, 15)
    {
        Weight = 1.0;
        Stackable = true;
        Amount = amount;
    }

    public override Food Cook() => new FriedEggs();
}

[SerializationGenerator(0, false)]
public partial class BrightlyColoredEggs : CookableFood
{
    [Constructible]
    public BrightlyColoredEggs() : base(0x9B5, 15)
    {
        Weight = 0.5;
        Hue = 3 + Utility.Random(20) * 5;
    }

    public override string DefaultName => "brightly colored eggs";

    public override Food Cook() => new FriedEggs();
}

[SerializationGenerator(0, false)]
public partial class EasterEggs : CookableFood
{
    [Constructible]
    public EasterEggs() : base(0x9B5, 15)
    {
        Weight = 0.5;
        Hue = 3 + Utility.Random(20) * 5;
    }

    public override int LabelNumber => 1016105; // Easter Eggs

    public override Food Cook() => new FriedEggs();
}

[SerializationGenerator(0, false)]
public partial class CookieMix : CookableFood
{
    [Constructible]
    public CookieMix() : base(0x103F, 20) => Weight = 1.0;

    public override Food Cook() => new Cookies();
}

[SerializationGenerator(0, false)]
public partial class CakeMix : CookableFood
{
    [Constructible]
    public CakeMix() : base(0x103F, 40) => Weight = 1.0;

    public override int LabelNumber => 1041002; // cake mix

    public override Food Cook() => new Cake();
}

[SerializationGenerator(0, false)]
public partial class RawFishSteak : CookableFood
{
    [Constructible]
    public RawFishSteak(int amount = 1) : base(0x097A, 10)
    {
        Stackable = true;
        Amount = amount;
    }

    public override double DefaultWeight => 0.1;

    public override Food Cook() => new FishSteak();
}
