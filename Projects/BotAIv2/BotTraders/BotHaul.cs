using System;
using System.Collections.Generic;
using Server.Logging;
using Server.Mobiles;

namespace Server.BotAI.V2;

/// <summary>
/// A trader's haul: the orders written into its book, the purchase, the road — by land, gate or sea — and the handing over to
/// each buyer in turn, each line struck as it ends.
///
/// <para>
/// <b>Patrick's order of 30.09.2026, point 5, and plan v2's point 3 (01.10.2026, build 385).</b> Taken: one line a buyer in the
/// book (<see cref="BotOrders.Write"/>), the units reserved on each want (<see cref="BotWant.Reserve"/>, kept by the market's
/// sweep through <see cref="IBotWantCarrier"/>) so no second carrier sets out for them, and the word said in the trade chat.
/// Then the source — the trader's pack, its box at the bank, a bot's shop or a shopkeeper — and the buyers, nearest first,
/// walked to while far and followed once near (<see cref="BotTrading.NearTiles"/>), handed over at a meeting
/// (<see cref="BotHandover.Exchange"/>) at the price agreed. Each line struck as delivered or as given up, with the reason.
/// </para>
///
/// <para>
/// <b>A promise, and held like one.</b> Steadfast, and <see cref="Committed"/> from the moment it is taken: the book is the
/// trader's word to its buyers, so a better price elsewhere does not take it off the road — only what will not wait (a flight,
/// a friend in trouble), after which it is taken up again. Its own clocks end it: <see cref="BotTrading.MostHaulMs"/> for the
/// whole, <see cref="BotTrading.ChaseMs"/> after a buyer that keeps walking away. Goods left when every order has ended go to
/// another open want for the same kind if one can be had (<see cref="Adopt"/>), else stay as the trader's stock for its shop.
/// </para>
/// </summary>
public sealed class BotHaul : BotDeed, IBotWantCarrier
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotHaul));

    /// <summary>The ledger's word for a haul.</summary>
    public const string Trade = "haul";

    private enum Step
    {
        ToSource,
        Deliver
    }

    private readonly Map _map;

    private readonly Type _kind;

    private readonly string _label;

    private readonly List<(BotWant Want, int Units, int Price)> _planned;

    private readonly List<BotTradeOrder> _orders = [];

    private readonly double _claim;

    private readonly double _minutes;

    private readonly double _total;

    private readonly int _spend;

    private BotHaulSource _source;

    private Step _step;

    /// <summary>What one unit in hand cost the trader: what it paid, or what its own stock is reckoned at.</summary>
    private int _cost;

    /// <summary>Units this haul has in hand for its orders: bought or taken from stock, not yet handed over.</summary>
    private int _held;

    private int _handed;

    private int _handedGold;

    private int _delivered;

    private int _leftWorth;

    private BotTradeOrder _current;

    private Point3D _aim;

    private bool _aiming;

    private bool _chasing;

    private long _chaseTick;

    private long _resumedTick;

    private long _activeMs;

    private bool _paused;

    private long _pausedTick;

    private bool _closed;

    private int _tries;

    private BotVoyage _sea;

    private IBotWilful _bot;

    /// <summary>Where the trader stood when it took the haul: a hand-over down the dungeon block from outside it clears that ground (<see cref="BotUnreached.Reached"/>).</summary>
    private Point3D _setOut;

    /// <summary>Whether this haul's own want is standing at the bank (a gathering): withdrawn whenever the haul ends.</summary>
    private bool _asking;

    private int _gatherFrom;

    private int _gatherWealth;

    private long _gatherBegun;

    /// <summary>The trader's own want for the kind, as it stood before the gathering topped it up; said again when the gathering ends. See Gather.</summary>
    private int _ownUnits;

    private int _ownOffer;

    public BotHaul(BotHaulPlan plan, Map map)
    {
        _map = map;
        _kind = plan.Kind;
        _label = BotShopkeep.Words(plan.Kind);
        _source = plan.Source;
        _planned = [.. plan.Orders];
        _spend = plan.Spend;
        _minutes = Math.Max(0.5, plan.After);
        _total = Math.Max(0.5, plan.ToSource + plan.After);

        // Profit a minute of the work once at the source: the appraisal adds the walk there itself (nearness), so the claim is
        // stated over what follows it and the two together come back to the whole haul's profit a minute.
        // The expected profit, not the most it could make: a gathering is reckoned at its yield (BotTrading.GatherYield), and
        // the plan's rate already says so over the whole haul.
        _claim = Math.Clamp(plan.Rate * _total / _minutes, BotTrading.LeastClaim, BotTrading.MostClaim);

        // Seeded from a real tick, never left at nought; Taken seeds it again.
        _resumedTick = Core.TickCount;
    }

    /// <summary>The kind this haul carries. Read by the porter's keep list (<see cref="BotTrading.Keep"/>).</summary>
    public Type Goods => _kind;

    /// <summary>Units still owed to the orders open in the book.</summary>
    public int Owed
    {
        get
        {
            var owed = 0;

            for (var i = 0; i < _orders.Count; i++)
            {
                if (!_orders[i].Closed)
                {
                    owed += _orders[i].Units;
                }
            }

            return owed;
        }
    }

    private int OpenCount
    {
        get
        {
            var open = 0;

            for (var i = 0; i < _orders.Count; i++)
            {
                if (!_orders[i].Closed)
                {
                    open++;
                }
            }

            return open;
        }
    }

    public override string Kind => Trade;

    public override Map Map => _map;

    public override Point3D Where =>
        _step == Step.ToSource ? _source.Where : _current?.Buyer?.Location ?? (_orders.Count > 0 ? _orders[0].Buyer?.Location ?? _source.Where : _source.Where);

    public override double Expects => _claim;

    public override double Minutes => _minutes;

    /// <summary>The merchant's eye: a check on every lot bought and every lot handed over. See <see cref="BotTrader"/>.</summary>
    public override SkillName? Trains => SkillName.ArmsLore;

    public override int Outlay => _spend;

    /// <summary>A shopkeeper's counter is paid out of the pack.</summary>
    public override bool AtCounter => _source.From == BotHaulFrom.Counter;

    public override double Coin => 1.0;

    /// <summary>Goods bought for orders that ended unfulfilled, kept as stock: worth what they cost, not a loss.</summary>
    public override int Made => _leftWorth;

    public override bool Steadfast => true;

    /// <summary>The book is the trader's word: no better price takes it off the road. See the remarks.</summary>
    public override bool Committed => true;

    public override double HoldsFor => _total;

    /// <summary>Afloat, or standing at the bank while the island brings what it asked for: both have their own clocks.</summary>
    public override bool Still => _asking || _sea is { Still: true };

    public override string Stage
    {
        get
        {
            if (_sea != null)
            {
                return _sea.Stage;
            }

            if (_asking)
            {
                return $"buying {_label} at {_source.Cost}gp at the bank from whoever brings it, for {OpenCount} orders in the book";
            }

            if (_step == Step.ToSource)
            {
                return $"going {_source.Name} for {Owed} {_label}, {OpenCount} orders in the book";
            }

            return _current is { Closed: false } order
                ? $"bringing {order.Units} {_label} to {order.BuyerName} at {order.Price}gp, order #{order.Number} ({_delivered} of {_orders.Count} handed over)"
                : $"carrying {_held} {_label}, {OpenCount} orders open";
        }
    }

    // ---- The market's sweep ---------------------------------------------------------------------------------------------

    public bool Carries(BotWant want)
    {
        for (var i = 0; i < _orders.Count; i++)
        {
            if (!_orders[i].Closed && ReferenceEquals(_orders[i].Want, want))
            {
                return true;
            }
        }

        return false;
    }

    public bool Stale => _paused && Core.TickCount - _pausedTick >= BotDeliver.GiveUpMs;

    // ---- Taken, put down, taken up, let go ------------------------------------------------------------------------------

    public override void Taken(IBotWilful bot)
    {
        _bot = bot;
        _resumedTick = Core.TickCount;
        _setOut = bot?.Self?.Location ?? Point3D.Zero;

        BotTrading.Taken++;

        var body = bot?.Self;
        var book = BotOrders.Of(body);

        if (book == null)
        {
            return;
        }

        for (var i = 0; i < _planned.Count; i++)
        {
            var (want, units, price) = _planned[i];

            if (!BotMarket.Stands(want) || want.Payable <= 0 || want.Buyer?.Self is not { Deleted: false } buyer)
            {
                continue;
            }

            units = Math.Min(units, want.Payable);

            // <b>The buyer's word to the trader is held from the moment the book is written (03.10.2026, build next).</b> Read again
            // here, because another trader may have written carriage against the same free money since this one planned: the units
            // its free money still carries at this price, and the carriage on them held with the want (BotWant.Premium).
            units = Math.Min(units, BotMarket.Affords(want, price));

            if (units <= 0)
            {
                BotTrading.Broke++;

                continue;
            }

            var order = book.Write(body, want, units, price, _source.Name);

            _orders.Add(order);
            want.Reserve(bot, units, price);

            Promise(body, buyer, order);
        }
    }

    /// <summary>The order said aloud: the trader's word, and — when it asked carriage over the buyer's offer — the buyer's.</summary>
    private void Promise(Mobile body, Mobile buyer, BotTradeOrder order)
    {
        var fill = new Dictionary<string, string>
        {
            ["buyer"] = buyer.Name,
            ["amount"] = order.Units.ToString(),
            ["item"] = order.Item,
            ["price"] = order.Price.ToString(),
            ["offer"] = order.Offered.ToString(),
            ["number"] = order.Number.ToString()
        };

        if (order.Price > order.Offered)
        {
            BotVoice.Say(
                body,
                "trade",
                BotVoice.Phrase("trader:carriage", null, body, fill)
                ?? $"{buyer.Name}, I can bring your {order.Units} {order.Item} at {order.Price} each: {order.Offered} was your word, the road is mine. Order #{order.Number} in my book."
            );

            BotVoice.Say(
                buyer,
                "trade",
                BotVoice.Phrase("trader:agreed", null, buyer, new Dictionary<string, string> { ["trader"] = body.Name, ["price"] = order.Price.ToString() })
                ?? $"Agreed, {body.Name}: {order.Price} each, brought to me."
            );

            return;
        }

        BotVoice.Say(
            body,
            "trade",
            BotVoice.Phrase("trader:take", null, body, fill)
            ?? $"{buyer.Name}, your {order.Units} {order.Item} at {order.Price} each are in my book as order #{order.Number}. I am on my way."
        );
    }

    public override void Paused(IBotWilful bot)
    {
        _activeMs += Math.Max(0L, Core.TickCount - _resumedTick);
        _paused = true;
        _pausedTick = Core.TickCount;
        _sea?.Paused(bot);
    }

    public override void Resumed(IBotWilful bot)
    {
        _resumedTick = Core.TickCount;
        _paused = false;
        _chasing = false;
        _aiming = false;
        _sea?.Resumed(bot);

        for (var i = 0; i < _orders.Count; i++)
        {
            if (!_orders[i].Closed && BotMarket.Stands(_orders[i].Want))
            {
                // What the buyer's money still carries at the agreed price: a pause past the sweep released the carriage held for
                // it, and the buyer may have spent it since (review of build next, F6). The meeting hands what is paid for.
                var held = Math.Min(_orders[i].Units, BotMarket.Affords(_orders[i].Want, _orders[i].Price, bot));

                if (held > 0)
                {
                    // The same trader holding its order again, not another take (03.10.2026, build next). See BotWant.Reserve.
                    _orders[i].Want.Reserve(bot, held, _orders[i].Price, true);
                }
            }
        }
    }

    /// <summary>
    /// Let go, however it ended: the reservations released, and — when it ended from outside the haul, by a death, a watchdog or
    /// a drop — every line still open struck with that reason. A haul that ended by its own word has already closed its book.
    /// </summary>
    public override void Drop(IBotWilful bot)
    {
        Settle(bot);

        for (var i = 0; i < _orders.Count; i++)
        {
            _orders[i].Want?.Release(bot);

            // A meeting still standing with any of the buyers is a meeting missed. See BotRendezvous.Gone.
            BotRendezvous.Gone(bot, _orders[i].Want?.Buyer);
        }

        if (_closed)
        {
            return;
        }

        _closed = true;

        var body = bot?.Self;
        var died = body is { Alive: false } || body is { Deleted: true };

        StrikeOpen(body, died ? "the trader died on the road" : "the haul was given up");
        Keep(bot);

        if (died)
        {
            BotTrading.Deaths++;
        }
        else
        {
            BotTrading.DroppedHauls++;
        }
    }

    /// <summary>
    /// The way it asked for turned out not to exist. To a counter: that counter is passed over and the next tried. To a buyer:
    /// that order is struck and the haul goes on to the next. Nothing left: the haul fails, its book closed here.
    /// </summary>
    public override bool Bend(IBotWilful bot)
    {
        var body = bot?.Self;
        var ledger = bot?.Resolve?.Ledger;

        if (_step == Step.ToSource)
        {
            if (_source.From == BotHaulFrom.Counter && _source.Vendor is { } vendor)
            {
                ledger?.Beware(BotShops.ShopKind, _map, vendor.Location);
            }

            if (Resource(bot, _source.From == BotHaulFrom.Counter ? _source.Vendor : null))
            {
                return true;
            }

            Fail(bot, "no way to anywhere selling it");

            return false;
        }

        if (_current is { Closed: false } order)
        {
            ledger?.Beware(Trade, _map, order.Buyer?.Location ?? Where);

            // A buyer down the dungeon block is marked for every trader and carrier, not only this trader's ledger (03.10.2026,
            // build next): seven traders learned Edith's pocket one by one on live 396. See BotUnreached.
            BotUnreached.NoWay(_map, order.Buyer, _setOut, body);

            BotOrders.Of(body)?.Strike(body, order, "no way through to the buyer");
            order.Want?.Release(bot);
            BotHauler.Shun(bot, order.Want);
            _current = null;
            _aiming = false;
            _chasing = false;

            if (OpenCount > 0 || _held > 0 && Adopt(bot, body as BotMobile))
            {
                return true;
            }
        }

        Fail(bot, "no way through to the buyers");

        return false;
    }

    private void Fail(IBotWilful bot, string why)
    {
        if (_closed)
        {
            return;
        }

        _closed = true;
        Settle(bot);
        StrikeOpen(bot?.Self, why);
        Keep(bot);

        if (_delivered > 0)
        {
            BotTrading.Finished++;
        }
        else
        {
            BotTrading.FailedHauls++;
        }
    }

    // ---- The work ---------------------------------------------------------------------------------------------------------

    public override BotDoing Advance(IBotWilful bot)
    {
        _bot = bot;

        if (bot?.Self is not BotMobile { Deleted: false, Alive: true } body)
        {
            return BotDoing.Failed("no body");
        }

        if (_closed)
        {
            return _delivered > 0 ? BotDoing.Done("the book is closed") : BotDoing.Failed("the book is closed");
        }

        var active = _activeMs + Math.Max(0L, Core.TickCount - _resumedTick);

        if (active >= BotTrading.MostHaulMs)
        {
            return End(bot, body, $"the road took longer than {BotTrading.MostHaulMs / 60000} minutes");
        }

        // A voyage in progress answers first; it hands the haul back on landing.
        if (_sea != null)
        {
            var sailing = Sail(bot, out var lost);

            if (sailing.Kind != BotDoingKind.None)
            {
                return sailing;
            }

            if (lost != null)
            {
                if (_step == Step.ToSource)
                {
                    return End(bot, body, lost);
                }

                if (_current is { Closed: false } stranded)
                {
                    BotOrders.Of(body)?.Strike(body, stranded, lost);
                    stranded.Want?.Release(bot);
                    _current = null;
                }
            }
        }

        // The road to the source, which may hand on to another source (a shop shut, a counter empty, a gathering topped up at a
        // counter) and ask again in the same beat; on to the buyers once the goods are in hand.
        for (var pass = 0; pass < 3 && _step == Step.ToSource; pass++)
        {
            var going = ToSource(bot, body);

            if (going.Kind != BotDoingKind.None)
            {
                return going;
            }
        }

        if (_step == Step.ToSource)
        {
            return BotDoing.Work($"looking for somewhere to buy {_label}");
        }

        return Deliver(bot, body);
    }

    /// <summary>The road to the source and the purchase there. None when the goods are in hand and the buyers come next.</summary>
    private BotDoing ToSource(IBotWilful bot, BotMobile body)
    {
        Prune(bot, body);

        // Every buyer gone while the bank was still buying for them: what was brought so far is in hand, and goes to whoever
        // else wants it (Adopt, once the haul's own want is withdrawn) or to the shop.
        if (OpenCount == 0 && _asking)
        {
            Gathered(bot, body);
            _step = Step.Deliver;

            return default;
        }

        if (OpenCount == 0 && !Adopt(bot, body))
        {
            return End(bot, body, "every order fell through before anything was bought");
        }

        switch (_source.From)
        {
            case BotHaulFrom.Stock:
                {
                    _cost = _source.Cost;
                    _held = Math.Min(BotHandover.Carried(bot, _kind), Owed);
                    BotTrading.FromStock++;
                    Checked(body);
                    _step = Step.Deliver;

                    return default;
                }
            case BotHaulFrom.Box:
                {
                    // Out of the box at the bank the haul was offered at, like a carrier's (BotDeliver): the box is reached only there.
                    var carried = BotHandover.Carried(bot, _kind);
                    var need = Math.Max(0, Owed - carried);

                    if (need > 0)
                    {
                        BotMarket.FromBank(bot, _kind, need);
                    }

                    _cost = _source.Cost;
                    _held = Math.Min(BotHandover.Carried(bot, _kind), Owed);
                    BotTrading.FromStock++;
                    Checked(body);
                    _step = Step.Deliver;

                    return default;
                }
        }

        if (_source.From == BotHaulFrom.Gather)
        {
            return Gather(bot, body);
        }

        var where = _source.Where;

        if (!BotGates.Joined(_map, body.Location, where))
        {
            return SetSail(bot, body, where, $"to buy {_label}") ?? End(bot, body, $"{_source.Name} lies across water and there is no ship");
        }

        if (_source.From == BotHaulFrom.Shop)
        {
            var front = _source.Front;

            if (!BotShopkeep.Live(front))
            {
                return Resource(bot, null) ? ToSource(bot, body) : End(bot, body, $"{front?.Keeper?.Name}'s shop shut and nobody else sells {_label}");
            }

            if (!body.InRange(front.Keeper.Location, BotShopkeep.ServeReach))
            {
                return BotDoing.Walk(_map, front.Keeper, BotArrival.Within(BotShopkeep.ServeReach), $"to {front.Keeper.Name}'s shop for {Owed} {_label}");
            }
        }
        else if (_source.Vendor is { } vendor)
        {
            if (vendor.Deleted)
            {
                return Resource(bot, vendor) ? ToSource(bot, body) : End(bot, body, $"nobody left selling {_label}");
            }

            if (!body.InRange(vendor.Location, BotShops.CounterReach))
            {
                return BotDoing.Walk(_map, vendor, BotArrival.Within(BotShops.CounterReach), $"to {vendor.Name} for {Owed} {_label}");
            }
        }

        return Buy(bot, body);
    }

    /// <summary>
    /// The gathering: at its town's bank the trader says a want of its own for what its far buyers asked, at their offer less its
    /// least markup, and stands while the island's carriers bring it (each meeting a <see cref="BotDeliver"/> of theirs, paid
    /// hand to hand). It stops at <see cref="BotTrading.GatherMs"/> — later while a carrier is still on its way, up to twice that
    /// — or as soon as the orders are covered; withdraws its want; tops up at a counter if one sells the rest under the orders'
    /// prices; and sets out with what it has.
    /// </summary>
    private BotDoing Gather(IBotWilful bot, BotMobile body)
    {
        var carried = BotHandover.Carried(bot, _kind);
        var active = _activeMs + Math.Max(0L, Core.TickCount - _resumedTick);

        if (!_asking)
        {
            if (!body.InRange(_source.Where, BotTrading.BankStand))
            {
                return BotDoing.Walk(_map, _source.Where, BotArrival.Within(BotTrading.BankStand), $"to the bank to buy {Owed} {_label} from the bots of the town");
            }

            var need = Owed - _held;

            if (need <= 0)
            {
                _step = Step.Deliver;

                return default;
            }

            // <b>The trader's own standing want is not a refusal (02.10.2026, build 389).</b> Every trader that cooks keeps a want
            // for raw ribs open, and raw ribs is what the island asks for most: all 38 gatherings refused here on build 388 were
            // "could not say the want" for a trader that already wanted ribs for its own pan — 48 % of every failed haul, and the
            // haul share 79.8 % against 88.6 % without them. The standing want is topped up by what the orders need, at the price
            // the trader has learned (BotMarket.Ask tops rather than overwrites), and said again at its own size when the
            // gathering ends (StopAsking), so the pan is not left wanting what the buyer paid for.
            var own = BotMarket.Wanted(bot, _kind);

            _ownUnits = own?.Amount ?? 0;
            _ownOffer = own?.Offer ?? 0;

            if (BotMarket.Ask(bot, _kind, need, _source.Cost) == null)
            {
                return TopUp(bot, body, "could not say the want at the bank");
            }

            if (own != null)
            {
                BotTrading.GatherOverOwn++;
            }

            _asking = true;
            _gatherFrom = carried;
            _gatherWealth = BotYield.Wealth(body);
            _gatherBegun = active;

            BotTrading.Gathers++;

            logger.Information(
                "{Name} the trader is buying {Units} {Item} at {Bid}gp at the bank in {Town} for {Orders} orders from buyers no carrier here reaches",
                body.Name,
                need,
                _kind.Name,
                _source.Cost,
                BotTowns.Of(body.Location)?.Name ?? "the country",
                OpenCount
            );

            return BotDoing.Work($"buying {_label} at the bank");
        }

        var got = Math.Max(0, carried - _gatherFrom);
        var waited = active - _gatherBegun;
        var coming = BotMarket.Wanted(bot, _kind)?.Coming ?? 0;

        if (got < Owed - _held && (waited < BotTrading.GatherMs || coming > 0 && waited < BotTrading.GatherMs * 2L))
        {
            return BotDoing.Work($"buying {_label} at the bank: {got} brought so far");
        }

        got = Gathered(bot, body);

        return TopUp(bot, body, got > 0 ? null : "nobody brought any");
    }

    /// <summary>The gathering closed: the want withdrawn, what was brought taken into hand at what it came to. Returns how many.</summary>
    private int Gathered(IBotWilful bot, BotMobile body)
    {
        if (!_asking)
        {
            return 0;
        }

        StopAsking(bot);

        var got = Math.Max(0, BotHandover.Carried(bot, _kind) - _gatherFrom);
        var paid = Math.Max(0, _gatherWealth - BotYield.Wealth(body));

        if (got <= 0)
        {
            BotTrading.GatherEmpty++;

            // Nobody in this town holds the kind: no trader gathers for it again for a while, and this trader leaves these
            // buyers' wants alone (01.10.2026, build 386: Liftrasa stood for the same lightning scroll nine times in four hours,
            // because Trim struck the lines without shunning the want and the next look took it again).
            BotHauler.ShunGathering(_kind);

            for (var i = 0; i < _orders.Count; i++)
            {
                if (!_orders[i].Closed)
                {
                    BotHauler.Shun(bot, _orders[i].Want);
                }
            }

            return 0;
        }

        var had = _held;

        _held += got;
        _cost = Math.Max(1, (int)Math.Round((had * (double)_cost + paid) / Math.Max(1, _held)));

        BotTrading.GatheredUnits += got;
        BotTrading.GatheredGold += paid;

        // Each meeting at the bank booked the trader's payment aside, as a buyer's purchase in the middle of other work
        // (BotDeliver → BotHandover.Exchange); here the purchase is this haul's own cost, so it is put back into the haul's takings.
        BotYield.Aside(body, -paid);

        Checked(body);

        return got;
    }

    /// <summary>
    /// After a gathering, or instead of one: the rest of what the orders want bought at the nearest counter when it sells under
    /// the orders' own prices, else the orders brought down to what is in hand. On to the buyers either way.
    /// </summary>
    private BotDoing TopUp(IBotWilful bot, BotMobile body, string why)
    {
        var need = Owed - _held;

        if (need > 0)
        {
            // The top-up at a shopkeeper's counter is passed over into a glut, as the proposer passes the counter over (01.10.2026,
            // build 385 review): what the gathering did not bring, the orders are brought down to.
            var glutted = BotGlut.Glutted(_kind);
            var vendor = glutted ? null : BotShops.Nearest(bot, _kind);
            var price = vendor == null ? 0 : BotShops.Price(vendor, _kind);

            if (glutted)
            {
                BotTrading.Glutted++;
            }

            if (price > 0 && price < LeastPrice())
            {
                _source = new BotHaulSource { From = BotHaulFrom.Counter, Vendor = vendor, Where = vendor.Location, Cost = price, Shelf = need };
                _step = Step.ToSource;

                BotTrading.GatherTopped++;

                var book = BotOrders.Of(body);

                for (var i = 0; i < _orders.Count; i++)
                {
                    book?.Rebought(body, _orders[i], _source.Name);
                }

                return default;
            }

            // Struck for what happened: twelve empty gatherings on build 386 read "the counter ran short" in the book.
            Trim(bot, body, need, why ?? "the counter ran short");
        }

        if (_held <= 0 || OpenCount == 0)
        {
            return End(bot, body, why ?? "nobody brought any");
        }

        _step = Step.Deliver;

        return default;
    }

    /// <summary>The lowest price an open order was agreed at.</summary>
    private int LeastPrice()
    {
        var least = int.MaxValue;

        for (var i = 0; i < _orders.Count; i++)
        {
            if (!_orders[i].Closed && _orders[i].Price < least)
            {
                least = _orders[i].Price;
            }
        }

        return least;
    }

    /// <summary>A gathering cut short from outside: what was brought taken into hand (so it is kept as stock), the want withdrawn.</summary>
    private void Settle(IBotWilful bot)
    {
        if (bot?.Self is BotMobile body)
        {
            Gathered(bot, body);
        }
        else
        {
            StopAsking(bot);
        }
    }

    /// <summary>The haul's own want at the bank taken back. Nothing was held for it, so nothing is refunded.</summary>
    private void StopAsking(IBotWilful bot)
    {
        if (!_asking)
        {
            return;
        }

        _asking = false;
        BotMarket.Withdrawn(bot, _kind, BotWithdraw.Closed);

        // The trader's own want, said again at its own size and price. See Gather.
        if (_ownUnits > 0 && _ownOffer > 0)
        {
            BotMarket.Ask(bot, _kind, _ownUnits, _ownOffer);
        }

        _ownUnits = 0;
        _ownOffer = 0;
    }

    /// <summary>At the counter or the shop: buy what the open orders want, try another counter if this one has none.</summary>
    private BotDoing Buy(IBotWilful bot, BotMobile body)
    {
        var need = Owed - _held;

        if (need <= 0)
        {
            _step = Step.Deliver;

            return default;
        }

        var before = BotYield.Wealth(body);
        var got = 0;
        string refused;

        if (_source.From == BotHaulFrom.Shop)
        {
            got = BotShopkeep.Serve(bot, _source.Front, _kind, need, _source.Cost, false, out _, out refused);

            if (got > 0)
            {
                BotTrading.FromShops++;
            }
        }
        else
        {
            got = BotShops.Buy(bot, _source.Vendor, _kind, need, out refused);

            if (got > 0)
            {
                BotTrading.FromCounters++;
            }
        }

        if (got <= 0)
        {
            return Resource(bot, _source.Vendor) ? ToSource(bot, body) : End(bot, body, $"could not buy {_label}: {refused ?? "nobody sold it"}");
        }

        // What it actually came to, read off the purse: a town's standing moves a shopkeeper's price a little either way.
        var paid = Math.Max(0, before - BotYield.Wealth(body));
        var had = _held;

        _held += got;
        _cost = Math.Max(1, (int)Math.Round((had * (double)_cost + paid) / Math.Max(1, _held)));

        BotTrading.BoughtUnits += got;
        BotTrading.BoughtGold += paid;

        Checked(body);

        logger.Information(
            "{Name} the trader bought {Units} {Item} {Where} for {Paid}gp against {Orders} orders in its book",
            body.Name,
            got,
            _kind.Name,
            _source.Name,
            paid,
            OpenCount
        );

        // Short: the last orders written give way first, each brought down or struck.
        if (got < need)
        {
            Trim(bot, body, need - got);
        }

        _step = Step.Deliver;

        return default;
    }

    /// <summary>
    /// Another counter for the same kind, after this one could not be reached or had none: <see cref="BotShops.Next"/>, at most
    /// twice a haul. A shop that shut sends the haul to the nearest shopkeeper instead.
    /// </summary>
    private bool Resource(IBotWilful bot, BaseVendor emptied)
    {
        if (_tries >= 2 || _source.From is BotHaulFrom.Stock or BotHaulFrom.Box)
        {
            return false;
        }

        var next = emptied != null ? BotShops.Next(bot, emptied, _kind, ref _tries) : BotShops.Nearest(bot, _kind);

        if (emptied == null)
        {
            _tries++;
        }

        if (next == null || ReferenceEquals(next, emptied))
        {
            return false;
        }

        var price = BotShops.Price(next, _kind);

        if (price <= 0)
        {
            return false;
        }

        _source = new BotHaulSource { From = BotHaulFrom.Counter, Vendor = next, Where = next.Location, Cost = price, Shelf = Owed };
        _step = Step.ToSource;

        BotTrading.Repicked++;

        var book = BotOrders.Of(bot?.Self);

        for (var i = 0; i < _orders.Count; i++)
        {
            book?.Rebought(bot?.Self, _orders[i], _source.Name);
        }

        return true;
    }

    /// <summary>The buyers, nearest first: walked to while far, followed once near, handed their goods at the meeting.</summary>
    private BotDoing Deliver(IBotWilful bot, BotMobile body)
    {
        var book = BotOrders.Of(body);

        // At most one ending a beat per order, and a guard against a loop that strikes and adopts the same want forever.
        for (var guard = 0; guard < 8; guard++)
        {
            Prune(bot, body);

            var order = _current is { Closed: false, Buyer.Alive: true } ? _current : Nearest(body);

            if (order == null)
            {
                // Every buyer left is lying dead and waited for (Prune): the trader stands where it is until one rises.
                if (OpenCount > 0)
                {
                    _current = null;

                    return BotDoing.Work($"waiting for a buyer to rise, {_held} {_label} in hand");
                }

                if (BotHandover.Carried(bot, _kind) > 0 && _held > 0 && Adopt(bot, body))
                {
                    continue;
                }

                return End(bot, body, null);
            }

            if (_current != order)
            {
                _current = order;
                _aiming = false;
                _chasing = false;
            }

            var buyer = order.Buyer;

            if (!BotGates.Joined(_map, body.Location, buyer.Location))
            {
                var sailing = SetSail(bot, body, buyer.Location, $"to {order.BuyerName}");

                if (sailing != null)
                {
                    return sailing.Value;
                }

                book?.Strike(body, order, "across the water, and no ship");
                order.Want?.Release(bot);
                BotHauler.Shun(bot, order.Want);
                _current = null;

                continue;
            }

            if (!body.InRange(buyer.Location, BotHandover.MeetTiles))
            {
                if (!body.InRange(buyer.Location, BotTrading.NearTiles))
                {
                    _chasing = false;

                    // Walked to where the buyer was, and aimed again only when it has gone well away from there or the trader
                    // arrived without finding it: a walk order must stay the same from beat to beat (BotDoing.Matches).
                    if (!_aiming || !Utility.InRange(_aim, buyer.Location, BotTrading.NearTiles) || body.InRange(_aim, BotTrading.NearTiles / 2))
                    {
                        _aim = buyer.Location;
                        _aiming = true;
                    }

                    return BotDoing.Walk(_map, _aim, BotArrival.Within(Math.Max(2, BotTrading.NearTiles / 2)), $"to {order.BuyerName} with {order.Units} {_label}");
                }

                if (!_chasing)
                {
                    _chasing = true;
                    _chaseTick = Core.TickCount;
                }
                else if (Core.TickCount - _chaseTick >= BotTrading.ChaseMs)
                {
                    BotRendezvous.Gone(bot, order.Want?.Buyer);
                    book?.Strike(body, order, "could not catch up with the buyer");
                    order.Want?.Release(bot);
                    BotHauler.Shun(bot, order.Want);
                    _current = null;

                    continue;
                }

                // <b>A meeting, once near (02.10.2026).</b> The buyer is told and comes to the trader; on unsafe ground both go to the
                // guarded place the meeting names. The same as a carrier's delivery: see BotDeliver and BotRendezvous.
                if (BotRendezvous.Running && order.Want?.Buyer is { } wanting && BotRendezvous.Call(bot, wanting, order.Want, order.Units) is { Place: var place } && place != Point3D.Zero)
                {
                    return body.InRange(place, 2)
                        ? BotDoing.Work($"waiting at the meeting place for {order.BuyerName} with {order.Units} {_label}")
                        : BotDoing.Walk(_map, place, BotArrival.Within(2), $"to the meeting place for {order.BuyerName} with {order.Units} {_label}");
                }

                // Kept up with (12.10.2026, w7): a plan that reaches the buyer is a way however far it walks on; ChaseMs ends the chase.
                return BotDoing.WalkAfter(_map, buyer, BotArrival.Within(BotHandover.MeetTiles), $"after {order.BuyerName} with {order.Units} {_label}");
            }

            Hand(bot, body, order);
        }

        return BotDoing.Work($"settling the book with {_held} {_label} in hand");
    }

    /// <summary>The meeting: goods for coin at the price agreed, the want brought down, the line struck as delivered.</summary>
    private void Hand(IBotWilful bot, BotMobile body, BotTradeOrder order)
    {
        var book = BotOrders.Of(body);
        var buyer = order.Buyer;
        var carried = BotHandover.Carried(bot, _kind);

        _current = null;
        _chasing = false;
        _aiming = false;

        if (carried <= 0)
        {
            _held = 0;
            BotRendezvous.Gone(bot, order.Want?.Buyer);
            book?.Strike(body, order, "nothing left to hand over");
            order.Want?.Release(bot);

            return;
        }

        var units = Math.Min(order.Units, carried);
        var given = BotHandover.Exchange(bot, buyer, _kind, units, order.Price, true, $"order #{order.Number} in a trader's book", out var paid, out var refused);

        if (given <= 0)
        {
            BotRendezvous.Gone(bot, order.Want?.Buyer);

            // A buyer that cannot pay at the meeting cannot honour the want either: it stands down, as at a carrier's meeting —
            // when it cannot pay for one at its own offer (build next). A buyer whose free money will not carry the carriage over the
            // offer still honours the want at the offer, for a carrier that asks that; only this order is struck. See
            // BotMarket.Affords.
            var unpaid = order.Want != null ? BotMarket.Affords(order.Want, order.Want.Offer) <= 0 : BotYield.Spendable(buyer) < order.Price;
            var carriage = !unpaid && order.Want != null && BotMarket.Affords(order.Want, order.Price, bot) <= 0;

            if (unpaid && buyer is { Alive: true } && order.Want?.Buyer is { } wanting)
            {
                BotMarket.Withdrawn(wanting, _kind, BotWithdraw.Unpaid);
            }

            logger.Information(
                "{Name} the trader could not hand order #{Number} to {Buyer}: {Why}",
                body.Name,
                order.Number,
                order.BuyerName,
                refused ?? "refused"
            );

            book?.Strike(body, order, unpaid ? "the buyer could not pay" : carriage ? "the buyer's free money would not carry the carriage" : "refused at the meeting");
            order.Want?.Release(bot);
            BotHauler.Shun(bot, order.Want);

            return;
        }

        BotMarket.Received(order.Want, given, paid, body, BotRoute.Trader);
        order.Want?.Release(bot);

        // Handed down the dungeon block by a haul taken outside it: the way exists. See BotUnreached.
        BotUnreached.Reached(_map, buyer, _setOut);

        // The meeting, if one was called, is met. See BotRendezvous.Handed.
        BotRendezvous.Handed(order.Want?.Buyer);

        var cost = given * _cost;

        book?.Deliver(body, order, given, paid, cost);

        _held = Math.Max(0, _held - given);
        _handed += given;
        _handedGold += paid;
        _delivered++;

        BotTrading.Handovers++;
        BotTrading.HandedUnits += given;
        BotTrading.HandedGold += paid;
        BotTrading.HandedCost += cost;

        // Paid, and the takings above the pocket float go into the account at once (build 388). See BotTrading.PutAway.
        BotTrading.PutAway(body);

        if (order.Price > order.Offered)
        {
            BotTrading.Agreed++;
            BotTrading.Carriage += (long)(order.Price - order.Offered) * given;
        }

        Checked(body);
    }

    /// <summary>Lines whose want or buyer is gone, struck; their units released.</summary>
    private void Prune(IBotWilful bot, BotMobile body)
    {
        var book = BotOrders.Of(body);

        for (var i = 0; i < _orders.Count; i++)
        {
            var order = _orders[i];

            if (order.Closed)
            {
                continue;
            }

            // <b>A dead buyer is waited for, a while.</b> A bot rises about a minute after it falls (61 s from death to its next
            // work on 384, at 0.96 deaths an online hour), and its want stands meanwhile: striking the line at the first beat it
            // lay dead would have thrown away one order in five on a quarter-hour haul. Struck after DeadWaitMs.
            if (order.Buyer is { Deleted: false, Alive: false })
            {
                if (!order.DeadSeen)
                {
                    order.DeadSeen = true;
                    order.DeadSince = Core.TickCount;
                }

                if (Core.TickCount - order.DeadSince < BotTrading.DeadWaitMs && BotMarket.Stands(order.Want))
                {
                    continue;
                }
            }
            else
            {
                order.DeadSeen = false;
            }

            var why = order.Buyer switch
            {
                null or { Deleted: true } => "the buyer is gone",
                { Alive: false } => "the buyer died",
                { } b when b.Map != _map => "the buyer went to another land",
                _ => !BotMarket.Stands(order.Want) ? "the want was met or withdrawn" : null
            };

            // <b>And a buyer whose pack will not take the goods, before they are bought and before the walk (06.10.2026, work17).</b>
            // Another carrier's refusal may mark it while this haul is on the road; the hand-over would be refused at the meeting
            // and the buyer's walk to it (a meet) fail with it. Struck here, the goods go to another want (Adopt) or stay as stock.
            if (why == null && order.Buyer is { Alive: true } full && (BotRoom.Shut(full) || !BotRoom.Receives(full, _kind)))
            {
                BotRoom.Note(full);
                BotRoom.HaulOrderStruck();
                BotRendezvous.Gone(bot, order.Want?.Buyer);
                why = "the buyer's pack has no room";
            }

            if (why == null)
            {
                continue;
            }

            book?.Strike(body, order, why);
            order.Want?.Release(bot);

            if (ReferenceEquals(_current, order))
            {
                _current = null;
            }
        }
    }

    private BotTradeOrder Nearest(Mobile body)
    {
        BotTradeOrder best = null;
        var bestAway = int.MaxValue;

        for (var i = 0; i < _orders.Count; i++)
        {
            var order = _orders[i];

            // A buyer lying dead is gone back to when it has risen. See Prune.
            if (order.Closed || order.Buyer is not { Alive: true })
            {
                continue;
            }

            var away = Math.Max(Math.Abs(order.Buyer.X - body.X), Math.Abs(order.Buyer.Y - body.Y));

            if (away < bestAway)
            {
                best = order;
                bestAway = away;
            }
        }

        return best;
    }

    /// <summary>The purchase came up short: the last orders written are brought down, or struck when nothing is left for them.</summary>
    private void Trim(IBotWilful bot, BotMobile body, int short_, string why = "the counter ran short")
    {
        var book = BotOrders.Of(body);

        for (var i = _orders.Count - 1; i >= 0 && short_ > 0; i--)
        {
            var order = _orders[i];

            if (order.Closed)
            {
                continue;
            }

            if (order.Units <= short_)
            {
                short_ -= order.Units;
                book?.Strike(body, order, why);
                order.Want?.Release(bot);

                continue;
            }

            order.Units -= short_;
            short_ = 0;

            if (BotMarket.Stands(order.Want))
            {
                // The same trader holding fewer, not another take (review of build next, F8). See BotWant.Reserve.
                order.Want.Reserve(bot, order.Units, order.Price, true);
            }
        }
    }

    /// <summary>
    /// Another open want for the same kind, answered from what this haul has in hand — or, before buying, from the same source:
    /// a buyer died or got its goods elsewhere, and the goods (or the walk to them) need not go to waste. Never under cost once
    /// bought. The nearest worth most a minute, one at a time.
    /// </summary>
    private bool Adopt(IBotWilful bot, BotMobile body)
    {
        if (body == null)
        {
            return false;
        }

        var bought = _step == Step.Deliver;
        var spare = bought ? Math.Min(_held, BotHandover.Carried(bot, _kind)) - Owed : _source.Shelf - Owed;
        var cost = bought ? Math.Max(1, _cost) : _source.Cost;

        if (spare <= 0)
        {
            return false;
        }

        var capital = BotYield.Spendable(body) - BotTrading.LeastCapital;

        if (!bought && _source.Buys)
        {
            spare = Math.Min(spare, capital / Math.Max(1, cost));
        }

        var now = Core.TickCount;
        var wants = BotMarket.Wants;
        var own = BotHauler.OwnKinds(bot);
        BotWant best = null;
        var bestUnits = 0;
        var bestPrice = 0;
        var bestRate = 0.0;

        for (var i = 0; i < wants.Count; i++)
        {
            var want = wants[i];

            if (want.Kind != _kind || Carries(want) || !BotHauler.Answerable(bot, body, _map, want, now, own))
            {
                continue;
            }

            var buyer = want.Buyer.Self;

            // <b>Leftovers taken up from inside the dungeon block go nowhere outside its land (03.10.2026, build next).</b> On live
            // 396 nine orders were written by traders standing down there — most in the pocket at (6139, 1432) they had walked into
            // after a buyer — for buyers on the island, and eight were struck "no way through". See BotUnreached.
            if (BotUnreached.Refuses(_map, buyer, bought ? body.Location : _source.Where))
            {
                BotTrading.Unreached++;
                want.Passed(BotPass.Sealed);

                continue;
            }

            if (BotUnreached.Shut(_map, bought ? body.Location : _source.Where, buyer.Location))
            {
                BotTrading.Stranded++;
                want.Passed(BotPass.Sealed);

                continue;
            }

            if (!BotHauler.Road(body, _map, body.Location, buyer.Location, out var road)
                || BotPeril.Overwhelms(_map, buyer.Location, body.Location, BotQuad.Strength(body), out _, false)
                || BotHauler.Deadly(body, _map, body.Location, buyer.Location))
            {
                continue;
            }

            var units = Math.Min(spare, want.Payable);

            if (BotTrading.OverItsCeiling(want, cost))
            {
                continue;
            }

            var price = BotTrading.Quote(cost, want.Offer, BotTrading.Alternative(buyer, _kind, Math.Max(1, units), want));

            units = Math.Min(units, BotMarket.Affords(want, price));

            if (units <= 0 || price < cost || !bought && price <= cost)
            {
                continue;
            }

            var rate = units * (double)price / Math.Max(0.5, road + BotTrading.HandMinutes);

            if (rate > bestRate)
            {
                best = want;
                bestUnits = units;
                bestPrice = price;
                bestRate = rate;
            }
        }

        if (best?.Buyer?.Self is not { } chosen)
        {
            return false;
        }

        var order = BotOrders.Of(body)?.Write(body, best, bestUnits, bestPrice, bought ? "from what it carries" : _source.Name);

        if (order == null)
        {
            return false;
        }

        _orders.Add(order);
        best.Reserve(bot, bestUnits, bestPrice);
        BotTrading.Adopted++;

        Promise(body, chosen, order);

        return true;
    }

    // ---- The sea --------------------------------------------------------------------------------------------------------

    /// <summary>
    /// A voyage begun to a place no road or gate reaches, for a trader well off enough to sail (<see cref="BotTrading.CanSail"/>):
    /// the ordinary one (<see cref="BotVoyage"/> — the ship bought if it has none, the dock, the lane, the landing), run inside
    /// the haul until it lands. Null when it cannot sail.
    /// </summary>
    private BotDoing? SetSail(IBotWilful bot, BotMobile body, Point3D to, string why)
    {
        if (!BotTrading.CanSail(body))
        {
            return null;
        }

        var town = BotTowns.Of(to) ?? BotTowns.Nearest(to);
        var dock = town == null ? null : BotDocks.Of(town);

        if (dock == null || !BotSea.Plan(_map, body.Location, dock, out var from, out var lane, out _, true))
        {
            return null;
        }

        _sea = new BotVoyage(_map, from, dock, lane, town, to, $"a trader's haul {why}", false);
        _sea.Taken(bot);

        BotTrading.Sailings++;

        logger.Information(
            "{Name} the trader sails from {From} to {To} {Why}, with {Orders} orders in its book",
            body.Name,
            from.Name,
            dock.Name,
            why,
            OpenCount
        );

        var sailing = Sail(bot, out var lost);

        if (sailing.Kind != BotDoingKind.None)
        {
            return sailing;
        }

        return lost == null ? BotDoing.Work($"landed at {dock.Name}") : null;
    }

    /// <summary>The voyage's own answer while it sails; none once it has landed or was lost (and then why).</summary>
    private BotDoing Sail(IBotWilful bot, out string lost)
    {
        lost = null;

        var doing = _sea.Advance(bot);

        switch (doing.Kind)
        {
            case BotDoingKind.Done:
                {
                    _sea = null;
                    BotTrading.Landed++;

                    return default;
                }
            case BotDoingKind.Failed:
                {
                    _sea = null;
                    BotTrading.SeaLost++;
                    lost = $"the voyage failed: {doing.Note}";

                    return default;
                }
            default:
                {
                    return doing;
                }
        }
    }

    // ---- Endings --------------------------------------------------------------------------------------------------------

    /// <summary>
    /// The haul's own ending: every line still open struck with <paramref name="why"/>, what is left over kept as stock. Finished
    /// when at least one order was handed over — the work a trader exists for happened — failed when none was.
    /// </summary>
    private BotDoing End(IBotWilful bot, BotMobile body, string why)
    {
        _closed = true;
        Gathered(bot, body);

        StrikeOpen(body, why ?? "nothing more to do");

        var left = Keep(bot);

        if (_delivered > 0)
        {
            BotTrading.Finished++;

            return BotDoing.Done(
                $"handed over {_delivered} of {_orders.Count} orders, {_handed} {_label} for {_handedGold}gp"
                + (left > 0 ? $"; {left} kept as stock for the shop" : "")
            );
        }

        BotTrading.FailedHauls++;

        return BotDoing.Failed(why ?? $"none of {_orders.Count} orders handed over");
    }

    private void StrikeOpen(Mobile body, string why)
    {
        var book = BotOrders.Of(body);

        for (var i = 0; i < _orders.Count; i++)
        {
            if (!_orders[i].Closed)
            {
                book?.Strike(body, _orders[i], why);
                _orders[i].Want?.Release(_bot);
            }
        }
    }

    /// <summary>
    /// What the haul bought and could not hand over, kept as stock and counted as made (<see cref="Made"/>) — at what it cost, or
    /// at the balance's reading of the kind when that is lower (<see cref="BotGlut.Yield"/>, 01.10.2026, build 385 review): a
    /// leftover of a glutted kind is one more on the pile, and a ledger that read it at cost would have learned that a haul
    /// which buys eighty and hands over twenty pays as well as one that hands over eighty.
    /// </summary>
    private int Keep(IBotWilful bot)
    {
        if (_held <= 0 || _source.From is BotHaulFrom.Stock or BotHaulFrom.Box)
        {
            return 0;
        }

        var left = Math.Min(_held, BotHandover.Carried(bot, _kind));
        var cost = Math.Max(1, _cost);

        _leftWorth = Math.Min(left * cost, BotGlut.Yield(_kind, left, cost));
        BotTrading.Leftover += left;

        return left;
    }

    /// <summary>The merchant's eye on a lot. See <see cref="BotTrader"/>.</summary>
    private static void Checked(Mobile body) => body?.CheckSkill(SkillName.ArmsLore, 0.0, 100.0);
}
