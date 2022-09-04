using System;
using ModernUO.Serialization;
using Server.Targeting;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class Dough : Item
{
    [Constructible]
    public Dough() : base(0x103d)
    {
        Stackable = Core.ML;
        Weight = 1.0;
    }
}

[SerializationGenerator(0, false)]
public partial class SweetDough : Item
{
    [Constructible]
    public SweetDough() : base(0x103d)
    {
        Stackable = Core.ML;
        Weight = 1.0;
        Hue = 150;
    }

    public override int LabelNumber => 1041340; // sweet dough
}

[SerializationGenerator(0, false)]
public partial class JarHoney : Item
{
    [Constructible]
    public JarHoney() : base(0x9ec)
    {
        Weight = 1.0;
        Stackable = true;
    }
}

[SerializationGenerator(0, false)]
public partial class BowlFlour : Item
{
    [Constructible]
    public BowlFlour() : base(0xa1e) => Weight = 1.0;
}

[SerializationGenerator(0, false)]
public partial class WoodenBowl : Item
{
    [Constructible]
    public WoodenBowl() : base(0x15f8) => Weight = 1.0;
}

[TypeAlias("Server.Items.SackFlourOpen")]
[SerializationGenerator(0, false)]
public partial class SackFlour : Item, IHasQuantity
{
    [Constructible]
    public SackFlour() : base(0x1039)
    {
        Weight = 5.0;
        _quantity = 20;
    }

    [CommandProperty(AccessLevel.GameMaster)]
    [SerializableProperty(0)]
    public int Quantity
    {
        get => _quantity;
        set
        {
            _quantity = Math.Min(20, Math.Max(0, value));

            if (_quantity == 0)
            {
                Delete();
            }
            else if (_quantity < 20 && ItemID is 0x1039 or 0x1045)
            {
                ++ItemID;
            }

            this.MarkDirty();
        }
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (Movable && ItemID is 0x1039 or 0x1045)
        {
            ++ItemID;
        }
    }
}

[SerializationGenerator(0, false)]
public partial class Eggshells : Item
{
    [Constructible]
    public Eggshells() : base(0x9b4) => Weight = 0.5;
}

[SerializationGenerator(0, false)]
public partial class WheatSheaf : Item
{
    [Constructible]
    public WheatSheaf(int amount = 1) : base(7869)
    {
        Weight = 1.0;
        Stackable = true;
        Amount = amount;
    }

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
