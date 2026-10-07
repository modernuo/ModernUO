using System;
using Server.Items;
using Server.Targeting;

namespace Server.Engines.Craft.T2A;

/// <summary>An item that can be double-clicked under T2A to combine with, or cook over, a target.</summary>
public interface IT2ACombinable
{
    bool TryCombine(Mobile from, object target);
}

/// <summary>
/// Shared helpers for T2A cooking, which is item-on-item and item-on-heat-source rather than a craft menu.
/// Each ingredient item decides its own results in <see cref="IT2ACombinable.TryCombine"/>.
/// </summary>
public sealed class T2ACooking
{
    private const int _heatSourceRange = 1;
    private const int _waterRange = 2;
    private const int _burnRange = 3;

    private T2ACooking()
    {
    }

    /// <summary>Asks the player for a target if the item is in their pack.</summary>
    public static void Prompt(Mobile from, Item source)
    {
        if (!ValidateInPack(from, source))
        {
            return;
        }

        from.SendAsciiMessage("What do you wish to combine this with, or cook it over?");
        from.Target = new CookTarget(source);
    }

    public static bool ValidateInPack(Mobile from, Item item)
    {
        if (item is { Deleted: false } && item.IsChildOf(from.Backpack))
        {
            return true;
        }

        from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
        return false;
    }

    public static bool CannotCombine(Mobile from)
    {
        from.SendAsciiMessage("You cannot combine those.");
        return false;
    }

    //TODO Support static water troughs and barrels (no static water ID list exists yet)
    public static IHasQuantity GetWaterSource(object o) =>
        o switch
        {
            BaseBeverage { Content: BeverageType.Water, Quantity: > 0 } bev => bev,
            BaseWaterContainer { IsEmpty: false } container                 => container,
            AddonComponent { Addon: IWaterSource { Quantity: > 0 } trough } => trough,
            IWaterSource { Quantity: > 0 } source                           => source,
            _                                                               => null
        };

    // Ground water only, so a nearby open container (someone else's pack) cannot be drawn from.
    public static bool CanReachWater(Mobile from, Item water)
    {
        if (water.IsChildOf(from.Backpack) || water.Parent == from ||
            water.RootParent == null && water.Map == from.Map &&
            from.InRange(water.GetWorldLocation(), _waterRange) && from.InLOS(water))
        {
            return true;
        }

        from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
        return false;
    }

    /// <summary>Spends one unit of each ingredient and gives the player the result.</summary>
    public static bool Combine(Mobile from, object source, object target, Item result, string message)
    {
        ConsumeOne(source);
        ConsumeOne(target);

        from.AddToBackpack(result);
        from.SendAsciiMessage(message);
        return true;
    }

    private static void ConsumeOne(object o)
    {
        switch (o)
        {
            case SackFlour sack:
                {
                    sack.Quantity--;
                    break;
                }
            case BowlFlour bowl:
                {
                    //TODO Confirm whether an empty bowl is returned
                    bowl.Delete();
                    break;
                }
            case IHasQuantity container:
                {
                    container.Quantity--;
                    break;
                }
            case Item item:
                {
                    item.Consume();
                    break;
                }
        }
    }

    /// <summary>Cooks the source over a heat source if it is close enough and the player is not already cooking.</summary>
    public static bool BeginHeat(Mobile from, Item source, object target)
    {
        var point = target is IPoint3D p ? new Point3D(p) : from.Location;
        if (from.Map == null || !from.InRange(point, _heatSourceRange))
        {
            from.SendLocalizedMessage(500446); // That is too far away.
            return false;
        }

        if (!from.BeginAction<T2ACooking>())
        {
            from.SendLocalizedMessage(500119); // You must wait to perform another action
            return false;
        }

        from.PlaySound(0x225);
        source.Consume();

        Timer.DelayCall(TimeSpan.FromSeconds(5.0), FinishHeat, from, source, from.Map, point);
        return true;
    }

    // The source is already consumed here and may be deleted, so only its type and CookingLevel are read.
    private static void FinishHeat(Mobile from, Item source, Map map, Point3D point)
    {
        from.EndAction<T2ACooking>();

        if (from.Deleted)
        {
            return;
        }

        if (from.Map != map || !from.InRange(point, _burnRange))
        {
            from.SendLocalizedMessage(500686); // You burn the food to a crisp! It's ruined.
            return;
        }

        var (min, max) = source switch
        {
            CookableFood food => (food.CookingLevel, 100.0),
            Dough             => CraftRange(typeof(BreadLoaf)),
            SweetDough        => CraftRange(typeof(Muffins)),
            _                 => (0.0, 100.0)
        };

        if (!from.CheckSkill(SkillName.Cooking, min, max))
        {
            from.SendLocalizedMessage(500686); // You burn the food to a crisp! It's ruined.
            return;
        }

        Item result = source switch
        {
            CookableFood food => food.Cook(),
            Dough             => new BreadLoaf(),
            SweetDough        => new Muffins(),
            _                 => null
        };

        if (result != null)
        {
            from.AddToBackpack(result);
            from.PlaySound(0x57);
        }
    }

    // Falls back to the full range when the cooking craft system has no entry or is not initialized.
    private static (double Min, double Max) CraftRange(Type type)
    {
        var skills = DefCooking.CraftSystem?.CraftItems.SearchFor(type)?.Skills;

        return skills is { Count: > 0 } ? (skills[0].MinSkill, skills[0].MaxSkill) : (0.0, 100.0);
    }

    private sealed class CookTarget : Target
    {
        private readonly Item _source;

        public CookTarget(Item source) : base(_waterRange, false, TargetFlags.None) => _source = source;

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (ValidateInPack(from, _source) && _source is IT2ACombinable combinable)
            {
                combinable.TryCombine(from, targeted);
            }
        }
    }
}
