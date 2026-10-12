using System;
using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;

namespace Server.BotAI.V2;

/// <summary>
/// Taking what the population would not buy to somebody who will. The shard's only faucet.
///
/// <para>
/// <b>Every other coin in this world is a coin that already existed.</b> Trade between bots moves gold about;
/// a shopkeeper's purse creates it. Until this undertaking existed there was no gold at all — bots are born
/// with none and nothing minted any — so every piece of work that cost money to start failed on its first
/// beat and only digging, which costs nothing, could happen at all.
/// </para>
///
/// <para>
/// <b>The condition for coming here is the whole design.</b> A shopkeeper is not where goods go; it is where goods go
/// <em>when the population is not asking for them</em>: nobody has said a want for the kind, and nobody has been short of it
/// lately (<see cref="BotShopkeep.Demanded"/>). Under the auction that was a stall that had stood unsold for ten minutes; since
/// the stalls went (01.10.2026) it is read straight off the wants and the shortages, and the goods come out of the pack or, at a
/// bank, out of the bot's own bank box (<see cref="BotMarket.FromBank"/>), where the porter put what it did not keep.
/// </para>
///
/// <para>
/// It matters that this is narrow. The first version pointed every bot at this faucet and lost 110,900 gold
/// in a night in the other direction: 67k mined against 156k spent over counters, a population of 116 traders
/// to 14 fighters, and a median purse of 57 against a poverty line of 800. Selling to a shopkeeper has to be
/// what a bot does with what nobody wanted, not what a bot does.
/// </para>
///
/// <para>
/// And it is deliberately <b>the wrong way to make a living</b>, which the numbers already say without being
/// told to: a blacksmith pays 4 for an iron ingot and a bot asks 6; a tailor pays 6 for a shirt and a bot
/// asks 12. The counter is the floor under the market, not a competitor to it.
/// </para>
/// </summary>
public sealed class BotPeddle : BotDeed
{
    /// <summary>The ledger's key.</summary>
    public const string Trade = "peddle";

    /// <summary>How long the errand is expected to take once the bot is there.</summary>
    public static double WorkMinutes { get; set; } = 3.0;

    private readonly BaseVendor _shop;

    private readonly Type _kind;

    private readonly string _label;

    private readonly int _units;

    private readonly int _price;

    private int _earned;

    private int _sold;

    private bool _begun;

    /// <summary>Loads taken out of the bot's own bank box at a bank before the walk, and how many things they held.</summary>
    public static long FromBox { get; private set; }

    public static long FromBoxUnits { get; private set; }

    /// <summary>Loads to be taken out of the bank box at the first beat rather than carried already. See <c>BotPeddler</c>.</summary>
    private readonly bool _fromBox;

    /// <summary>
    /// The capital's price the chooser counted for this counter (one where none), and how many tiles further than the nearest
    /// buyer the load was drawn for it. See <see cref="BotCapital"/>.
    /// </summary>
    private readonly double _factor;

    private readonly int _passed;

    private int _premium;

    public BotPeddle(BaseVendor shop, Type kind, string label, int units, int price, bool fromBox = false, double factor = 1.0, int passed = 0)
    {
        _fromBox = fromBox;
        _shop = shop;
        _kind = kind;
        _label = label;
        _units = Math.Max(1, units);
        _price = Math.Max(1, price);
        _factor = Math.Max(1.0, factor);
        _passed = Math.Max(0, passed);
    }

    public override string Kind => Trade;

    /// <summary>
    /// A load on its way to a counter is seen through. See <see cref="BotDeed.Steadfast"/>. Nothing in here
    /// keeps time, so there is nothing to put right when it is taken up again.
    /// </summary>
    public override bool Steadfast => true;

    public override Map Map => _shop?.Map;

    public override Point3D Where => _shop?.Location ?? Point3D.Zero;

    /// <summary>
    /// What the load is actually worth over that counter, per minute — not a flat prior.
    ///
    /// The proposer knows both numbers exactly, which almost nothing else in this project does: how many
    /// there are, and what this shopkeeper pays for one. A guess would be strictly worse than the truth, and
    /// twenty ingots outranking three is the behaviour anybody would want.
    ///
    /// <para>
    /// <b>With the capital's price in it when the chooser counted it (30.09.2026).</b> The load was sent to the capital's counter
    /// because it fetches <see cref="BotCapital.Premium"/> times more there; claimed at the shopkeeper's price alone it would be
    /// weighed at less than the counter it walked past.
    /// </para>
    /// </summary>
    public override double Expects => Math.Max(1.0, (_units * (double)_price * _factor + Extra) / Math.Max(0.5, WorkMinutes));

    /// <summary>
    /// What the rest of a hunter's loot fetches over the same counter (<see cref="RestWorth"/>), sold on the same visit and so
    /// claimed with the load (02.10.2026, build next). Nought for a bot that keeps shop.
    /// </summary>
    public int Extra { get; init; }

    /// <summary>A load of capped surplus out of a pack whose full bank box had no place for it (06.10.2026, box16). See <c>BotPeddler</c>.</summary>
    public bool BoxedOut { get; init; }

    /// <summary>The most units of the kind this load sells, when it is not every one over the allowance (a want near keeps the rest). Nought: all.</summary>
    public int Most { get; init; }

    public override double Minutes => WorkMinutes;

    /// <summary>Nothing. Handing goods over a counter teaches a bot nothing at all.</summary>
    public override SkillName? Trains => null;

    /// <summary>Nothing to pay. This is the earning half of a living, and the only one that makes new coin.</summary>
    public override int Outlay => 0;

    /// <summary>
    /// <b>Not a counter that needs a free place in the pack (30.09.2026, build 368).</b> The shopkeeper pays into the pack,
    /// but <c>BaseVendor.OnSellItems</c> takes the goods out first — whole stacks, see <c>BotShops.Sell</c> — and only then
    /// adds the coin, so a sale always leaves the place the coin needs. Asked as a counter, it was vetoed to exactly the bot
    /// it cures: Adela the Architect at "126 of 125 things", her three lots out and her bank box full, was refused "peddle
    /// goes through a counter and the pack has no room for a coin" while the daggers and spears she forged had nowhere else
    /// to go, and took an empty unload 4869 times in the hour of 363 instead. See <see cref="BotDeed.AtCounter"/>.
    /// </summary>
    public override bool AtCounter => false;

    /// <summary>All of it, and this is the one undertaking in the project of which that is true.</summary>
    public override double Coin => 1.0;

    /// <summary>Nothing is produced here. The takings are the coin, counted by the brain as it always is.</summary>
    public override int Made => 0;

    public override string Stage =>
        _sold > 0 ? $"sold {_sold} {_label} for {_earned}gp" : $"taking {_units} {_label} to {_shop?.Name}";

    /// <summary>
    /// The way to the shopkeeper turned out not to exist.
    ///
    /// <para>
    /// <b>Nothing to bend to here, and something to write down — and it was the writing down that was
    /// missing.</b> What this undertaking carries was priced against <em>this</em> shopkeeper, so swapping in
    /// another one mid-errand would carry a stale price; failing is the honest answer. But the failure has to
    /// be filed under the <em>place's</em> name, because that is the word the shop lookup asks in. Filed under
    /// the undertaking's name — which is all <c>BotWill.Settle</c> can do — it is written and never read, and
    /// the next beat picks the same unreachable shopkeeper on distance alone. Calla walked at Gus thirty-one
    /// times in an hour on 26.08.2026 that way.
    /// </para>
    /// </summary>
    public override bool Bend(IBotWilful bot)
    {
        if (_shop == null)
        {
            return false;
        }

        bot?.Resolve?.Ledger?.Beware(BotShops.ShopKind, _shop.Map, _shop.Location);

        return false;
    }

    public override BotDoing Advance(IBotWilful bot)
    {
        var body = bot?.Self;

        if (body == null)
        {
            return BotDoing.Failed("no body");
        }

        if (_shop == null || _shop.Deleted || _shop.Map == null || _shop.Map == Map.Internal)
        {
            return BotDoing.Failed("the shopkeeper is gone");
        }

        // Out of the bank box at the first beat, standing at the bank the peddler found the bot at: the box is reached only there.
        if (!_begun)
        {
            _begun = true;

            if (_fromBox)
            {
                var drawn = BotMarket.FromBank(bot, _kind, _units);

                if (drawn <= 0 && Gather(body, _kind).Count == 0)
                {
                    return BotDoing.Failed($"could not take the {_label} out of its bank box");
                }

                if (drawn > 0)
                {
                    FromBox++;
                    FromBoxUnits += drawn;
                }
            }
        }

        if (!body.InRange(_shop.Location, BotShops.CounterReach))
        {
            // The distance the work itself asks for, on the line above. See BotArrival.Beside.
            // <b>Followed rather than aimed at, because a shopkeeper wanders.</b> A walk order to a point is
            // matched by that point, so a vendor that shuffles one tile behind its counter makes every beat a
            // fresh order: the journey is replaced, and with it every counter that would have said this was
            // going nowhere — MaxEmptyPlans, MaxPlansWithoutCloser and StallAttempts all reset before any of
            // them can fire. That is why a stuck errand of this kind is silent. On 03.09.2026 at 04:16 the
            // roll-call had Doran the Crafter on peddle for 210 seconds and Calla for 182, neither arriving,
            // neither failing, and nothing in the log at all. BotDoing.Walk's follow form matches on the
            // mobile itself, so the order stands still while the shopkeeper does not. Every errand that walks
            // to a counter is changed with it — sew, restock, acquire and inscribe have the same shape.
            return BotDoing.Walk(_shop.Map, _shop, BotArrival.Within(BotShops.CounterReach), $"to {_shop.Name} with {_label}");
        }

        // Never the bot's own allowance of a capped kind (06.10.2026, build next): a load drawn out of a full box may be of a kind the
        // kit keeps a number of, and the counter is shown every one of the kind in the pack. See BotPeddler.FullShare.
        var goods = Upto(Beyond(bot, Gather(body, _kind), _kind), Most);

        if (goods.Count == 0)
        {
            return BotDoing.Failed($"no {_label} left in the pack by the time it got here");
        }

        // Everything of the kind in the pack goes over; BotShops.Sell counts the units as it builds the order.
        _earned = BotShops.Sell(bot, _shop, goods, out var units, out _premium);

        // A coloured load's hold on the counters' purse ends here: what sold is off the purse itself (BotOre.Peddled).
        BotOre.Release(body);

        if (_earned <= 0)
        {
            // Standing at the counter holding goods it cannot sell: they stay in the pack, for the porter's next trip to the box.
            return BotDoing.Failed("the shopkeeper would not buy it after all");
        }

        _sold = units;

        // A capped kind's surplus gone over a counter, by kind, with the other routes it may take. See BotSurplus.
        BotSurplus.CountIfCapped(bot, BotSurplus.Shop, _kind, units);

        // The capital's price: a load drawn past a nearer buyer is counted as carried there, whatever the treasury could pay.
        if (_passed > 0 && BotCapital.In(_shop.Location))
        {
            BotCapital.Delivered(units, _earned + _premium, _passed);
        }

        // <b>And the rest of a hunter's loot this shopkeeper buys, on the same visit (02.10.2026, build next; Patrick's option 4).</b>
        // A peddle carried one kind, so a hunter with hides, bones, a gem and a dagger made four walks or none. Only a bot that keeps no
        // shop (BotPeddler.Hunter): a maker's goods the island is short of wait for its shift by the bank. Its own kit, what a near
        // want is waiting for, and its supper are kept.
        var extra = BotPeddler.Hunter(body) ? Rest(bot, body, _kind) : null;
        var also = "";

        if (extra is { Count: > 0 })
        {
            var more = BotShops.Sell(bot, _shop, extra, out var moreUnits, out var morePremium);

            if (more > 0)
            {
                AlsoSold++;
                AlsoUnits += moreUnits;
                AlsoGold += more;

                _earned += more;
                _premium += morePremium;
                _sold += moreUnits;

                also = $" and {moreUnits} other things of its loot";
            }
        }

        return _premium > 0
            ? BotDoing.Done($"{units} {_label}{also} for {_earned}gp and {_premium}gp more from the city at the capital's counter")
            : BotDoing.Done($"{units} {_label}{also} for {_earned}gp");
    }

    /// <summary>Visits on which a hunter sold other loot the same shopkeeper buys, and the things and gold. See <see cref="Rest"/>.</summary>
    public static long AlsoSold { get; private set; }

    public static long AlsoUnits { get; private set; }

    public static long AlsoGold { get; private set; }

    public static void ForgetCounts()
    {
        AlsoSold = 0;
        AlsoUnits = 0;
        AlsoGold = 0;
    }

    /// <summary>
    /// The rest of the pack a hunter may sell at this counter (never a coloured bar: see <see cref="BotOre.CounterRoom"/>): unbound standard loot not of <paramref name="but"/>, not on its own keep
    /// list (<see cref="BotUnload.Keeps"/>), not what a want within a carrier's reach is waiting for, and not raw meat (its supper; see
    /// <see cref="BotOven.Keeps"/>). The shopkeeper's own refusal is <see cref="BotShops.Sell"/>'s, which shows it only what it buys.
    /// </summary>
    private static List<Item> Rest(IBotWilful bot, Mobile body, Type but)
    {
        List<Item> found = [];

        var pack = body.Backpack;

        if (pack == null)
        {
            return found;
        }

        var bond = (body as BotMobile)?.Bond;
        var keep = BotUnload.Keeps(bot);
        List<Item> carried = [.. pack.Items];

        for (var i = 0; i < carried.Count; i++)
        {
            var item = carried[i];

            if (item == null || item.Deleted || !item.Movable || item is Gold || !item.IsStandardLoot() || BotBinding.IsBound(item, bond)
                || !BotShopkeep.Merchandise(item, bond))
            {
                continue;
            }

            var kind = item.GetType();

            // Coloured bars go over a counter only as a load of their own, offered against the counters' hourly purse (BotOre.CounterRoom).
            if (kind == but || keep != null && keep.ContainsKey(kind) || BotCourier.Deliverable(bot, kind) || Supper(kind)
                || BotOre.Coloured(kind))
            {
                continue;
            }

            found.Add(item);
        }

        return found;
    }

    /// <summary>What <see cref="Rest"/> fetches at this shopkeeper's own prices: the extra a hunter's visit is claimed at.</summary>
    internal static int RestWorth(IBotWilful bot, Mobile body, BaseVendor shop, Type but)
    {
        if (shop == null || body == null)
        {
            return 0;
        }

        var rest = Rest(bot, body, but);
        var worth = 0;

        for (var i = 0; i < rest.Count; i++)
        {
            if (BotShops.Buys(shop, rest[i], out var paying))
            {
                worth += paying * Math.Max(1, rest[i].Amount);
            }
        }

        return worth;
    }

    private static bool Supper(Type kind)
    {
        for (var i = 0; i < BotOven.Raw.Count; i++)
        {
            if (BotOven.Raw[i] == kind)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Of <paramref name="goods"/>, only what is over the number the bot keeps of the kind (<see cref="BotUnload.Keeps"/>): the
    /// allowance stays in the pack, a stack across the line is split by the engine's own split. A kind kept whole is kept whole; a
    /// kind with no number is all for sale, as before.
    /// </summary>
    private static List<Item> Beyond(IBotWilful bot, List<Item> goods, Type kind)
    {
        if (goods.Count == 0 || !BotUnload.Keeps(bot).TryGetValue(kind, out var cap) || cap <= 0)
        {
            return goods;
        }

        if (cap >= int.MaxValue)
        {
            return [];
        }

        var left = cap;
        List<Item> sell = [];

        for (var i = 0; i < goods.Count; i++)
        {
            var item = goods[i];
            var many = Math.Max(1, item.Amount);

            if (left <= 0)
            {
                sell.Add(item);

                continue;
            }

            if (many <= left)
            {
                left -= many;

                continue;
            }

            // LiftItemDupe leaves the original holding the amount asked for and puts the rest beside it: the original is the part sold.
            if (item.Stackable && Mobile.LiftItemDupe(item, many - left) != null)
            {
                sell.Add(item);
            }

            left = 0;
        }

        return sell;
    }

    /// <summary>
    /// No more than <paramref name="most"/> units of <paramref name="goods"/> (all of them for nought): a stack across the line is
    /// split by the engine's own split, the original holding the part sold.
    /// </summary>
    private static List<Item> Upto(List<Item> goods, int most)
    {
        if (most <= 0 || goods.Count == 0)
        {
            return goods;
        }

        var left = most;
        List<Item> sell = [];

        for (var i = 0; i < goods.Count && left > 0; i++)
        {
            var item = goods[i];
            var many = Math.Max(1, item.Amount);

            if (many <= left)
            {
                sell.Add(item);
                left -= many;

                continue;
            }

            if (item.Stackable && Mobile.LiftItemDupe(item, left) != null)
            {
                sell.Add(item);
            }

            left = 0;
        }

        return sell;
    }

    /// <summary>
    /// Everything of that kind in the pack, as objects, so the counter can be shown all of it — except what is
    /// bound, which rides in the pack to be kept and not to be sold.
    /// </summary>
    private static List<Item> Gather(Mobile body, Type kind)
    {
        List<Item> found = [];

        var pack = body.Backpack;

        if (pack == null || kind == null)
        {
            return found;
        }

        var bond = (body as BotMobile)?.Bond;
        List<Item> carried = [.. pack.Items];

        for (var i = 0; i < carried.Count; i++)
        {
            var item = carried[i];

            if (!item.Deleted && item.Movable && kind.IsInstanceOfType(item) && !BotBinding.IsBound(item, bond))
            {
                found.Add(item);
            }
        }

        return found;
    }
}
