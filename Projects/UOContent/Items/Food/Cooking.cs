using System;
using ModernUO.Serialization;
using Server.Engines.Craft.T2A;
using Server.Targeting;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class Dough : Item, IT2ACombinable
{
    [Constructible]
    public Dough() : base(0x103d)
    {
        Stackable = Core.ML;
    }

    public override double DefaultWeight => 1.0;

    public override void OnDoubleClick(Mobile from)
    {
        if (T2ACraftSystem.Enabled)
        {
            T2ACooking.Prompt(from, this);
        }
        else
        {
            base.OnDoubleClick(from);
        }
    }

    public bool TryCombine(Mobile from, object target)
    {
        if (CookableFood.IsHeatSource(target))
        {
            return T2ACooking.BeginHeat(from, this, target);
        }

        if (target is not Item item)
        {
            return T2ACooking.CannotCombine(from);
        }

        if (!T2ACooking.ValidateInPack(from, item))
        {
            return false;
        }

        Item result = item switch
        {
            JarHoney                               => new SweetDough(),
            Pear                                   => new UnbakedFruitPie(),
            Peach                                  => new UnbakedPeachCobbler(),
            Apple                                  => new UnbakedApplePie(),
            Pumpkin or SmallPumpkin                => new UnbakedPumpkinPie(),
            Eggs                                   => new UnbakedQuiche(),
            Ham or Spam or CookedBird or FishSteak => new UnbakedMeatPie(),
            Sausage                                => new UncookedSausagePizza(),
            _                                      => null
        };

        if (result == null)
        {
            return T2ACooking.CannotCombine(from);
        }

        return T2ACooking.Combine(
            from,
            this,
            item,
            result,
            item is JarHoney
                ? "You mix the dough with honey to make sweet dough."
                : "You combine the dough with the ingredient."
        );
    }
}

[SerializationGenerator(0, false)]
public partial class SweetDough : Item, IT2ACombinable
{
    [Constructible]
    public SweetDough() : base(0x103d)
    {
        Stackable = Core.ML;
        Hue = 150;
    }

    public override double DefaultWeight => 1.0;
    public override int LabelNumber => 1041340; // sweet dough

    public override void OnDoubleClick(Mobile from)
    {
        if (T2ACraftSystem.Enabled)
        {
            T2ACooking.Prompt(from, this);
        }
        else
        {
            base.OnDoubleClick(from);
        }
    }

    public bool TryCombine(Mobile from, object target)
    {
        if (CookableFood.IsHeatSource(target))
        {
            return T2ACooking.BeginHeat(from, this, target);
        }

        switch (target)
        {
            case JarHoney honey:
                {
                    return T2ACooking.ValidateInPack(from, honey) && T2ACooking.Combine(
                        from,
                        this,
                        honey,
                        new CookieMix(),
                        "You mix the sweet dough with honey to make cookie mix."
                    );
                }
            case SackFlour sack:
                {
                    if (!T2ACooking.ValidateInPack(from, sack))
                    {
                        return false;
                    }

                    if (!sack.IsOpen)
                    {
                        from.SendAsciiMessage("You must open the sack of flour first.");
                        return false;
                    }

                    return T2ACooking.Combine(
                        from,
                        this,
                        sack,
                        new CakeMix(),
                        "You mix the flour with sweet dough to make cake mix."
                    );
                }
            case BowlFlour bowl:
                {
                    return T2ACooking.ValidateInPack(from, bowl) && T2ACooking.Combine(
                        from,
                        this,
                        bowl,
                        new CakeMix(),
                        "You mix the flour with sweet dough to make cake mix."
                    );
                }
            default:
                {
                    return T2ACooking.CannotCombine(from);
                }
        }
    }
}

[SerializationGenerator(0, false)]
public partial class JarHoney : Item, IT2ACombinable
{
    [Constructible]
    public JarHoney() : base(0x9ec)
    {
        Stackable = true;
    }

    public override double DefaultWeight => 1.0;

    public override void OnDoubleClick(Mobile from)
    {
        if (T2ACraftSystem.Enabled)
        {
            T2ACooking.Prompt(from, this);
        }
        else
        {
            base.OnDoubleClick(from);
        }
    }

    public bool TryCombine(Mobile from, object target)
    {
        switch (target)
        {
            case Dough dough:
                {
                    return T2ACooking.ValidateInPack(from, dough) && T2ACooking.Combine(
                        from,
                        this,
                        dough,
                        new SweetDough(),
                        "You mix the dough with honey to make sweet dough."
                    );
                }
            case SweetDough sweetDough:
                {
                    return T2ACooking.ValidateInPack(from, sweetDough) && T2ACooking.Combine(
                        from,
                        this,
                        sweetDough,
                        new CookieMix(),
                        "You mix the sweet dough with honey to make cookie mix."
                    );
                }
            default:
                {
                    return T2ACooking.CannotCombine(from);
                }
        }
    }
}

[SerializationGenerator(0, false)]
public partial class BowlFlour : Item, IT2ACombinable
{
    [Constructible]
    public BowlFlour() : base(0xa1e)
    {
    }

    public override double DefaultWeight => 1.0;

    public override void OnDoubleClick(Mobile from)
    {
        if (T2ACraftSystem.Enabled)
        {
            T2ACooking.Prompt(from, this);
        }
        else
        {
            base.OnDoubleClick(from);
        }
    }

    public bool TryCombine(Mobile from, object target)
    {
        switch (target)
        {
            case SweetDough sweetDough:
                {
                    return T2ACooking.ValidateInPack(from, sweetDough) && T2ACooking.Combine(
                        from,
                        this,
                        sweetDough,
                        new CakeMix(),
                        "You mix the flour with sweet dough to make cake mix."
                    );
                }
            case Item item when T2ACooking.GetWaterSource(item) is { } water:
                {
                    return T2ACooking.CanReachWater(from, item) && T2ACooking.Combine(
                        from,
                        this,
                        water,
                        new Dough(),
                        "You mix the flour with water to make dough."
                    );
                }
            default:
                {
                    return T2ACooking.CannotCombine(from);
                }
        }
    }
}

[SerializationGenerator(0, false)]
public partial class WoodenBowl : Item
{
    [Constructible]
    public WoodenBowl() : base(0x15f8)
    {
    }

    public override double DefaultWeight => 1.0;
}

[TypeAlias("Server.Items.SackFlourOpen")]
[SerializationGenerator(0, false)]
public partial class SackFlour : Item, IHasQuantity, IT2ACombinable
{
    [Constructible]
    public SackFlour() : base(0x1039)
    {
        _quantity = 20;
    }

    public override double DefaultWeight => 5.0;

    [SerializableField(0, fieldChanged: nameof(OnQuantityChanged), allowFieldChange: nameof(AllowQuantityChange))]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _quantity;

    private bool AllowQuantityChange(ref int value)
    {
        value = Math.Min(20, Math.Max(0, value));
        return true;
    }

    private void OnQuantityChanged(int oldValue, int newValue)
    {
        if (_quantity == 0)
        {
            Delete();
        }
        else if (_quantity < 20 && ItemID is 0x1039 or 0x1045)
        {
            ++ItemID;
        }
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (Movable && ItemID is 0x1039 or 0x1045)
        {
            ++ItemID;
        }

        if (!T2ACraftSystem.Enabled || !T2ACooking.ValidateInPack(from, this))
        {
            return;
        }

        if (IsOpen)
        {
            T2ACooking.Prompt(from, this);
        }
        else
        {
            from.SendAsciiMessage("You must open the sack of flour first.");
        }
    }

    public bool TryCombine(Mobile from, object target)
    {
        if (!IsOpen)
        {
            from.SendAsciiMessage("You must open the sack of flour first.");
            return false;
        }

        switch (target)
        {
            case SweetDough sweetDough:
                {
                    return T2ACooking.ValidateInPack(from, sweetDough) && T2ACooking.Combine(
                        from,
                        this,
                        sweetDough,
                        new CakeMix(),
                        "You mix the flour with sweet dough to make cake mix."
                    );
                }
            case Item item when T2ACooking.GetWaterSource(item) is { } water:
                {
                    return T2ACooking.CanReachWater(from, item) && T2ACooking.Combine(
                        from,
                        this,
                        water,
                        new Dough(),
                        "You mix the flour with water to make dough."
                    );
                }
            default:
                {
                    return T2ACooking.CannotCombine(from);
                }
        }
    }

    public bool IsOpen => ItemID is not (0x1039 or 0x1045);
}

[SerializationGenerator(0, false)]
public partial class Eggshells : Item
{
    [Constructible]
    public Eggshells() : base(0x9b4)
    {
    }

    public override double DefaultWeight => 0.5;
}

[SerializationGenerator(0, false)]
public partial class WheatSheaf : Item
{
    [Constructible]
    public WheatSheaf(int amount = 1) : base(7869)
    {
        Stackable = true;
        Amount = amount;
    }

    public override double DefaultWeight => 1.0;

    public override void OnDoubleClick(Mobile from)
    {
        if (Movable)
        {
            from.BeginTarget(4, false, TargetFlags.None, OnTarget);
        }
    }

    public virtual void OnTarget(Mobile from, object obj)
    {
        if (obj is AddonComponent addon)
        {
            obj = addon.Addon;
        }

        if (obj is IFlourMill mill)
        {
            var needs = mill.MaxFlour - mill.CurFlour;

            if (needs > Amount)
            {
                needs = Amount;
            }

            mill.CurFlour += needs;
            Consume(needs);
        }
    }
}
