using System;
using Server.Mobiles;

namespace Server.BotAI.V2;

/// <summary>
/// Getting hold of one spell the book is short of: off a shelf, at a bot's shop by the bank, or by asking the
/// population for it aloud and waiting for a scribe to bring one.
///
/// <para>
/// <b>Three routes, and which one exists is a fact about the spell rather than a choice.</b> The first three
/// circles are on a shopkeeper's shelf for twelve, twenty-two and thirty-two gold. Above that no shop in the
/// era sells them, so either a scribe has already written one and keeps shop with it by the bank, or nobody has and
/// the only thing left to do is ask — a spoken want that says what this bot will pay and waits for somebody to find
/// it worth writing and carrying over (01.10.2026: the stall and the escrow went with the auction).
/// </para>
///
/// <para>
/// <b>It is priced as an errand, because that is what it is.</b> Buying creates no wealth: coin becomes goods,
/// and money put down on a want is coin becoming a claim. So the takings are declared as what was spent and
/// the trip comes out at about nothing per minute — never punished, and never preferred over work that
/// produces something. A mage with a pen will always rather write; a healer with no trade at all has this and
/// the shops, which is an honest account of what a healer is on this shard today.
/// </para>
///
/// <para>
/// <b>Nothing here prices a spell.</b> The want's opening offer is what the engine's own shelf charges for
/// that circle, continued one step per circle above the third, and from then on the market moves it. What a
/// fourth-circle scroll is worth is whatever makes somebody write one.
/// </para>
/// </summary>
public sealed class BotAcquire : BotDeed
{
    /// <summary>The ledger's key. A kind of work — getting a spell — not one spell.</summary>
    public const string Trade = "acquire";

    /// <summary>
    /// What it is reckoned at before experience corrects it. The same as an errand to the shops, deliberately:
    /// it produces nothing, and the only reason to do it is that the book is short.
    /// </summary>
    public static double Prior { get; set; } = 12.0;

    /// <summary>How long it is expected to take once the bot is there.</summary>
    public static double WorkMinutes { get; set; } = 2.0;

    /// <summary>How many of a scroll a want asks for at a time. One: a book needs one of each.</summary>
    public static int Ask { get; set; } = 1;

    private enum Route
    {
        /// <summary>The scroll is already in the pack — a carrier brought it, or the bot had one. Learn it.</summary>
        Delivered,

        /// <summary>A shopkeeper sells it. Walk over and buy.</summary>
        Counter,

        /// <summary>Another bot keeps shop with one by the bank. Walk over and buy it there, hand to hand.</summary>
        Shop,

        /// <summary>Nobody has one. Ask, with the money down.</summary>
        Board
    }

    private readonly Route _route;

    private readonly Type _kind;

    private readonly int _spell;

    private BaseVendor _shop;

    /// <summary>Times this errand has been sent on to another shopkeeper because the shelf emptied. See BotShops.Next.</summary>
    private int _repicks;

    private readonly BotStorefront _front;

    private readonly Map _map;

    private readonly Point3D _where;

    private int _price;

    private int _paid;

    private bool _learned;

    /// <summary>
    /// What the scroll is being bought for, which is two entirely different errands wearing one name.
    ///
    /// <para>
    /// <b>A scroll bought to learn goes into a book; a scroll bought to cast stays in the pack.</b>
    /// <c>BotSeeker</c> fills a caster's book, <c>BotArmoury</c> stocks whatever a bot can throw — and both
    /// were building the same undertaking, which then tried to write every scroll into a book. What that
    /// produced was 233 of 430 rounds in a session reporting "the book would not take it" about warriors who
    /// had no book, had never wanted one, and had in fact got exactly what they set out for. A round that
    /// succeeded, filed as a failure, 233 times, with the ledger pricing the trade off it.
    /// </para>
    ///
    /// <para>
    /// And underneath the mislabelling, a real refusal: the round gives up early when the book already holds
    /// the spell, which is right for learning and backwards for stocking. A mage that knew Magic Arrow could
    /// never buy a Magic Arrow scroll to throw.
    /// </para>
    /// </summary>
    private enum Purpose
    {
        /// <summary>Into the book. See BotSeeker.</summary>
        Learn,

        /// <summary>Into the pack, to be thrown. See BotArmoury.</summary>
        Stock
    }

    private readonly Purpose _purpose;

    private BotAcquire(
        Route route, Type kind, int spell, Map map, Point3D where, int price, BaseVendor shop, BotStorefront front,
        Purpose purpose = Purpose.Learn
    )
    {
        _purpose = purpose;
        _route = route;
        _kind = kind;
        _spell = spell;
        _map = map;
        _where = where;
        _price = Math.Max(1, price);
        _shop = shop;
        _front = front;
    }

    /// <summary>Collecting what a standing want has already been filled with.</summary>
    public static BotAcquire Delivery(Type kind, int spell, Map map, Point3D where, bool toCast = false) =>
        new(Route.Delivered, kind, spell, map, where, 1, null, null, Bought(toCast));

    /// <summary>Which of the two errands this is. Named so the four factories all say it the same way.</summary>
    private static Purpose Bought(bool toCast) => toCast ? Purpose.Stock : Purpose.Learn;

    /// <summary>Off a shopkeeper's shelf.</summary>
    public static BotAcquire Counter(Type kind, int spell, BaseVendor shop, int price, bool toCast = false) =>
        new(Route.Counter, kind, spell, shop?.Map, shop?.Location ?? Point3D.Zero, price, shop, null, Bought(toCast));

    /// <summary>Off another bot's stall. No walk: the market holds its goods out of the world.</summary>
    /// <summary>At a bot's shop by the bank, at the keeper's price. See <c>BotShopkeep.Visit</c>.</summary>
    public static BotAcquire AtShop(Type kind, int spell, BotStorefront front, int price, bool toCast = false) =>
        new(Route.Shop, kind, spell, front?.Map, front?.Location ?? Point3D.Zero, price, null, front, Bought(toCast));

    /// <summary>By asking the population, with the money down.</summary>
    /// <summary>
    /// The board route, or nothing at all when the board has no room.
    ///
    /// <para>
    /// <b>Asked here rather than in <see cref="Asking"/>, and the difference is a loop.</b> This route ends
    /// in <c>BotMarket.Ask</c>, which answers null both when the bot cannot fund the want and when the
    /// board is full — and the errand reported only the first, so "could not put the money down for it" was
    /// said 125 times in one half-hour window by bots with money, about a board that had been full the whole
    /// time. A candidate passed over here is free; the errand failing on its first beat is offered again on
    /// the next.
    /// </para>
    /// </summary>
    public static BotAcquire Board(Type kind, int spell, Map map, Point3D where, int offer, bool toCast = false) =>
        BotMarket.Full || !BotMarket.Running ? null : new BotAcquire(Route.Board, kind, spell, map, where, offer, null, null, Bought(toCast));

    public override string Kind => Trade;

    /// <summary>
    /// Getting a spell is seen through. See <see cref="BotDeed.Steadfast"/>.
    ///
    /// <para>
    /// <b>The same dithering as mining, on the spending side.</b> With the hold on for the gathering trades, the
    /// first twenty-two minutes of build 34 still showed this errand dropped at the two-minute mark nine times and
    /// taken up again within ten minutes in 89% of its drops — a bot walking to a shelf for a scroll, lured away
    /// halfway, and walking back. Nothing in here keeps time, so there is nothing to put right on resuming.
    /// </para>
    /// </summary>
    public override bool Steadfast => true;

    public override Map Map => _map;

    public override Point3D Where => _where;

    public override double Expects => Prior;

    public override double Minutes => WorkMinutes;

    /// <summary>Nothing. Putting a scroll into a book teaches a bot nothing at all; casting from it would.</summary>
    public override SkillName? Trains => null;

    /// <summary>What it will cost. The one number the decision layer measures need against.</summary>
    public override int Outlay => _route == Route.Delivered ? 0 : Ask * _price;

    /// <summary>The routes that buy over a counter — a shopkeeper's or a bot's shop's till, both paying out of the pack. See <see cref="BotDeed.AtCounter"/>.</summary>
    public override bool AtCounter => _route is Route.Counter or Route.Shop;

    /// <summary>Not a penny comes back. This is the spending half of a living.</summary>
    public override double Coin => 0.0;

    /// <summary>
    /// Goods are worth what they cost, so the errand comes out at about nothing rather than at a loss. Money
    /// put down on a want counts the same way: it has not been lost, it has become a claim on a scroll.
    /// </summary>
    public override int Made => _paid;

    public override string Stage
    {
        get
        {
            var name = _kind?.Name ?? "a spell";

            if (_learned)
            {
                return $"learned {name}";
            }

            return _route switch
            {
                Route.Delivered => $"collecting {name}",
                Route.Counter => $"buying {name}",
                Route.Shop => $"buying {name} at {_front?.Keeper?.Name ?? "a bot"}'s shop",
                _ => $"asking for {name} at {_price}gp"
            };
        }
    }

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

        if (_kind == null || _spell < 0)
        {
            return BotDoing.Failed("no such spell");
        }

        // Only when the errand was to learn it. A bot stocking scrolls to throw wants them whether or not
        // the spell is also written in a book it may not even carry — see Purpose.
        if (_purpose == Purpose.Learn && BotGrimoire.Holds(body, _spell))
        {
            // Somebody else's delivery, a corpse, or a scroll bought a moment ago: whatever the reason, the
            // book has it now and there is nothing left to do.
            return BotDoing.Done($"already knows {_kind.Name}");
        }

        return _route switch
        {
            Route.Delivered => Collecting(bot, body),
            Route.Counter => Buying(bot, body),
            Route.Shop => Visiting(bot, body),
            _ => Asking(bot, body)
        };
    }

    /// <summary>
    /// Taking delivery. The scroll may already be in the pack — the market hands over on its own beat — so
    /// this asks the market once and then looks where the answer would have put it either way.
    /// </summary>
    private BotDoing Collecting(IBotWilful bot, Mobile body) => Learn(bot, body);

    private BotDoing Buying(IBotWilful bot, Mobile body)
    {
        if (_shop == null || _shop.Deleted || _shop.Map == null || _shop.Map == Map.Internal)
        {
            return BotDoing.Failed("the shopkeeper is gone");
        }

        if (!body.InRange(_shop.Location, BotShops.CounterReach))
        {
            // The distance the work itself asks for, on the line above. See BotArrival.Beside.
            // Followed rather than aimed at: a shopkeeper wanders. See BotPeddle for the whole reason.
            return BotDoing.Walk(_shop.Map, _shop, BotArrival.Within(BotShops.CounterReach), $"to {_shop.Name} for a scroll");
        }

        var bought = BotShops.Buy(bot, _shop, _kind, Ask, out var refused);

        // The shelf emptied by whoever got here first: on to the next shopkeeper with the scroll, at that one's price.
        // See BotShops.Next.
        if (bought <= 0 && BotShops.Next(bot, _shop, _kind, ref _repicks) is { } next)
        {
            _shop = next;
            _price = Math.Max(1, BotShops.Price(next, _kind));

            return BotDoing.Walk(next.Map, next, BotArrival.Within(BotShops.CounterReach), $"on to {next.Name} for a scroll");
        }

        if (bought <= 0)
        {
            // The second counter in the project, and it was left saying the old sentence when the first one
            // was taught to name its reason — so scroll buying went on being the one shop errand nobody could
            // diagnose. Two call sites, one message: whichever gets instrumented, the other has to follow.
            return BotDoing.Failed(refused ?? "the shop would not sell it");
        }

        _paid = bought * _price;

        return Learn(bot, body);
    }

    /// <summary>
    /// A bot's shop by the bank: walk to the keeper and buy at its till (<c>BotShopkeep.Visit</c>). A shop gone or sold out sends the
    /// errand on to a shopkeeper when one sells the scroll, at that counter's price.
    /// </summary>
    private BotDoing Visiting(IBotWilful bot, Mobile body)
    {
        var visit = BotShopkeep.Visit(bot, _front, _kind, Ask, _price, false, out var got, out var cost, out var onward);

        if (onward != null)
        {
            _shop = onward;
            _price = Math.Max(1, BotShops.Price(onward, _kind));

            return Buying(bot, body);
        }

        if (got <= 0)
        {
            return visit;
        }

        _paid = cost;

        return Learn(bot, body);
    }

    /// <summary>
    /// Says the want aloud, or asks for more on the one already there, and finishes.
    ///
    /// <para>
    /// <b>Finishing here rather than waiting is the point.</b> A want is a standing position, not an errand: it is said in the
    /// trade chat, raises its own offer while nobody answers it, and is answered by a scribe who walks the scroll over and is paid
    /// at the meeting (<see cref="BotDeliver"/>) — after which the scroll is in the pack and the seeker's next look learns it. A
    /// bot that stood still until its want was answered would be a bot doing nothing for half an hour.
    /// </para>
    /// </summary>
    private BotDoing Asking(IBotWilful bot, Mobile body)
    {
        // <b>Its own stock first.</b> A scribe with harm scrolls of its own was sent to buy harm scrolls (Lysa Ashdown 53 times in
        // ten minutes on 14.09.2026, off her own stall). Standing at a bank, its own bank box is where they lie now.
        if (BotMarket.FromBank(bot, _kind, Ask) > 0)
        {
            (body as BotMobile)?.Rearm();

            return Learn(bot, body);
        }

        var want = BotMarket.Ask(bot, _kind, Ask, _price);

        if (want == null)
        {
            // Two causes, one answer from Ask, and saying only one of them cost a night of reading the wrong
            // subsystem. The board is checked first because it is the one that is nothing to do with this bot.
            return BotDoing.Failed(
                BotMarket.Full
                    ? $"the market already holds {BotMarket.MaxWants} wants"
                    : BotMarket.Reserved(body) > 0
                        ? $"could not pay for it if it came, with {BotMarket.Reserved(body)}gp of its {BotYield.Wealth(body)}gp held for its other wants"
                        : "could not pay for it if it came"
            );
        }

        // Nothing is paid until a scribe brings it, hand to hand: nothing spent, nothing made.
        return BotDoing.Done($"asked aloud for {_kind.Name} at {want.Offer}gp");
    }

    private BotDoing Learn(IBotWilful bot, Mobile body)
    {
        var scrolls = BotQuill.Gather(body, _kind);

        // Stocking is over the moment the scroll is in the pack: that is where a scroll to be thrown lives.
        if (_purpose == Purpose.Stock)
        {
            if (scrolls.Count == 0)
            {
                return BotDoing.Failed($"paid for {_kind.Name} and it is not in the pack");
            }

            _learned = true;
            BotMarket.Withdrawn(bot, _kind, BotWithdraw.Have);

            return BotDoing.Done($"stocked {_kind.Name} for {_paid}gp");
        }

        string refused = null;

        for (var i = 0; i < scrolls.Count; i++)
        {
            if (!BotGrimoire.Write(body, scrolls[i], out var why))
            {
                refused ??= why;

                continue;
            }

            _learned = true;

            // The spell is in the book, so whatever was still standing on the market asking for it is asking
            // for nothing. The money comes back. Without this a caster that bought a scroll off a stall would
            // leave its own want up, holding its gold until the market gave up on it half an hour later.
            BotMarket.Withdrawn(bot, _kind, BotWithdraw.Have);

            return BotDoing.Done($"learned {_kind.Name} for {_paid}gp");
        }

        // It is in the pack and the book would not take it. Not a failure worth a caution on the place: the
        // scroll is real, it is worth money, and the population has one more of them than it did.
        return BotDoing.Done(
            scrolls.Count == 0
                ? $"paid for {_kind.Name} and it is not in the pack"
                : $"has {_kind.Name} but the book would not take it: {refused ?? "no reason given"}"
        );
    }
}
