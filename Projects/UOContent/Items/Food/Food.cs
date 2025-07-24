using ModernUO.Serialization;
using Server.Collections;
using Server.ContextMenus;

namespace Server.Items;

[SerializationGenerator(0, false)]
public abstract partial class Food : Item
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Mobile _poisoner;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Poison _poison;

    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _fillFactor;

    public Food(int itemID, int amount = 1) : base(itemID)
    {
        Stackable = true;
        Amount = amount;
        FillFactor = 1;
    }

    public override void GetContextMenuEntries(Mobile from, ref PooledRefList<ContextMenuEntry> list)
    {
        base.GetContextMenuEntries(from, ref list);

        if (from.Alive)
        {
            list.Add(new EatEntry());
        }
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!Movable)
        {
            return;
        }

        if (from.InRange(GetWorldLocation(), 1))
        {
            Eat(from);
        }
    }

    public override bool CanStackWith(Item dropped) =>
        (dropped is not Food food || Poison == food.Poison && Poisoner == food.Poisoner) &&
        base.CanStackWith(dropped);


    public virtual bool Eat(Mobile from)
    {
        // Fill the Mobile with FillFactor
        if (CheckHunger(from))
        {
            // Play a random "eat" sound
            from.PlaySound(Utility.Random(0x3A, 3));

            if (from.Body.IsHuman && !from.Mounted)
            {
                from.Animate(34, 5, 1, true, false, 0);
            }

            if (Poison != null)
            {
                from.ApplyPoison(Poisoner, Poison);
            }

            Consume();
            return true;
        }

        return false;
    }

    public virtual bool CheckHunger(Mobile from) => FillHunger(from, FillFactor);

    public static bool FillHunger(Mobile from, int fillFactor)
    {
        if (from.Hunger >= 20)
        {
            from.SendLocalizedMessage(500867); // You are simply too full to eat any more!
            return false;
        }

        var iHunger = from.Hunger + fillFactor;

        if (from.Stam < from.StamMax)
        {
            from.Stam += Utility.Random(6, 3) + fillFactor / 5;
        }

        if (iHunger >= 20)
        {
            from.Hunger = 20;
            from.SendLocalizedMessage(500872); // You manage to eat the food, but you are stuffed!
        }
        else
        {
            from.Hunger = iHunger;

            if (iHunger < 5)
            {
                from.SendLocalizedMessage(500868); // You eat the food, but are still extremely hungry.
            }
            else if (iHunger < 10)
            {
                from.SendLocalizedMessage(500869); // You eat the food, and begin to feel more satiated.
            }
            else if (iHunger < 15)
            {
                from.SendLocalizedMessage(500870); // After eating the food, you feel much less hungry.
            }
            else
            {
                from.SendLocalizedMessage(500871); // You feel quite full after consuming the food.
            }
        }

        return true;
    }
}

[SerializationGenerator(0, false)]
public partial class BreadLoaf : Food
{
    [Constructible]
    public BreadLoaf(int amount = 1) : base(0x103B, amount)
    {
        FillFactor = 3;
    }

    public override double DefaultWeight => 1.0;
}

[SerializationGenerator(0, false)]
public partial class Bacon : Food
{
    [Constructible]
    public Bacon(int amount = 1) : base(0x979, amount)
    {
        FillFactor = 1;
    }

    public override double DefaultWeight => 1.0;
}

[SerializationGenerator(0, false)]
public partial class SlabOfBacon : Food
{
    [Constructible]
    public SlabOfBacon(int amount = 1) : base(0x976, amount)
    {
        FillFactor = 3;
    }

    public override double DefaultWeight => 1.0;
}

[SerializationGenerator(0, false)]
public partial class FishSteak : Food
{
    [Constructible]
    public FishSteak(int amount = 1) : base(0x97B, amount) => FillFactor = 3;

    public override double DefaultWeight => 0.1;
}

[SerializationGenerator(0, false)]
public partial class CheeseWheel : Food
{
    [Constructible]
    public CheeseWheel(int amount = 1) : base(0x97E, amount) => FillFactor = 3;

    public override double DefaultWeight => 0.1;
}

[SerializationGenerator(0, false)]
public partial class CheeseWedge : Food
{
    [Constructible]
    public CheeseWedge(int amount = 1) : base(0x97D, amount) => FillFactor = 3;

    public override double DefaultWeight => 0.1;
}

[SerializationGenerator(0, false)]
public partial class CheeseSlice : Food
{
    [Constructible]
    public CheeseSlice(int amount = 1) : base(0x97C, amount) => FillFactor = 1;

    public override double DefaultWeight => 0.1;
}

[SerializationGenerator(0, false)]
public partial class FrenchBread : Food
{
    [Constructible]
    public FrenchBread(int amount = 1) : base(0x98C, amount)
    {
        FillFactor = 3;
    }

    public override double DefaultWeight => 2.0;
}

[SerializationGenerator(0, false)]
public partial class FriedEggs : Food
{
    [Constructible]
    public FriedEggs(int amount = 1) : base(0x9B6, amount)
    {
        FillFactor = 4;
    }

    public override double DefaultWeight => 1.0;
}

[SerializationGenerator(0, false)]
public partial class CookedBird : Food
{
    [Constructible]
    public CookedBird(int amount = 1) : base(0x9B7, amount)
    {
        FillFactor = 5;
    }

    public override double DefaultWeight => 1.0;
}

[SerializationGenerator(0, false)]
public partial class RoastPig : Food
{
    [Constructible]
    public RoastPig(int amount = 1) : base(0x9BB, amount)
    {
        FillFactor = 20;
    }

    public override double DefaultWeight => 45.0;
}

[SerializationGenerator(0, false)]
public partial class Sausage : Food
{
    [Constructible]
    public Sausage(int amount = 1) : base(0x9C0, amount)
    {
        FillFactor = 4;
    }

    public override double DefaultWeight => 1.0;
}

[SerializationGenerator(0, false)]
public partial class Ham : Food
{
    [Constructible]
    public Ham(int amount = 1) : base(0x9C9, amount)
    {
        FillFactor = 5;
    }

    public override double DefaultWeight => 1.0;
}

[SerializationGenerator(0, false)]
public partial class Cake : Food
{
    [Constructible]
    public Cake() : base(0x9E9)
    {
        Stackable = false;
        FillFactor = 10;
    }

    public override double DefaultWeight => 1.0;
}

[SerializationGenerator(0, false)]
public partial class Ribs : Food
{
    [Constructible]
    public Ribs(int amount = 1) : base(0x9F2, amount)
    {
        FillFactor = 5;
    }

    public override double DefaultWeight => 1.0;
}

[SerializationGenerator(0, false)]
public partial class Cookies : Food
{
    [Constructible]
    public Cookies() : base(0x160b)
    {
        Stackable = Core.ML;
        FillFactor = 4;
    }

    public override double DefaultWeight => 1.0;
}

[SerializationGenerator(0, false)]
public partial class Muffins : Food
{
    [Constructible]
    public Muffins() : base(0x9eb)
    {
        Stackable = false;
        FillFactor = 4;
    }

    public override double DefaultWeight => 1.0;
}

[TypeAlias("Server.Items.Pizza")]
[SerializationGenerator(0, false)]
public partial class CheesePizza : Food
{
    [Constructible]
    public CheesePizza() : base(0x1040)
    {
        Stackable = false;
        FillFactor = 6;
    }

    public override double DefaultWeight => 1.0;
    public override int LabelNumber => 1044516; // cheese pizza
}

[SerializationGenerator(0, false)]
public partial class SausagePizza : Food
{
    [Constructible]
    public SausagePizza() : base(0x1040)
    {
        Stackable = false;
        FillFactor = 6;
    }

    public override double DefaultWeight => 1.0;
    public override int LabelNumber => 1044517; // sausage pizza
}

[SerializationGenerator(0, false)]
public partial class FruitPie : Food
{
    [Constructible]
    public FruitPie() : base(0x1041)
    {
        Stackable = false;
        FillFactor = 5;
    }

    public override double DefaultWeight => 1.0;
    public override int LabelNumber => 1041346; // baked fruit pie
}

[SerializationGenerator(0, false)]
public partial class MeatPie : Food
{
    [Constructible]
    public MeatPie() : base(0x1041)
    {
        Stackable = false;
        FillFactor = 5;
    }

    public override double DefaultWeight => 1.0;
    public override int LabelNumber => 1041347; // baked meat pie
}

[SerializationGenerator(0, false)]
public partial class PumpkinPie : Food
{
    [Constructible]
    public PumpkinPie() : base(0x1041)
    {
        Stackable = false;
        FillFactor = 5;
    }

    public override double DefaultWeight => 1.0;
    public override int LabelNumber => 1041348; // baked pumpkin pie
}

[SerializationGenerator(0, false)]
public partial class ApplePie : Food
{
    [Constructible]
    public ApplePie() : base(0x1041)
    {
        Stackable = false;
        FillFactor = 5;
    }

    public override double DefaultWeight => 1.0;
    public override int LabelNumber => 1041343; // baked apple pie
}

[SerializationGenerator(0, false)]
public partial class PeachCobbler : Food
{
    [Constructible]
    public PeachCobbler() : base(0x1041)
    {
        Stackable = false;
        FillFactor = 5;
    }

    public override double DefaultWeight => 1.0;
    public override int LabelNumber => 1041344; // baked peach cobbler
}

[SerializationGenerator(0, false)]
public partial class Quiche : Food
{
    [Constructible]
    public Quiche() : base(0x1041)
    {
        Stackable = Core.ML;
        FillFactor = 5;
    }

    public override double DefaultWeight => 1.0;
    public override int LabelNumber => 1041345; // baked quiche
}

[SerializationGenerator(0, false)]
public partial class LambLeg : Food
{
    [Constructible]
    public LambLeg(int amount = 1) : base(0x160a, amount)
    {
        FillFactor = 5;
    }

    public override double DefaultWeight => 2.0;
}

[SerializationGenerator(0, false)]
public partial class ChickenLeg : Food
{
    [Constructible]
    public ChickenLeg(int amount = 1) : base(0x1608, amount)
    {
        FillFactor = 4;
    }

    public override double DefaultWeight => 1.0;
}

[Flippable(0xC74, 0xC75)]
[SerializationGenerator(0, false)]
public partial class HoneydewMelon : Food
{
    [Constructible]
    public HoneydewMelon(int amount = 1) : base(0xC74, amount)
    {
        FillFactor = 1;
    }

    public override double DefaultWeight => 1.0;
}

[Flippable(0xC64, 0xC65)]
[SerializationGenerator(0, false)]
public partial class YellowGourd : Food
{
    [Constructible]
    public YellowGourd(int amount = 1) : base(0xC64, amount)
    {
        FillFactor = 1;
    }

    public override double DefaultWeight => 1.0;
}

[Flippable(0xC66, 0xC67)]
[SerializationGenerator(0, false)]
public partial class GreenGourd : Food
{
    [Constructible]
    public GreenGourd(int amount = 1) : base(0xC66, amount)
    {
        FillFactor = 1;
    }

    public override double DefaultWeight => 1.0;
}

[Flippable(0xC7F, 0xC81)]
[SerializationGenerator(0, false)]
public partial class EarOfCorn : Food
{
    [Constructible]
    public EarOfCorn(int amount = 1) : base(0xC81, amount)
    {
        FillFactor = 1;
    }

    public override double DefaultWeight => 1.0;
}

[SerializationGenerator(0, false)]
public partial class Turnip : Food
{
    [Constructible]
    public Turnip(int amount = 1) : base(0xD3A, amount)
    {
        FillFactor = 1;
    }

    public override double DefaultWeight => 1.0;
}

[SerializationGenerator(0, false)]
public partial class SheafOfHay : Item
{
    [Constructible]
    public SheafOfHay() : base(0xF36)
    {
    }

    public override double DefaultWeight => 10.0;
}
