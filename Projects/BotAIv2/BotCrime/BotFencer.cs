using System;
using System.Collections.Generic;
using Server.Items;
using Server.Logging;
using Server.Mobiles;

namespace Server.BotAI.V2;

/// <summary>
/// Offers whoever holds stolen goods the way to turn them into coin: the walk to the band's fence, who buys at half their worth,
/// or — for the fence itself, or a thief with no fence to go to — the walk to a shopkeeper in another town than the one they
/// were taken in.
///
/// <para>
/// <b>Patrick, 07.10.2026: "a fence".</b> The brief: a Shadow trader buys stolen goods at fifty per cent and resells them in
/// another town; the market's worth applies; counted; and the fence may itself be caught — wanted once three stolen things are
/// traced to it. The fence is <see cref="BotFence"/>'s keeper, the one member of the band who commits no crime: it pays out of
/// its own purse (<see cref="FenceShare"/> of <see cref="BotMarket.Worth"/>), and sells over a counter as far from the theft as
/// the leash allows. A sale in the town the thing was stolen in is traced <see cref="TraceAtHome"/> of the time, anywhere else
/// <see cref="TraceChance"/>; <see cref="TracesToPost"/> traces and the town posts the seller.
/// </para>
/// </summary>
public sealed class BotFencer : IBotProposer
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotFencer));

    /// <summary>Whether stolen goods are fenced and resold at all.</summary>
    public static bool Running { get; set; } = true;

    /// <summary>What the fence pays of a thing's worth. Patrick's half.</summary>
    public static double FenceShare { get; set; } = 0.5;

    /// <summary>The least the stolen goods must be worth for the walk to the fence or the counter to be offered.</summary>
    public static int LeastLot { get; set; } = 20;

    /// <summary>The chance one lot sold over a counter in another town is traced back to its seller.</summary>
    public static double TraceChance { get; set; } = 0.1;

    /// <summary>The same in the town it was stolen in.</summary>
    public static double TraceAtHome { get; set; } = 0.5;

    /// <summary>Traces that put the seller on the wanted list. Patrick's three.</summary>
    public static int TracesToPost { get; set; } = 3;

    /// <summary>What the resale is reckoned at a minute over and above what the goods fetch: a walk to a far town.</summary>
    public static double Minutes { get; set; } = 6.0;

    /// <summary>How long a chosen counter is kept for one seller before it is chosen again.</summary>
    public static int ChoiceMs { get; set; } = 60000;

    public static long ToFence { get; private set; }

    public static long Bought { get; private set; }

    public static long BoughtUnits { get; private set; }

    public static long BoughtGold { get; private set; }

    public static long Broke { get; private set; }

    public static long NoFence { get; private set; }

    public static long ToCounter { get; private set; }

    public static long Sold { get; private set; }

    public static long SoldUnits { get; private set; }

    public static long SoldGold { get; private set; }

    public static long NoCounter { get; private set; }

    public static long Traced { get; private set; }

    public static long FencesPosted { get; private set; }

    private static readonly Dictionary<Serial, (BaseVendor Vendor, long Tick)> _choice = [];

    public string Name => "Fencer";

    public BotStanding Rung => BotStanding.Free;

    public BotDeed Propose(IBotWilful bot)
    {
        if (!Running || !BotCrime.Running || bot?.Self is not BotMobile { Deleted: false, Alive: true } body || BotCrime.HotOf(body) == null)
        {
            return null;
        }

        var map = body.Map;

        if (map == null || map == Map.Internal || body.Tired || body.Squad != null || BotOutlaw.Jailed(body) || BotDungeon.Under(body.Location))
        {
            return null;
        }

        var worth = BotCrime.HotWorth(body);

        if (worth < LeastLot)
        {
            return null;
        }

        // A thief with a fence free to buy walks to the fence; the fence, or a thief without one, walks to a counter.
        var fence = BotFence.Who;

        if (fence != null && fence != body && fence.Alive && fence.Map == map && !BotOutlaw.Jailed(fence)
            && BotPopulation.Within(map, fence.Location, body))
        {
            ToFence++;

            return new BotFenceSale(fence, worth);
        }

        if (fence == null)
        {
            NoFence++;
        }

        var now = Core.TickCount;

        if (!_choice.TryGetValue(body.Serial, out var chosen) || now - chosen.Tick >= ChoiceMs || chosen.Vendor is not { Deleted: false })
        {
            chosen = (Counter(body, out _), now);
            _choice[body.Serial] = chosen;
        }

        if (chosen.Vendor == null)
        {
            NoCounter++;

            return null;
        }

        // Outlaws keep out of guarded towns, and every counter stands in one: the goods wait in the pack until the grey lifts.
        if (BotOutlaw.Keeps(body, map, chosen.Vendor.Location))
        {
            return null;
        }

        ToCounter++;

        return new BotResell(chosen.Vendor, worth);
    }

    /// <summary>
    /// The counter a seller's stolen goods fetch most at, after the road: one in no town any of the goods were taken in, within the
    /// seller's leash; or, failing every other, one in the town itself.
    /// </summary>
    public static BaseVendor Counter(BotMobile body, out int paying)
    {
        paying = 0;

        var list = BotCrime.HotOf(body);
        var map = body.Map;
        var pack = body.Backpack;

        if (list == null || map == null || pack == null)
        {
            return null;
        }

        HashSet<string> towns = new(StringComparer.Ordinal);
        Dictionary<Type, Item> samples = [];

        for (var i = 0; i < list.Count; i++)
        {
            if (list[i].Town != null)
            {
                towns.Add(list[i].Town);
            }

            if (!samples.ContainsKey(list[i].Kind) && pack.FindItemByType(list[i].Kind) is { } sample)
            {
                samples[list[i].Kind] = sample;
            }
        }

        if (samples.Count == 0)
        {
            return null;
        }

        BaseVendor best = null;
        var bestScore = 0.0;
        var shops = BotShops.Shops;

        for (var i = 0; i < shops.Count; i++)
        {
            var vendor = shops[i];

            if (vendor is not { Deleted: false } || vendor.Map != map || !BotPopulation.Within(map, vendor.Location, body))
            {
                continue;
            }

            var pays = 0;

            foreach (var (kind, sample) in samples)
            {
                if (BotShops.Buys(vendor, sample, out var price))
                {
                    pays += price * Math.Max(1, BotCrime.HotAmount(body, kind));
                }
            }

            if (pays <= 0)
            {
                continue;
            }

            var town = BotTowns.Of(vendor.Location)?.Name;
            var home = town != null && towns.Contains(town);
            var score = pays * (home ? 0.25 : 1.0) - body.GetDistanceToSqrt(vendor.Location) * 0.05;

            if (score > bestScore)
            {
                bestScore = score;
                best = vendor;
                paying = pays;
            }
        }

        return best;
    }

    /// <summary>Stolen goods bought by the fence: counted, and said.</summary>
    public static void Fenced(int units, int gold)
    {
        Bought++;
        BoughtUnits += units;
        BoughtGold += gold;
    }

    public static void Short() => Broke++;

    /// <summary>
    /// A lot of stolen goods sold over a counter: each kind may be traced back to the seller, and three traces post it.
    /// </summary>
    public static void Resold(BotMobile seller, BaseVendor vendor, List<(Type Kind, int Amount)> sold, int gold)
    {
        Sold++;
        SoldGold += gold;

        var town = BotTowns.Of(vendor.Location)?.Name ?? BotCrime.TownAt(vendor.Location);
        var sheet = BotCrime.Of(seller.Name, true);
        var traced = 0;
        var worth = 0;

        for (var i = 0; i < sold.Count; i++)
        {
            SoldUnits += sold[i].Amount;
            worth += sold[i].Amount * BotCrime.Worth(sold[i].Kind);

            var home = false;
            var lots = BotCrime.HotOf(seller);

            if (lots != null)
            {
                for (var k = 0; k < lots.Count; k++)
                {
                    if (lots[k].Kind == sold[i].Kind && lots[k].Town == town)
                    {
                        home = true;

                        break;
                    }
                }
            }

            if (Utility.RandomDouble() < (home ? TraceAtHome : TraceChance))
            {
                traced++;
                sheet.Traced++;
                Traced++;
            }

            BotCrime.Cool(seller, sold[i].Kind, sold[i].Amount);
        }

        logger.Information(
            "{Seller} sold stolen goods to {Vendor} in {Town} for {Gold}gp{Traced}",
            seller.Name,
            vendor.Name,
            town,
            gold,
            traced > 0 ? $"; {traced} of them traced back to it, {sheet.Traced} of {TracesToPost} now" : ""
        );

        if (traced > 0 && sheet.Traced >= Math.Max(1, TracesToPost) && !sheet.Posted)
        {
            FencesPosted++;
            BotCrime.Report(seller, vendor, town, "selling stolen goods", worth, Math.Max(1, BotCrime.ReportsToPost));
        }
    }

    public static void NoSale() => NoCounter++;

    public static string Describe() =>
        !Running
            ? "the fence: off"
            : $"the fence: {ToFence} walks to the fence and {Bought} lots bought, {BoughtUnits} things for {BoughtGold}gp at {FenceShare:P0} of worth ({Broke} the fence could not pay, {NoFence} looks with no fence in the band); {ToCounter} walks to a counter, {Sold} sold {SoldUnits} things for {SoldGold}gp ({NoCounter} found no counter), {Traced} traced back to the seller and {FencesPosted} sellers posted for it";

    public static void Forget()
    {
        _choice.Clear();
        ToFence = 0;
        Bought = 0;
        BoughtUnits = 0;
        BoughtGold = 0;
        Broke = 0;
        NoFence = 0;
        ToCounter = 0;
        Sold = 0;
        SoldUnits = 0;
        SoldGold = 0;
        NoCounter = 0;
        Traced = 0;
        FencesPosted = 0;
    }
}

/// <summary>A thief's walk to the fence with stolen goods, and the sale at half their worth, pack to pack. See <see cref="BotFencer"/>.</summary>
public sealed class BotFenceSale : BotDeed
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotFenceSale));

    public const string Trade = "fence";

    private readonly BotMobile _fence;

    private readonly Map _map;

    private readonly Point3D _where;

    private readonly int _worth;

    public BotFenceSale(BotMobile fence, int worth)
    {
        _fence = fence;
        _map = fence.Map;
        _where = fence.Location;
        _worth = worth;
    }

    public override string Kind => Trade;

    public override Map Map => _map;

    public override Point3D Where => _where;

    public override double Expects => Math.Max(1.0, _worth * BotFencer.FenceShare / Minutes);

    public override double Minutes => 2.0;

    public override string Stage => $"taking stolen goods to {_fence?.Name}";

    public override BotDoing Advance(IBotWilful bot)
    {
        if (bot?.Self is not BotMobile { Deleted: false, Alive: true } body || body.Map != _map)
        {
            return BotDoing.Failed("no body");
        }

        if (_fence is not { Deleted: false, Alive: true } || _fence.Map != _map || BotOutlaw.Jailed(_fence))
        {
            return BotDoing.Failed("the fence is gone");
        }

        if (BotCrime.HotOf(body) == null)
        {
            return BotDoing.Done("nothing stolen left to sell");
        }

        if (!body.InRange(_fence.Location, 2))
        {
            return BotDoing.Walk(_map, _fence, BotArrival.Within(2), $"to {_fence.Name}, the fence");
        }

        return Sell(body);
    }

    private BotDoing Sell(BotMobile thief)
    {
        var lots = BotCrime.HotOf(thief);
        var from = thief.Backpack;
        var into = _fence.Backpack;

        if (lots == null || from == null || into == null)
        {
            return BotDoing.Failed("no pack to hand over from");
        }

        var units = 0;
        var gold = 0;
        List<(Type Kind, int Amount)> kinds = [];

        for (var i = 0; i < lots.Count; i++)
        {
            var at = kinds.FindIndex(k => k.Kind == lots[i].Kind);

            if (at < 0)
            {
                kinds.Add((lots[i].Kind, lots[i].Amount));
            }
            else
            {
                kinds[at] = (kinds[at].Kind, kinds[at].Amount + lots[i].Amount);
            }
        }

        for (var k = 0; k < kinds.Count; k++)
        {
            var (kind, want) = kinds[k];
            var price = Math.Max(1, (int)(BotCrime.Worth(kind) * BotFencer.FenceShare));
            var moved = 0;

            while (moved < want && from.FindItemByType(kind) is { } stack && BotShopkeep.Merchandise(stack, thief.Bond))
            {
                var part = Math.Min(Math.Max(1, stack.Amount), want - moved);
                var afford = BotYield.Wealth(_fence) / price;

                if (afford <= 0)
                {
                    BotFencer.Short();

                    break;
                }

                part = Math.Min(part, afford);

                if (part < Math.Max(1, stack.Amount) && (!stack.Stackable || Mobile.LiftItemDupe(stack, part) == null))
                {
                    break;
                }

                var bill = part * price;

                if (!BotMarket.Charge(_fence, bill, out _))
                {
                    BotFencer.Short();

                    break;
                }

                if (!into.TryDropItem(_fence, stack, false))
                {
                    BotMarket.Refund(_fence, bill);

                    break;
                }

                BotMarket.Pay(thief, bill);
                moved += part;
                gold += bill;
            }

            if (moved > 0)
            {
                units += moved;
                BotCrime.Cool(thief, kind, moved, _fence);
            }
            else
            {
                // Gone from the pack by some other road — sold, banked, dropped at a death: off the book.
                BotCrime.Cool(thief, kind, want);
            }
        }

        if (units <= 0)
        {
            return BotDoing.Failed($"{_fence.Name} would not or could not buy");
        }

        BotFencer.Fenced(units, gold);

        logger.Information(
            "{Thief} sold {Units} stolen things to {Fence} the fence for {Gold}gp, {Share:P0} of their worth",
            thief.Name,
            units,
            _fence.Name,
            gold,
            BotFencer.FenceShare
        );

        BotVoice.Say(_fence, "local", $"Half their worth and no questions: {gold} gold.", true);
        BotVoice.Say(thief, "local", "A pleasure, as always.", true);
        BotEvents.Post("crime", thief, $"sold {units} stolen things to {_fence.Name} the fence for {gold}gp");

        return BotDoing.Done($"sold {units} stolen things to {_fence.Name} for {gold}gp");
    }
}

/// <summary>Stolen goods carried to a counter in another town and sold there. See <see cref="BotFencer"/>.</summary>
public sealed class BotResell : BotDeed
{
    public const string Trade = "resell";

    private readonly BaseVendor _vendor;

    private readonly Map _map;

    private readonly int _worth;

    public BotResell(BaseVendor vendor, int worth)
    {
        _vendor = vendor;
        _map = vendor.Map;
        _worth = worth;
    }

    public override string Kind => Trade;

    public override Map Map => _map;

    public override Point3D Where => _vendor.Location;

    public override double Expects => Math.Max(1.0, _worth / Math.Max(1.0, BotFencer.Minutes));

    public override double Minutes => BotFencer.Minutes;

    public override bool AtCounter => true;

    public override string Stage => $"carrying stolen goods to {_vendor?.Name}";

    public override BotDoing Advance(IBotWilful bot)
    {
        if (bot?.Self is not BotMobile { Deleted: false, Alive: true } body || body.Map != _map)
        {
            return BotDoing.Failed("no body");
        }

        if (_vendor is not { Deleted: false })
        {
            return BotDoing.Failed("the counter is gone");
        }

        var lots = BotCrime.HotOf(body);

        if (lots == null)
        {
            return BotDoing.Done("nothing stolen left to sell");
        }

        if (!body.InRange(_vendor.Location, Math.Max(1, BotShops.CounterReach)))
        {
            return BotDoing.Walk(_map, _vendor.Location, BotArrival.Within(Math.Max(1, BotShops.CounterReach - 1)), $"to {_vendor.Name} with stolen goods");
        }

        var before = BotCrime.Census(body);
        List<Item> goods = [];
        var pack = body.Backpack;

        if (pack == null)
        {
            return BotDoing.Failed("no pack");
        }

        for (var i = 0; i < pack.Items.Count; i++)
        {
            var item = pack.Items[i];

            if (item != null && BotCrime.HotAmount(body, item.GetType()) > 0 && BotShopkeep.Merchandise(item, body.Bond))
            {
                goods.Add(item);
            }
        }

        if (goods.Count == 0)
        {
            // The stolen things are gone from the pack by some other road: off the book.
            for (var i = lots.Count - 1; i >= 0; i--)
            {
                BotCrime.Cool(body, lots[i].Kind, lots[i].Amount);
            }

            return BotDoing.Done("the stolen goods were gone from the pack");
        }

        var gold = BotShops.Sell(bot, _vendor, goods);
        var after = BotCrime.Census(body);
        List<(Type Kind, int Amount)> sold = [];

        foreach (var (kind, had) in before)
        {
            if (kind == typeof(Gold))
            {
                continue;
            }

            var gone = Math.Min(had - after.GetValueOrDefault(kind), BotCrime.HotAmount(body, kind));

            if (gone > 0)
            {
                sold.Add((kind, gone));
            }
        }

        if (gold <= 0 || sold.Count == 0)
        {
            BotFencer.NoSale();

            return BotDoing.Failed($"{_vendor.Name} would not buy the stolen goods");
        }

        BotFencer.Resold(body, _vendor, sold, gold);

        return BotDoing.Done($"sold stolen goods to {_vendor.Name} for {gold}gp");
    }
}
