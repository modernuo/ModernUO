using System;
using System.Collections.Generic;
using Server.Items;
using Server.Logging;
using Server.Multis;

namespace Server.BotAI.V2;

/// <summary>
/// A house of a bot's own is furnished over time (PLAN-life A4): the first thing a house gets is a bed, bought with the
/// owner's own money while the owner is at home to see it delivered.
///
/// <para>
/// <b>Honest money, no escrow.</b> The bed is paid for out of the owner's purse and account through
/// <see cref="BotMarket.Charge"/> — which never spends a want's reservation — only when the owner keeps
/// <see cref="BotAbode.Keep"/> over the price; the coin leaves the world as it does at any shopkeeper's, and the owner's
/// work never learns the spend (<see cref="BotYield.Aside"/>). Nothing is reserved, so a bot that walks off mid-minute
/// owes nothing and a restart strands nothing: the bed stands in the house, where the engine keeps it.
/// </para>
///
/// <para>
/// <b>Slow, and at home.</b> One house is looked at a minute; a house is asked again after <see cref="EveryHours"/>
/// hours; the owner must be inside its own house when the bed comes — furniture nobody sees arrive is furniture that
/// was always there. Placement is the halls' own machinery (<see cref="BotFittings.InstallOwned"/>): the engine's
/// <c>CouldFit</c> answers walls and other addons, so a bed is never put through a wall.
/// </para>
/// </summary>
public static class BotFurnish
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotFurnish));

    /// <summary>Whether the population's houses are furnished at all.</summary>
    public static bool Running { get; set; } = true;

    /// <summary>What a bed costs, in gold. Eight hundred: furniture is comfort, not a roof — the cheapest house is ten thousand.</summary>
    public static int BedPrice { get; set; } = 800;

    /// <summary>Hours before a house is looked at again. Six: a house fills over a week, not an evening.</summary>
    public static double EveryHours { get; set; } = 6.0;

    // ---- counters ----------------------------------------------------------------------------------------------------------

    /// <summary>Beds bought and set up.</summary>
    public static long Bought { get; private set; }

    /// <summary>Gold spent on them, out of the owners' purses.</summary>
    public static long Spent { get; private set; }

    /// <summary>Houses with money and an owner at home, but nowhere the engine would take a bed.</summary>
    public static long NoRoom { get; private set; }

    /// <summary>Looks at a house whose owner was not inside it.</summary>
    public static long NotHome { get; private set; }

    /// <summary>Looks at a house whose owner could not pay and keep its keep-back.</summary>
    public static long Short { get; private set; }

    /// <summary>When each house, by its owner's name, was last looked at; a minute's clock paces the list.</summary>
    private static readonly Dictionary<string, long> _tried = new(StringComparer.Ordinal);

    /// <summary>
    /// One house a minute, from the estate's clock: the first house due whose owner is home and can pay gets its bed.
    /// </summary>
    public static void Tick(long now)
    {
        if (!Running)
        {
            return;
        }

        foreach (var house in BotAbode.Houses)
        {
            if (house is not { Deleted: false } || house.Map == null || house.Map == Map.Internal)
            {
                continue;
            }

            if (house.Owner is not BotMobile owner || owner.Deleted || !owner.Alive || string.IsNullOrEmpty(owner.Name))
            {
                continue;
            }

            if (HasBed(house))
            {
                continue;
            }

            if (_tried.TryGetValue(owner.Name, out var tried)
                && now - tried < Math.Max(1.0, EveryHours) * 3600000)
            {
                continue;
            }

            // Furniture nobody sees arrive is furniture that was always there: the owner is home for it — inside, or at
            // the doorstep, which is what a walk home counts as home (BotRepose).
            if (!house.IsInside(owner) && (house.BanLocation == Point3D.Zero || !owner.InRange(house.BanLocation, 2)))
            {
                NotHome++;

                continue;
            }

            if (BotYield.Spendable(owner) < BedPrice + BotAbode.Keep)
            {
                Short++;

                continue;
            }

            var bed = BotFittings.InstallOwned(house, static () => new ElvenBedSouthAddon())
                      ?? BotFittings.InstallOwned(house, static () => new ElvenBedEastAddon());

            _tried[owner.Name] = now;

            if (bed == null)
            {
                NoRoom++;

                continue;
            }

            if (!BotMarket.Charge(owner, BedPrice, out _))
            {
                // The wealth was read a line above on this same thread; if it moved regardless, the bed goes back.
                house.Addons?.Remove(bed);
                bed.Delete();
                Short++;

                continue;
            }

            BotYield.Aside(owner, BedPrice);
            Bought++;
            Spent += BedPrice;

            logger.Information("{Owner} has furnished {House} with a bed for {Price}gp", owner.Name, house.Sign?.Name ?? "its house", BedPrice);

            BotEvents.Post("abode", owner, $"furnished its house with a bed for {BedPrice}gp");
            BotCampTalk.Say(owner, "abode:furnished", null, null, null);

            // One house a beat: the next is looked at on the next tick.
            return;
        }
    }

    /// <summary>Whether the house already has a bed of any make among its addons.</summary>
    private static bool HasBed(BaseHouse house)
    {
        if (house.Addons == null)
        {
            return false;
        }

        for (var i = 0; i < house.Addons.Count; i++)
        {
            if (house.Addons[i] is { Deleted: false } addon && addon.GetType().Name.Contains("Bed", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    public static string Describe() =>
        !Running
            ? "furnishing is off"
            : $"furnishing: {Bought} beds bought for {Spent}gp, {NoRoom} houses with nowhere a bed fits, {NotHome} looks with the owner out, {Short} short of the price";

    public static void Forget()
    {
        _tried.Clear();
        Bought = 0;
        Spent = 0;
        NoRoom = 0;
        NotHome = 0;
        Short = 0;
    }
}
