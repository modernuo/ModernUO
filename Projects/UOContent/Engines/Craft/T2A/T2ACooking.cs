using System;
using System.Collections.Generic;
using Server.Items;
using Server.Targeting;

namespace Server.Engines.Craft.T2A;

/// <summary>
/// T2A cooking is item-on-item and item-on-heat-source, not a craft menu. It is table driven:
/// add a <see cref="CombineRow"/> to <c>_combineRows</c> or a <see cref="HeatRow"/> to <c>_heatRows</c>.
/// </summary>
public static class T2ACooking
{
    private const int _heatSourceRange = 1;
    private const int _waterRange = 2;
    private const int _burnRange = 3;

    // Held for the duration of the heat timer so a mobile cooks one thing at a time.
    private sealed class CookLock;

    /// <summary>One side of a recipe: how to recognise it, check it is usable, and spend one unit of it.</summary>
    private sealed class Ingredient
    {
        public Func<object, bool> Matches { get; init; }
        public Func<Mobile, object, bool> Validate { get; init; }
        public Action<object> Consume { get; init; }

        // IsInstanceOfType so one row covers subclasses (every CookableFood, both pumpkin types).
        public static Ingredient InPack(params Type[] types) => new()
        {
            Matches = o => MatchesAny(o, types),
            Validate = ValidateInPack,
            Consume = o => ((Item)o).Consume()
        };

        private static bool MatchesAny(object o, Type[] types)
        {
            for (var i = 0; i < types.Length; i++)
            {
                if (types[i].IsInstanceOfType(o))
                {
                    return true;
                }
            }

            return false;
        }
    }

    private sealed class CombineRow
    {
        public Ingredient A { get; init; }
        public Ingredient B { get; init; }

        // Also works with B double-clicked and A targeted.
        public bool Symmetric { get; init; }
        public Func<Item> Create { get; init; }
        public string Message { get; init; }
    }

    private sealed class HeatRow
    {
        public Ingredient Source { get; init; }
        public Func<Item, (double Min, double Max)> Skill { get; init; }
        public Func<Item, Item> Create { get; init; }
    }

    private sealed record HeatJob(Mobile From, Item Source, HeatRow Row, Map Map, Point3D Point);

    private static readonly Ingredient _flour = new()
    {
        Matches = o => o is SackFlour or BowlFlour,
        Validate = (from, o) =>
        {
            if (!ValidateInPack(from, o))
            {
                return false;
            }

            if (o is SackFlour { IsOpen: false })
            {
                from.SendAsciiMessage("You must open the sack of flour first.");
                return false;
            }

            return true;
        },
        Consume = o =>
        {
            if (o is SackFlour sack)
            {
                sack.Quantity--;
            }
            else
            {
                //TODO Confirm whether an empty bowl is returned
                ((Item)o).Delete();
            }
        }
    };

    // Static water troughs have no tile ID list in this repo.
    //TODO Support static water troughs
    private static readonly Ingredient _water = new()
    {
        Matches = o => GetWaterSource(o) != null,
        Validate = (from, o) =>
        {
            // Ground water only, so a nearby open container (someone else's pack) cannot be drawn from
            var item = (Item)o;
            if (item.IsChildOf(from.Backpack) || item.Parent == from ||
                item.RootParent == null && item.Map == from.Map &&
                from.InRange(item.GetWorldLocation(), _waterRange) && from.InLOS(item))
            {
                return true;
            }

            from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
            return false;
        },
        Consume = o => GetWaterSource(o).Quantity--
    };

    private static readonly Ingredient _dough = Ingredient.InPack(typeof(Dough));
    private static readonly Ingredient _sweetDough = Ingredient.InPack(typeof(SweetDough));
    private static readonly Ingredient _honey = Ingredient.InPack(typeof(JarHoney));

    private static readonly List<CombineRow> _combineRows =
    [
        new()
        {
            A = _flour, B = _water, Create = () => new Dough(),
            Message = "You mix the flour with water to make dough."
        },
        new()
        {
            A = _dough, B = _honey, Symmetric = true, Create = () => new SweetDough(),
            Message = "You mix the dough with honey to make sweet dough."
        },
        new()
        {
            A = _sweetDough, B = _honey, Symmetric = true, Create = () => new CookieMix(),
            Message = "You mix the sweet dough with honey to make cookie mix."
        },
        new()
        {
            A = _flour, B = _sweetDough, Symmetric = true, Create = () => new CakeMix(),
            Message = "You mix the flour with sweet dough to make cake mix."
        },
        DoughRow(typeof(Pear), () => new UnbakedFruitPie()),
        DoughRow(typeof(Peach), () => new UnbakedPeachCobbler()),
        DoughRow(typeof(Apple), () => new UnbakedApplePie()),
        DoughRow(typeof(Pumpkin), () => new UnbakedPumpkinPie()),
        DoughRow(typeof(SmallPumpkin), () => new UnbakedPumpkinPie()),
        DoughRow(typeof(Eggs), () => new UnbakedQuiche()),
        DoughRow(typeof(Ham), () => new UnbakedMeatPie()),
        DoughRow(typeof(Spam), () => new UnbakedMeatPie()),
        DoughRow(typeof(CookedBird), () => new UnbakedMeatPie()),
        DoughRow(typeof(FishSteak), () => new UnbakedMeatPie()),
        DoughRow(typeof(Sausage), () => new UncookedSausagePizza())
    ];

    private static readonly List<HeatRow> _heatRows =
    [
        new()
        {
            Source = Ingredient.InPack(typeof(CookableFood)),
            Skill = s => (((CookableFood)s).CookingLevel, 100.0),
            Create = s => ((CookableFood)s).Cook()
        },
        new()
        {
            Source = _dough,
            Skill = _ => CraftRange(typeof(BreadLoaf)),
            Create = _ => new BreadLoaf()
        },
        new()
        {
            Source = _sweetDough,
            Skill = _ => CraftRange(typeof(Muffins)),
            Create = _ => new Muffins()
        }
    ];

    private static CombineRow DoughRow(Type ingredient, Func<Item> create) => new()
    {
        A = _dough,
        B = Ingredient.InPack(ingredient),
        Create = create,
        Message = "You combine the dough with the ingredient."
    };

    // Falls back to the full range when the cooking craft system has no entry or is not initialized.
    private static (double Min, double Max) CraftRange(Type type)
    {
        var skills = DefCooking.CraftSystem?.CraftItems.SearchFor(type)?.Skills;

        return skills is { Count: > 0 } ? (skills[0].MinSkill, skills[0].MaxSkill) : (0.0, 100.0);
    }

    private static bool ValidateInPack(Mobile from, object o)
    {
        if (o is Item { Deleted: false } item && item.IsChildOf(from.Backpack))
        {
            return true;
        }

        from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
        return false;
    }

    private static IHasQuantity GetWaterSource(object o) =>
        o switch
        {
            BaseBeverage { Content: BeverageType.Water, Quantity: > 0 } bev                  => bev,
            BaseWaterContainer { IsEmpty: false } container                                  => container,
            AddonComponent { Addon: IWaterSource { Quantity: > 0 } trough }                  => trough,
            IWaterSource { Quantity: > 0 } source                                            => source,
            _                                                                                => null
        };

    /// <summary>Starts the target flow for a double-clicked item. Returns false if no recipe uses it.</summary>
    public static bool TryBeginUse(Mobile from, Item source)
    {
        if (!UsedAsSource(source))
        {
            return false;
        }

        if (!ValidateInPack(from, source))
        {
            return true;
        }

        if (source is SackFlour { IsOpen: false })
        {
            from.SendAsciiMessage("You must open the sack of flour first.");
            return true;
        }

        from.SendAsciiMessage("What do you wish to combine this with, or cook it over?");
        from.Target = new CookTarget(source);
        return true;
    }

    private static bool UsedAsSource(Item source)
    {
        for (var i = 0; i < _combineRows.Count; i++)
        {
            var row = _combineRows[i];
            if (row.A.Matches(source) || row.Symmetric && row.B.Matches(source))
            {
                return true;
            }
        }

        for (var i = 0; i < _heatRows.Count; i++)
        {
            if (_heatRows[i].Source.Matches(source))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Combines source with target if a row allows it. Sends its own failure messages.</summary>
    public static bool TryCombine(Mobile from, Item source, object target)
    {
        if (!ValidateInPack(from, source))
        {
            return false;
        }

        for (var i = 0; i < _combineRows.Count; i++)
        {
            var row = _combineRows[i];
            Ingredient sourceSide;
            Ingredient targetSide;

            if (row.A.Matches(source) && row.B.Matches(target))
            {
                sourceSide = row.A;
                targetSide = row.B;
            }
            else if (row.Symmetric && row.B.Matches(source) && row.A.Matches(target))
            {
                sourceSide = row.B;
                targetSide = row.A;
            }
            else
            {
                continue;
            }

            if (!sourceSide.Validate(from, source) || !targetSide.Validate(from, target))
            {
                return false;
            }

            sourceSide.Consume(source);
            targetSide.Consume(target);

            from.AddToBackpack(row.Create());
            from.SendAsciiMessage(row.Message);
            return true;
        }

        from.SendAsciiMessage("You cannot combine those.");
        return false;
    }

    private static void BeginHeat(Mobile from, Item source, object target)
    {
        HeatRow row = null;
        for (var i = 0; i < _heatRows.Count && row == null; i++)
        {
            if (_heatRows[i].Source.Matches(source))
            {
                row = _heatRows[i];
            }
        }

        if (row == null)
        {
            from.SendAsciiMessage("That cannot be cooked.");
            return;
        }

        var point = target is IPoint3D p ? new Point3D(p) : from.Location;
        if (from.Map == null || !from.InRange(point, _heatSourceRange))
        {
            from.SendLocalizedMessage(500446); // That is too far away.
            return;
        }

        if (!from.BeginAction<CookLock>())
        {
            from.SendLocalizedMessage(500119); // You must wait to perform another action
            return;
        }

        from.PlaySound(0x225);
        source.Consume();

        Timer.DelayCall(TimeSpan.FromSeconds(5.0), FinishHeat, new HeatJob(from, source, row, from.Map, point));
    }

    private static void FinishHeat(HeatJob job)
    {
        var from = job.From;
        from.EndAction<CookLock>();

        if (from.Deleted)
        {
            return;
        }

        if (from.Map != job.Map || !from.InRange(job.Point, _burnRange))
        {
            from.SendLocalizedMessage(500686); // You burn the food to a crisp! It's ruined.
            return;
        }

        var (min, max) = job.Row.Skill(job.Source);
        if (from.CheckSkill(SkillName.Cooking, min, max))
        {
            from.AddToBackpack(job.Row.Create(job.Source));
            from.PlaySound(0x57);
        }
        else
        {
            from.SendLocalizedMessage(500686); // You burn the food to a crisp! It's ruined.
        }
    }

    private class CookTarget : Target
    {
        private readonly Item _source;

        public CookTarget(Item source) : base(_waterRange, false, TargetFlags.None) => _source = source;

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (!ValidateInPack(from, _source))
            {
                return;
            }

            if (CookableFood.IsHeatSource(targeted))
            {
                BeginHeat(from, _source, targeted);
            }
            else
            {
                TryCombine(from, _source, targeted);
            }
        }
    }
}
