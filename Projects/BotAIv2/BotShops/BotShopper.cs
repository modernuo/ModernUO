using System;
using System.Collections.Generic;
using Server.Engines.Craft;
using Server.Items;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// Offers a trip to the shops to any bot that has run out of something its class needs.
///
/// <para>
/// <b>Every bot, not a trade.</b> A caster with no reagents is not a caster, a bot with no bandages cannot
/// mend itself, and both facts are true of whoever happens to be standing there. What each one needs comes
/// from its own class's kit — the same list the world handed it at birth — so this needs no table of who
/// buys what and gains a class the moment one is added.
/// </para>
///
/// <para>
/// Supplies are deliberately not bound: they are meant to run out. That is what makes a shop worth walking
/// to, and later what makes another bot's production worth buying.
/// </para>
/// </summary>
public sealed class BotShopper : IBotProposer
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotShopper));

    /// <summary>
    /// How far below what it was born with a supply has to fall before the bot goes shopping.
    ///
    /// Half. Higher and bots live at the counter; lower and a caster discovers it is dry in the middle of
    /// the one fight where that matters.
    /// </summary>
    public static double Short { get; set; } = 0.5;

    /// <summary>
    /// <b>A bandage is a cloth and a pair of scissors (29.09.2026).</b> The healer sells a bandage at 5gp and the tailor a
    /// cloth at 2gp, and every bot carries scissors; in the two hours after the wipe the population paid 5290gp for 1058
    /// bandages, its largest single outflow to the shopkeepers. A bot short of bandages with scissors in its pack is sent
    /// for cloth instead wherever cloth is the cheaper of the two, and cuts the whole purchase the moment it is paid for
    /// (<c>BotRestock.Cuts</c>, <c>BotWeave.Bandages</c>) so nothing is left over for the unload to sell back. Off
    /// restores the bandage counter.
    /// </summary>
    public static bool CutsBandages { get; set; } = true;

    /// <summary>Bandage errands sent for cloth instead. See <see cref="CutsBandages"/>.</summary>
    public static long AsCloth { get; private set; }

    /// <summary>Bandage errands sent for scissors first, so the next can be cloth. See <see cref="CutsBandages"/>.</summary>
    public static long ScissorsFirst { get; private set; }

    /// <summary>Set for the errand being built, read by <c>Ordered</c>; single-threaded, and reset at every look.</summary>
    private static bool _cutting;

    /// <summary>
    /// The eight the world hands a caster at birth. What it runs out of is what it goes back for.
    ///
    /// All eight rather than the six the starting spells need: bloodmoss and mandrake root are what half
    /// the first three circles are made of, and every scroll worth writing wants one or the other. A list
    /// short of them is a list that lets a caster collect spells it can never cast.
    /// </summary>
    private static readonly Type[] Reagents =
    [
        typeof(SulfurousAsh), typeof(BlackPearl), typeof(Garlic), typeof(Ginseng),
        typeof(SpidersSilk), typeof(Nightshade), typeof(Bloodmoss), typeof(MandrakeRoot)
    ];

    /// <summary>
    /// What to open an order at when nothing of the kind has ever changed hands here.
    ///
    /// <para>
    /// Only ever the last resort: <see cref="BotMarket.Worth"/> answers with what somebody is paying, then
    /// with what one has really sold for, and reaches this only when the shard has no experience of the thing
    /// at all. Deliberately small — the want raises its own offer every beat until somebody bites, so an
    /// opening ask that is too low costs a minute and one that is too high overpays for ever.
    /// </para>
    /// </summary>
    public static int Guess { get; set; } = 5;

    /// <summary>
    /// The stand-in, never under what the cheapest shopkeeper on the island asks for one (03.10.2026, build next): the market's
    /// reckoning no longer lifts an opening to another buyer's raised offer, and five gold for a potion the alchemist sells at
    /// fifteen is not an estimate. On build 396 every lesser cure opened at 20, ×4 of five, read off a raised want.
    /// </summary>
    private static int Stand(Type wanted) => Math.Max(Guess, BotShops.Lowest(wanted));

    /// <summary>What a bot keeps back rather than spend on restocking — it still has to eat and be patched up: <see cref="BotPurse.KeepBack"/> unless dialled apart.</summary>
    public static int Reserve
    {
        get => _reserve ?? BotPurse.KeepBack;
        set => _reserve = value;
    }

    private static int? _reserve;

    /// <summary>Supplies asked of the population because no shelf and no bot's shop had any.</summary>
    public static long Asked { get; private set; }

    private static bool _saidNoShops;

    public string Name => "Shopper";

    public BotStanding Rung => BotStanding.Free;

    // ---- Every gate, counted apart. This proposer had none at all. ------------------------------
    //
    // <b>The one errand that stands between "supplies run out" and "bots die of it", and it was silent.</b>
    // Every other proposer on this shard names its refusals; this one named none, so "nobody needs anything"
    // and "everybody needs something and cannot get it" were the same nought. It mattered on the evening of
    // 27.08.2026: 221 of 252 moments at death's door were a bot reaching for a potion it did not have, and
    // nothing anywhere could say whether that was a bot too poor to buy one, a shard with none for sale, or a
    // bot that had simply never been offered the trip.

    /// <summary>Bots looked at.</summary>
    public static long Looks { get; private set; }

    /// <summary>Bots short of nothing at all. Most answers, and not a refusal.</summary>
    public static long Stocked { get; private set; }

    /// <summary>Trips to a shopkeeper offered.</summary>
    public static long ToCounter { get; private set; }

    /// <summary>
    /// Errands offered to buy a tool again while the last one still worked — offers, not purchases: it read as trips and
    /// climbed 1141 → 5163 in twenty minutes of build 349 against dozens of tools bought (`night-r23-wealth.md`). See
    /// <c>BotOutfit.SpareAt</c>.
    /// </summary>
    public static long Spares { get; private set; }


    /// <summary>Trips to the bot's own guild counter offered — the same price as the shelf, and next door.</summary>
    public static long ToHall { get; private set; }

    /// <summary>
    /// The guild's own counter passed over because this bot lately could not reach it, or the shard has proved there is
    /// no way to it from here. The supply is sought at a shopkeeper instead.
    /// </summary>
    public static long HallWalled { get; private set; }

    /// <summary>Orders put on the needs board because nobody sells the thing at all.</summary>
    public static long ToBoard { get; private set; }

    /// <summary>Wanted something nobody sells and could not afford to have one made either.</summary>
    public static long Broke { get; private set; }

    /// <summary>Wanted something nobody sells, could afford one made, and still got no order onto the board.</summary>
    public static long Unboarded { get; private set; }

    /// <summary>The fattest purse among those. See <c>BotStable.Richest</c> for why this is kept.</summary>
    public static long Richest { get; private set; }

    /// <summary>What the population is short of, by kind, so the summary can name the commonest.</summary>
    private static readonly System.Collections.Generic.Dictionary<Type, long> _short = [];

    /// <summary>
    /// How long a shortage keeps its full weight before the tally starts forgetting it.
    ///
    /// <para>
    /// <b>This tally is not a summary counter and it was being kept as one.</b> Every other number on this
    /// shard is cumulative since the world loaded, and that is right for a number somebody reads once every
    /// five minutes. This one is read by <c>BotSupplier</c> to decide <em>what to buy next</em>, and a
    /// lifetime total answers a different question from the one that decision asks: whatever ran short in
    /// the first ten minutes of a shard sits at the top of the restocking order for the rest of the night,
    /// however well supplied it has been since.
    /// </para>
    ///
    /// <para>
    /// Halved every quarter of an hour rather than windowed, because halving needs no history: a kind that
    /// is still running short keeps its place easily, one that has been fixed drifts down through the
    /// order in about an hour, and nothing has to be stored to know which. Entries that fall to nothing are
    /// dropped, so a kind that stopped being a problem stops being considered at all.
    /// </para>
    /// </summary>
    public static int ForgetMs { get; set; } = 900000;

    private static long _forgot;

    private static bool _everForgot;

    /// <summary>
    /// Halves every shortage if the interval has passed. Called from both sides of the tally, so it happens
    /// whether the population is reporting shortages or the supplier is reading them.
    ///
    /// The "has it ever run" flag rather than a stamp of nought: on some hosts the tick count starts
    /// enormous and can wrap negative, so zero is a real reading and not a way of saying never. Same
    /// reasoning as <c>BotLadder.Hunted</c>.
    /// </summary>
    private static void Fade()
    {
        var now = Core.TickCount;

        if (!_everForgot)
        {
            _everForgot = true;
            _forgot = now;

            return;
        }

        if (now - _forgot < ForgetMs)
        {
            return;
        }

        _forgot = now;

        List<Type> gone = null;

        foreach (var (kind, times) in _short)
        {
            var left = times / 2;

            if (left <= 0)
            {
                (gone ??= []).Add(kind);
            }
            else
            {
                _short[kind] = left;
            }
        }

        if (gone == null)
        {
            return;
        }

        for (var i = 0; i < gone.Count; i++)
        {
            _short.Remove(gone[i]);
        }
    }

    /// <summary>Whether this tool is one of the class's own trade tools (its kit), rather than one everybody carries.</summary>
    private static bool Trade(BotClass klass, Type tool)
    {
        var own = klass.Kit.Tools;

        for (var i = 0; i < own.Count; i++)
        {
            if (own[i] == tool)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Restocks sent at the spare price because the bot was below its entry threshold. See <see cref="BotProvision"/>.</summary>
    public static long Provisioned { get; private set; }

    /// <summary>Units of a supply a bot short of it took out of its own bank box at a bank instead of buying (06.10.2026, build next).</summary>
    public static long FromOwnBox { get; private set; }

    public BotDeed Propose(IBotWilful bot)
    {
        var body = bot?.Self;
        var map = body?.Map;
        var klass = bot?.Class;
        var pack = body?.Backpack;

        if (map == null || map == Map.Internal || klass == null || pack == null)
        {
            return null;
        }

        // Whatever this bot is standing in the middle of, swept once for everybody.
        BotShops.Survey(map, body.Location);

        Looks++;

        // <b>This bot's asks of the island looked over first (03.10.2026, build next).</b> One met or gone is concluded, one past its
        // wait or turned urgent is taken back and walked for; one still waited for is passed over below (Wanting), so the wait for one
        // supply holds back no other. See BotAskFirst.
        BotAskFirst.Review(bot, body, klass, pack);

        if (!Wanting(bot, klass, pack, out var wanted, out var amount, out var band, out var spare))
        {
            // Nothing short but what the island is being asked for is waiting, not "short of nothing".
            if (BotAskFirst.ParkedThisLook)
            {
                BotAskFirst.OnlyWaited();
            }
            else
            {
                Stocked++;
            }

            return null;
        }

        // <b>Its own box before any counter (06.10.2026, build next; Patrick: "all the surplus they must keep in the bank").</b>
        // The porter puts every draught over the allowance in the box, and nothing but a keeper's shift ever took one out again, so a
        // bot with six heal potions in its box walked to a shopkeeper for two. Standing at a bank, it takes them out instead; the
        // next review asks again if that was not enough. See BotMarket.FromBank.
        // Never past what the porter lets the pack keep, or the next trip to the box would put it straight back.
        var own = 0;

        if (!band && body.FindBankNoCreate() is { Items.Count: > 0 }
            && BotUnload.Keeps(bot).TryGetValue(wanted, out var kept) && kept > 0)
        {
            own = BotMarket.FromBank(bot, wanted, Math.Min(amount, kept - pack.GetAmount(wanted)));
        }

        if (own > 0)
        {
            FromOwnBox += own;

            return null;
        }

        // <b>Below the entry threshold, supplies are the errand that wins, and the guild pays what the bot cannot.</b>
        // Patrick's order of 26.09.2026: consumables are compulsory (BotProvision). The trip is claimed at the spare price,
        // and the money is made sure of before the errand is priced, so the appraisal's "costs more than it has" does not
        // veto it.
        var needy = BotProvision.Consumable(wanted) && BotProvision.Short(body);

        if (needy)
        {
            spare = true;
            Provisioned++;
        }

        // What was asked for, before cloth stands in for bandages below: the threshold that makes part of it compulsory is the asked
        // kind's. See ByPurse.
        var askedKind = wanted;

        // Cloth and scissors are bandages at two-fifths of the price. See CutsBandages.
        _cutting = false;

        if (wanted == typeof(Bandage) && CutsBandages && ClothCheaper(bot, amount))
        {
            if (BotOutfit.Oldest<Scissors>(pack) != null)
            {
                wanted = typeof(Cloth);
                _cutting = true;
                AsCloth++;
            }
            else if (BotShops.Nearest(bot, typeof(Scissors)) != null)
            {
                // <b>The scissors first (29.09.2026).</b> Since 288 the bandages come before the tools, so a bot back from
                // its death — no scissors — bought bandages at five while cloth was at two; a pair of scissors at eleven
                // and twenty cloth at forty is half the price of twenty bandages, and the scissors stay.
                wanted = typeof(Scissors);
                amount = 1;
                spare = true;
                ScissorsFirst++;
            }
        }

        // <b>Not a purchase the pack cannot take (06.10.2026, work17).</b> On build 410 32 restocks failed at the counter for the bot's
        // own full pack, in six-a-second bursts the breaker paid in full: "there is no room in the pack for it" (Balgrim 11, Hrolf 6),
        // "the pack would not hold the 5gp it drew to pay with" (Jorunna 9), "the pack would not take Feather" (Adela 5). A restock
        // is offered only when the pack takes the thing and the coin drawn for it; otherwise the bot is marked, so its peddler and
        // porter (offered on this same review) make room first. Counted. See BotRoom.Receives.
        if (!BotRoom.Receives(body, wanted, true))
        {
            BotRoom.Note(body);
            BotRoom.RestockNotOffered();

            return null;
        }

        Fade();

        _short.TryGetValue(wanted, out var seen);
        _short[wanted] = seen + 1;

        var shop = BotShops.Nearest(bot, wanted);
        // At what this buyer pays there, the town's standing on top. See BotBurgh.Dear.
        var counter = shop == null ? 0 : BotBurgh.Dear(body, shop, BotShops.Price(shop, wanted));

        // What the bot is short of, for BotAskFirst: the bandage when cloth is what would be bought to cut. A supply the island is
        // being asked for is passed over in Wanting; scissors-first can still land on one, and it waits rather than buy it twice.
        var need = _cutting ? askedKind : wanted;

        if (BotAskFirst.Parked(body.Serial, need))
        {
            return null;
        }

        // <b>A bot's shop by the bank, first, when it beats the shopkeeper on price and the walk (29.09.2026; the market itself
        // since 01.10.2026).</b> The shop's walk and the counter's priced at what a shopping minute is worth, ties to ours. See
        // BotShopkeep.Beats; with no shop open it answers null at once and nothing below changes.
        var front = BotShopkeep.Beats(bot, wanted, amount, shop, counter, out var frontUnit, out var frontUnits);

        if (front != null)
        {
            // Within what the purse allows (02.10.2026, build next). See ByPurse.
            frontUnits = ByPurse(body, askedKind, wanted, frontUnits, frontUnit, needy, band, pack);

            if (frontUnits <= 0)
            {
                return null;
            }

            if (needy)
            {
                BotProvision.Fund(body, frontUnit * frontUnits);
            }

            return Ordered(new BotRestock(front, wanted, frontUnits, frontUnit), band, spare);
        }

        // <b>No stall to weigh any more (01.10.2026).</b> The auction's stalls were the cheapest supply there was — no walk — and
        // with them gone a bot's supply comes from a bot's shop by the bank (above), its guild's counter, a shopkeeper, or a want
        // said aloud that a carrier answers. Coin paid to one of ours still wins a tie, at the shop above.
        //
        // <b>Then the guild's own shelf, before the walk to Britain.</b> A hall counter is stocked out of the
        // same shopkeepers at the same prices, so per unit it can only tie — and a tie goes to one of ours
        // for two reasons here rather than one: the coin stays in the population, and the errand is a walk
        // across the guild's yard instead of a walk across the island. Three percent of this population's
        // waking minutes were that walk.
        //
        // Priced by the unit to compare with a shelf, though what is actually bought is the whole lot: a
        // shop sells a packet. Rounded up, so a packet that works out fractionally dearer per bandage than
        // the town is not preferred on a rounding error.
        var merchant = BotShelf.Of(body);

        // <b>The same two questions the shopkeepers are asked, and the hall was never asked them.</b> BotShops.Nearest
        // skips a counter this bot lately failed to reach and one the shard has proved there is no way to; the guild's
        // own counter came later and was offered on price alone. On 14.09.2026 Edda Ashdown was sent to The Needle's
        // counter six times in two seconds — "no way from here", "no way through to Zane inside a house" — until the
        // breaker rested the work for five minutes and refused 71 more offers of it. Passed over here, the supply is
        // sought at a shopkeeper as it would be for a guild with no hall.
        if (merchant != null
            && (bot.Resolve?.Ledger?.Cautious(BotShops.ShopKind, merchant.Map, merchant.Location) == true
                || BotReach.Ask(map, body.Location, merchant.Location, BotArrival.Within(BotShelf.Reach)) == BotReachVerdict.Sealed))
        {
            HallWalled++;
            merchant = null;
        }

        var lotPrice = 0;
        var ours = merchant == null ? null : BotShelf.Offer(merchant, wanted, out lotPrice);

        if (ours != null)
        {
            var unit = (int)Math.Ceiling(lotPrice / (double)Math.Max(1, ours.Amount));

            if ((counter <= 0 || unit <= counter) && BotYield.Spendable(body) >= lotPrice)
            {
                ToHall++;

                return Ordered(new BotRestock(merchant, wanted, Math.Max(1, ours.Amount), unit), band, spare);
            }
        }

        // The island's pile before the shopkeeper. See PileMs. Not again right after BotAskFirst asked for the same need and walked.
        if (PileMs > 0 && !band && Patient(wanted) && BotProvision.Consumable(wanted) && BotGlut.Glutted(wanted)
            && pack.GetAmount(wanted) > 0 && !BotAskFirst.Lately(body.Serial, need))
        {
            var key = (body.Serial, wanted);
            var now = Core.TickCount;
            var offer = counter > 0 ? counter : BotMarket.Worth(wanted, Stand(wanted));

            if (_askedPile.TryGetValue(key, out var at) && now - at < PileMs * 3L)
            {
                if (now - at < PileMs && BotMarket.Wanted(bot, wanted) != null)
                {
                    PileWaited++;

                    return null;
                }

                // <b>Only the pile's own ask is taken back by the pile's clock (06.10.2026, needs15).</b> Rhiannel, 04.10 12:57:31–42:
                // her pile ask of 12:48:42 was past its three minutes and inside its nine, and every review took back the want the
                // board's order (Board) had said a moment before — 182 one-potion lines said and withdrawn in eleven seconds, each
                // "kept out after 0.0 min", until the nine minutes ran out. A want said after this clock's ask is another path's: it
                // is adopted and waited for from when it was said, as a want this clock does not know is below, not taken back.
                if (now - at >= PileMs && BotMarket.Wanted(bot, wanted) is { } standing && !OwnAsk(standing, at))
                {
                    _askedPile[key] = standing.PostedTick;
                    ForeignKept++;

                    return null;
                }

                // Waited, and nobody came: the want stands down and the bot walks; not asked again for a while.
                if (now - at >= PileMs && BotMarket.Withdrawn(bot, wanted, BotWithdraw.Walked) > 0)
                {
                    PileWalked++;
                }
            }
            else if (BotMarket.Wanted(bot, wanted) != null)
            {
                // A want for it stands and this clock does not know it (build 391 review): read back from the save, or said by
                // another path. Waited for from now, as the far ask does; falling through, the bot bought at a counter while the
                // want still stood, and a carrier then brought the same thing again.
                if (_askedPile.Count > 4096)
                {
                    _askedPile.Clear();
                }

                _askedPile[key] = now;
                PileWaited++;

                return null;
            }
            else if (BotYield.Spendable(body) - offer * amount > 0)
            {
                var order = BotOrder.For(map, body.Location, bot, wanted, offer, amount);

                if (order != null)
                {
                    if (_askedPile.Count > 4096)
                    {
                        _askedPile.Clear();
                    }

                    _askedPile[key] = now;
                    Piled++;

                    return order;
                }
            }
        }

        if (counter <= 0)
        {
            // <b>No counter in reach: a bot near that holds it is the only shop there is (03.10.2026, build next).</b> Asked at the
            // island's lowest counter price, as below; after the wait, the board as before. See BotAskFirst.
            if (BotAskFirst.Asks(bot, body, klass, pack, need, wanted, amount, BotAskFirst.Price(wanted), band, _cutting, needy, Owned(body.Serial, wanted, need), true))
            {
                return null;
            }

            Missing(wanted, map, body);

            var order = Board(bot, body, map, wanted, amount);

            if (order != null)
            {
                ToBoard++;
            }
            else
            {
                // <b>Counted by what actually happened, not by the branch it fell into.</b> Board returns
                // null for several reasons — nobody can make the thing, an order is already out for it, the
                // bot cannot afford the deposit — and every one of them used to be tallied under a figure
                // the summary calls "could not afford one made". A counter that names one cause and catches
                // all of them is this shard's most-repeated defect, and on 02.09.2026 it printed
                // "0 could not afford one made (the fattest purse among them held 0gp)" — a nought beside an
                // unset maximum, read as a destitute population.
                var wealth = BotYield.Spendable(body);

                // The same sum Board itself refuses on, read from the same place — see Board below.
                if (wealth - BotMarket.Worth(wanted, Stand(wanted)) * amount <= Reserve)
                {
                    Broke++;

                    if (wealth > Richest)
                    {
                        Richest = wealth;
                    }
                }
                else
                {
                    Unboarded++;
                }
            }

            return order;
        }

        // <b>A far counter for a thing the island makes is a want said aloud, not a walk (01.10.2026).</b> Measured over 1,076
        // online bot-hours on 30.09.2026 (`night-r29-stalling.md`): restocking took 93 bot-hours a day, 53 of them in 441 walks of
        // four minutes and more that bought about six gold of goods each — a pair of scissors at eleven after a death, twenty
        // cloth, a draught. Patrick's order of 30.09 makes every purchase hand to hand, and hand to hand runs both ways: when the
        // counter is AskFarMinutes or more away and the thing is one this island makes, the bot says what it wants at the
        // counter's price and goes on with its work, and a carrier who holds one walks it over (BotCourier). Not for a bot below
        // its entry threshold (it walks, as the provisioning order says), not for the band's order, not for a weapon or what it
        // shoots. If nobody has brought it within AskFarMs the want stands down and the bot walks after all.
        if (AskFarMinutes > 0.0 && !needy && !band && Patient(wanted) && Makeable(wanted)
            && BotAppraisal.Travel(body.Location, shop.Location) >= AskFarMinutes)
        {
            var key = (body.Serial, wanted);
            var now = Core.TickCount;
            var asked = _askedFar.TryGetValue(key, out var at);

            // <b>Somebody is bringing it: the bot waits for it, however long the road (01.10.2026, build 385).</b> A trader's haul
            // from a counter across the island takes longer than AskFarMs, and the want withdrawn under it at ten minutes made
            // the bot walk for what was already on its way and the haul arrive at a want that was gone. The carrier's own clocks
            // give the delivery up (BotDeliver.GiveUpMs, BotTrading.MostHaulMs); the market's sweep then frees the want.
            if (asked && BotMarket.Wanted(bot, wanted) is { Coming: > 0 })
            {
                WaitedFar++;

                return null;
            }

            // <b>A want for it already stands, and this bot's clock does not know it (02.10.2026, build 391):</b> read back from the
            // save after a restart (BotMarketStore). Waited for from now, as if just asked, rather than asked for a second time —
            // BotMarket.Ask would top the standing want up to twice what the bot needs.
            if (!asked && BotMarket.Wanted(bot, wanted) != null)
            {
                if (_askedFar.Count > 4096)
                {
                    _askedFar.Clear();
                }

                _askedFar[key] = now;
                WaitedFar++;

                return null;
            }

            if (!asked || now - at >= AskFarMs * 3L)
            {
                var order = BotOrder.For(map, body.Location, bot, wanted, counter, amount);

                if (order != null && BotYield.Spendable(body) - counter * amount > Reserve)
                {
                    if (_askedFar.Count > 4096)
                    {
                        _askedFar.Clear();
                    }

                    _askedFar[key] = now;
                    AskedFar++;

                    return order;
                }
            }
            else if (now - at < AskFarMs && BotMarket.Wanted(bot, wanted) != null)
            {
                WaitedFar++;

                return null;
            }
            else if (BotMarket.Wanted(bot, wanted) is { } standing && !OwnAsk(standing, at))
            {
                // Said after this clock's ask, by another path: adopted and waited for from when it was said, not taken back at once
                // (06.10.2026, needs15). See OwnAsk.
                _askedFar[key] = standing.PostedTick;
                ForeignKept++;

                return null;
            }
            else if (BotMarket.Withdrawn(bot, wanted, BotWithdraw.Walked) > 0)
            {
                // Nobody brought it in time: the want stands down so the thing is not bought twice, and the bot walks.
                WalkedFar++;
            }
        }

        // <b>Ask the island before the walk when a bot near holds it (03.10.2026, build next; flow9's proposal 1).</b> Cloth and
        // bandages, reagents, arrows and bolts and tools were 64 % of the gold bots paid shopkeepers and 9 % of the board's, while
        // pickers, fletchers and tinkers kept theirs to sell. A want at the counter's price, for no more than one holder within a
        // carrier's reach holds over its keep, three minutes for a carrier, then the walk below. Never for an urgent need or a bot
        // below its entry threshold (needy), nor while it already walks a restock. See BotAskFirst.
        if (BotAskFirst.Asks(bot, body, klass, pack, need, wanted, amount, counter, band, _cutting, needy, Owned(body.Serial, wanted, need), false))
        {
            return null;
        }

        // Within what the purse allows (02.10.2026, build next). See ByPurse.
        amount = ByPurse(body, askedKind, wanted, amount, counter, needy, band, pack);

        if (amount <= 0)
        {
            return null;
        }

        ToCounter++;

        if (needy)
        {
            BotProvision.Fund(body, counter * amount);
        }

        return Ordered(new BotRestock(shop, wanted, amount, counter), band, spare);
    }

    // ---- Supplies within what the purse allows (02.10.2026, build next; Patrick's option 4: "restock by their purse") -------------

    /// <summary>Whether a consumable is bought within what the purse allows: the compulsory part always, the rest only above the keep.</summary>
    public static bool PurseSized { get; set; } = true;

    /// <summary>Restocks bought smaller than asked for the purse, and the units left unbought; restocks not offered at all for it.</summary>
    public static long Trimmed { get; private set; }

    public static long TrimmedUnits { get; private set; }

    public static long KeptBack { get; private set; }

    /// <summary>
    /// How many of a consumable to buy, given the purse. <b>The part up to the threshold a door asks</b> (<see cref="BotProvision.Floor"/>:
    /// a quarter of the kit's bandages, a bottle of each, half the quiver, five casts' reagents) is bought whatever the purse —
    /// the guild pays what the bot cannot when it is below the threshold (<see cref="BotProvision.Fund"/>), and otherwise the purse
    /// buys what it can of it. <b>The rest, up to the full kit, only out of what the bot has above its keep</b>
    /// (<see cref="Reserve"/>, <see cref="BotPurse.KeepBack"/>). Tools, weapons and the band's orders are not sized here.
    ///
    /// <para>
    /// <b>Measured before it (build 392):</b> 806 of the 853 refusal mentions were a restock the purse could not pay ("restock costs
    /// 60gp and it has 24gp free"), the whole kit or nothing; the 130 bots refused on 390–392 asked a median 60gp holding 14.5gp;
    /// and a death left 63–74gp of supplies on the corpse, so a poor fighter's kit bought in full mostly died with it. Patrick:
    /// "supplies are bought within what the purse allows: buy fewer when poor, never go under the keep."
    /// </para>
    /// </summary>
    private static int ByPurse(Mobile body, Type asked, Type buying, int amount, int unit, bool needy, bool band, Container pack)
    {
        if (!PurseSized || band || amount <= 0 || unit <= 0 || body == null || !BotProvision.Consumable(asked)
            || buying != asked && buying != typeof(Cloth))
        {
            return amount;
        }

        // Cloth bought for bandages is one cloth a bandage, so the bandage's threshold counts in cloth.
        var held = pack?.GetAmount(asked) ?? 0;
        var must = Math.Clamp(BotProvision.Floor(body, asked) - held, 0, amount);
        var free = Math.Max(0, BotYield.Spendable(body));

        // The purse buys what it can of the compulsory part; below the threshold the guild pays what it cannot (as it did for
        // the whole kit before), and when the guild cannot either, the purse's share is what is bought.
        //
        // <b>The guild pays the doors' part only (08.10.2026, cast30 reviews A1/B F2).</b> The floor now carries a healer's or a
        // warrior-mage's attack herbs (BotProvision.Armed), and those are the bot's own to buy: sized here by DoorFloor, which reads
        // the key spells alone, so a needy healer's ash is never bought with the chest's or its guildmates' gold.
        var door = Math.Clamp(BotProvision.DoorFloor(body, asked) - held, 0, must);
        var mustUnits = Math.Min(must, free / unit);

        if (mustUnits < door && needy && BotProvision.Fund(body, door * unit))
        {
            free = Math.Max(0, BotYield.Spendable(body));
            mustUnits = Math.Min(must, Math.Max(door, free / unit));
        }

        var left = Math.Max(0, free - mustUnits * unit);
        var extraUnits = Math.Min(amount - must, Math.Max(0, left - Reserve) / unit);
        var units = mustUnits + extraUnits;

        if (units >= amount)
        {
            return amount;
        }

        if (units <= 0)
        {
            KeptBack++;

            return 0;
        }

        Trimmed++;
        TrimmedUnits += amount - units;

        return units;
    }

    /// <summary>
    /// How many minutes' walk from the counter makes a supply a want said aloud instead of a walk; nought never. Four: the
    /// 30.09.2026 measurement's line between the restocks that cost little and the 441 that cost 53 bot-hours for six gold apiece.
    /// </summary>
    public static double AskFarMinutes { get; set; } = 4.0;

    /// <summary>How long a bot waits for a carrier to bring a supply it asked for instead of walking, before it walks.</summary>
    public static int AskFarMs { get; set; } = 600000;

    /// <summary>Supplies asked for aloud rather than walked for, waits while a carrier was expected, and walks after nobody came.</summary>
    public static long AskedFar { get; private set; }

    public static long WaitedFar { get; private set; }

    public static long WalkedFar { get; private set; }

    private static readonly Dictionary<(Serial, Type), long> _askedFar = [];

    /// <summary>
    /// <b>The island's pile before the shopkeeper (02.10.2026, build 389; Patrick's option 4).</b> On build 386, 226 to 424 bandages
    /// lay glutted at a tenth of their price while the population bought 8,246gp of cloth at the counters to cut its own: the goods
    /// the market cannot see. A consumable the glut says is piled (BotGlut.Glutted) is asked for aloud at the counter's price before
    /// the walk to a counter, and a carrier who holds some walks it over (BotCourier); only a bot holding at least one of the thing
    /// waits, for this long, and then walks after all. Nought turns it off. Not for what a bow shoots (Patient), which a fight wants now.
    /// </summary>
    public static int PileMs { get; set; } = 180000;

    /// <summary>Piled consumables asked for aloud instead of walked for, waits while a carrier was expected, and walks after nobody came.</summary>
    public static long Piled { get; private set; }

    public static long PileWaited { get; private set; }

    public static long PileWalked { get; private set; }

    /// <summary>
    /// Looks at which the pile's or the far ask's clock found a want standing that another path said after its own ask, and adopted
    /// it rather than taking it back (06.10.2026, needs15). See <see cref="OwnAsk"/>.
    /// </summary>
    public static long ForeignKept { get; private set; }

    /// <summary>
    /// Whether a standing want is the one this clock's ask said, or one it adopted: said no later than a second after the ask. A
    /// want said later is another path's (the board's order, the other clock) and is not this clock's to take back.
    /// </summary>
    private static bool OwnAsk(BotWant want, long at) => want.PostedTick - at <= 1000;

    private static readonly Dictionary<(Serial, Type), long> _askedPile = [];

    /// <summary>
    /// Whether the pile's or the far ask's clock holds this need now (inside three of its waits, the span each keeps it for), so
    /// <see cref="BotAskFirst"/> does not ask the island for it a second way: after either has walked, the bot walks.
    /// </summary>
    private static bool Owned(Serial who, Type wanted, Type need)
    {
        var now = Core.TickCount;

        return Clocked(_askedPile, (who, wanted), now, PileMs * 3L) || Clocked(_askedFar, (who, wanted), now, AskFarMs * 3L)
               || need != wanted && (Clocked(_askedPile, (who, need), now, PileMs * 3L) || Clocked(_askedFar, (who, need), now, AskFarMs * 3L));
    }

    private static bool Clocked(Dictionary<(Serial, Type), long> clock, (Serial, Type) key, long now, long span) =>
        clock.TryGetValue(key, out var at) && now - at < span;

    /// <summary>Whether a supply may wait for a carrier: not a weapon, a shield or what a bow shoots, which a fight wants now.</summary>
    private static bool Patient(Type wanted) =>
        wanted != null && !typeof(BaseWeapon).IsAssignableFrom(wanted) && !typeof(BaseShield).IsAssignableFrom(wanted)
        && wanted != typeof(Arrow) && wanted != typeof(Bolt);

    /// <summary>
    /// Claims the errand as the band's when the want came from the band's order rather than from the bot's own kit.
    /// See <c>BotRestock.Claim</c> for why an errand to the shops otherwise cannot win one.
    /// </summary>
    /// <summary>Whether a shopkeeper within reach sells cloth under the price of a bandage, or bandages are not sold at all.</summary>
    /// <summary>
    /// Whether cloth to cut is the cheaper way to <paramref name="amount"/> bandages, the two walks priced in.
    ///
    /// <para>
    /// <b>Two gold against five sent Trinsic to Britain for cloth, through the jungle (01.10.2026, build 386).</b> The prices alone
    /// were compared, so a fighter in Trinsic with a healer selling bandages at its door walked eleven hundred tiles to Phyllis
    /// in Britain for cloth at two: 336 restock errands "after N Cloth" on build 386, 76 of them died (24 %, 11 % of every
    /// death on the shard), 1,767 bot-minutes. Each counter's price is landed now — the thing plus the walk there and back
    /// at the shopping minute's price (<see cref="BotShopkeep.WalkGold"/>) spread over the bandages bought — which is
    /// Patrick's rule that distance is a price. Cloth that is nearer stays cheaper; cloth across the island is not.
    /// </para>
    /// </summary>
    private static bool ClothCheaper(IBotWilful bot, int amount)
    {
        var body = bot?.Self;
        var cloth = BotShops.Nearest(bot, typeof(Cloth));

        if (body == null || cloth == null)
        {
            return false;
        }

        var clothPrice = BotShops.Price(cloth, typeof(Cloth));

        if (clothPrice <= 0)
        {
            return false;
        }

        var bandages = BotShops.Nearest(bot, typeof(Bandage));
        var bandagePrice = bandages == null ? 0 : BotShops.Price(bandages, typeof(Bandage));

        if (bandagePrice <= 0)
        {
            return true;
        }

        var n = Math.Max(1, amount);
        var walk = BotShopkeep.WalkGold;
        var clothLanded = clothPrice + 2.0 * BotAppraisal.Travel(body.Location, cloth.Location) * walk / n;
        var bandageLanded = bandagePrice + 2.0 * BotAppraisal.Travel(body.Location, bandages.Location) * walk / n;

        if (clothLanded < bandageLanded)
        {
            return true;
        }

        if (clothPrice < bandagePrice)
        {
            ClothFarther++;
        }

        return false;
    }

    /// <summary>Bandage errands kept to bandages because the cloth counter, though cheaper by the piece, was the dearer by the road. See <see cref="ClothCheaper"/>.</summary>
    public static long ClothFarther { get; private set; }

    private static BotDeed Ordered(BotRestock errand, bool band, bool spare)
    {
        if (errand == null)
        {
            return null;
        }

        errand.Cuts = _cutting;
        _cutting = false;

        if (band)
        {
            errand.Claim = BotFence.Prior;
            BotFence.Sending();

            return errand;
        }

        // The next tool, bought while the last one still works, is claimed at what keeps the trade going rather than
        // at what shopping creates. See BotRestock.Spare.
        if (spare)
        {
            errand.Claim = BotRestock.Spare;
            Spares++;
        }

        return errand;
    }

    /// <summary>
    /// What the population has been short of, commonest first.
    ///
    /// <para>
    /// <b>The tally was already being kept and only ever printed.</b> Every look for supplies writes down
    /// what the bot turned out to be lacking, so by the time a guild wonders what to put on its shelf the
    /// answer has been measured a few thousand times — by the population itself, in the only currency that
    /// matters, which is what it kept reaching for and not finding. A list written by hand here would be a
    /// second copy of that, and it would drift the day a class was added. See <see cref="BotSupplier"/>.
    /// </para>
    /// </summary>
    public static List<(Type Kind, long Times)> Shortages()
    {
        Fade();

        List<(Type Kind, long Times)> found = [];

        foreach (var (kind, times) in _short)
        {
            found.Add((kind, times));
        }

        found.Sort((a, b) => b.Times.CompareTo(a.Times));

        return found;
    }

    /// <summary>What the population is most often short of, and how often.</summary>
    private static string Commonest()
    {
        Type worst = null;
        long most = 0;

        foreach (var (kind, times) in _short)
        {
            if (times > most)
            {
                most = times;
                worst = kind;
            }
        }

        return worst == null ? "nothing" : $"{worst.Name} ({most} times)";
    }

    public static string Describe() =>
        Looks == 0
            ? "nobody has been looked at for supplies"
            : $"{Looks} looks for supplies: {Stocked} were short of nothing, {FromOwnBox} things taken out of the bot's own bank box at a bank instead of bought, {ToCounter} sent to a shopkeeper, "
              + $"{AskedFar} asked the island aloud instead of walking to a counter {AskFarMinutes:F0} minutes or more away ({WaitedFar} looks waited for the carrier, {WalkedFar} walked after all), {Piled} asked the island for a piled consumable before any counter ({PileWaited} looks waited for the carrier, {PileWalked} walked after nobody came; {ForeignKept} times either clock found a want another path had said since its ask and waited for it rather than taking it back), {BotShopkeep.ToFront} to a bot's shop by the bank, {ToHall} to their own guild's counter ({HallWalled} times it was passed over as out of reach), {ToBoard} put an order on the board, {Unmakeable} were left off it because nothing on this shard makes the thing, "
              + $"{Spares} offers were for a tool bought again before the last gave out, {BotRestock.ToolsBound} working tools bound as they were bought, {AsCloth} bandage trips sent for cloth to cut ({BotRestock.CutInto} bandages cut at the counter), {ClothFarther} kept to bandages for the cloth counter being the dearer by the road, and {ScissorsFirst} for scissors first, "
              + $"{Broke} wanted something nobody sells and could not afford one made (the fattest purse among them held {Richest}gp); "
              + $"by the purse{(PurseSized ? "" : " (off)")}: {Trimmed} restocks bought smaller than asked ({TrimmedUnits} units left unbought: the threshold whatever the purse, the rest only above the {Reserve}gp keep), {KeptBack} not offered for the keep; "
              + $"most often short of {Commonest()} lately";

    public static void ForgetCounts()
    {
        Trimmed = 0;
        TrimmedUnits = 0;
        KeptBack = 0;
        Looks = 0;
        Stocked = 0;
        FromOwnBox = 0;
        ToCounter = 0;
        AskedFar = 0;
        WaitedFar = 0;
        ClothFarther = 0;
        WalkedFar = 0;
        _askedFar.Clear();
        Piled = 0;
        PileWaited = 0;
        PileWalked = 0;
        ForeignKept = 0;
        _askedPile.Clear();
        Spares = 0;
        ToHall = 0;
        ToBoard = 0;
        Unmakeable = 0;
        Broke = 0;
        Richest = 0;
        _short.Clear();
        _everForgot = false;
    }

    /// <summary>
    /// The first thing this bot is short of, and how many it wants.
    ///
    /// <para>
    /// <b>A worn-through tool comes before everything.</b> Only the weapon is bound; a tool is a thing that
    /// wears out — the engine gives a fresh one twenty-five to seventy-five uses, spends one an attempt and
    /// destroys it at zero — so a crafter that has been sewing for a few minutes has no sewing kit, and a bot
    /// with no tool has no trade at all. It cannot even earn the price of bandages. And the failure is silent
    /// by nature: a proposer simply stops offering the work, which looks exactly like a bot that never had a
    /// kit. This errand is the whole of what stands between "tools wear out" and "trades quietly end".
    /// </para>
    ///
    /// <para>
    /// Then bandages before reagents: one of them is what keeps a bot standing and the other is what makes it
    /// useful.
    /// </para>
    /// </summary>
    private static bool Wanting(
        IBotWilful bot, BotClass klass, Container pack, out Type wanted, out int amount, out bool band, out bool spare
    )
    {
        band = false;
        spare = false;
        var kit = klass.Kit;
        var tools = BotOutfit.ToolsFor(klass);
        var body = bot.Self;
        var bond = bot.Bond;

        // The weapon before the tools, because a bot with no weapon cannot even defend itself, and because
        // this is the one thing on the list that a crafter can make. A blade wears down every time it lands
        // and the engine destroys it at zero — the same mechanic as the pickaxe, on the thing the shard's
        // whole economy of gold runs through.
        var rolled = bond?.Weapon;

        if (rolled?.Weapon != null && pack.GetAmount(rolled.Value.Weapon) <= 0 && !Held(body, rolled.Value.Weapon))
        {
            wanted = rolled.Value.Weapon;
            amount = 1;

            return true;
        }

        // Then what it shoots. Ammunition is bound by count, so death gives back what it was born with and
        // never more — but shooting spends it, and an archer out of arrows is an archer with a knife.
        if (rolled?.Ammunition != null && bond != null)
        {
            var quiver = rolled.Value.Ammunition;
            var granted = BotBinding.BoundCount(quiver, bond);

            // A supply the island is being asked for is passed over for the next (BotAskFirst.Parked, 03.10.2026), here and below.
            if (granted > 0 && Lacking(pack, quiver, granted, out amount) && !BotAskFirst.Parked(body.Serial, quiver))
            {
                wanted = quiver;

                return true;
            }
        }

        // <b>Bandages before tools (29.09.2026).</b> They stood fourth, behind the weapon, the ammunition and every tool on
        // the list — and the list holds three universal tools that every death empties out of the pack along with the
        // class's own. A bot back from its death bought scissors, a skinning knife and a skillet on three trips, each of
        // which could fail for want of a shopkeeper in reach, and went to work with no bandages in between: at 05:50 on
        // 29.09.2026 twelve of thirteen bots asked through the door carried none, while every dungeon, road and lone
        // venture was closed to them for it (BotProvision), and the mend they took under attack was the last thing they
        // did. A bandage is the thing that keeps a bot alive long enough to buy a skillet.
        if (Lacking(pack, typeof(Bandage), kit.Bandages, out amount) && !BotAskFirst.Parked(body.Serial, typeof(Bandage)))
        {
            wanted = typeof(Bandage);

            return true;
        }

        // <b>And the bottles before the tools as well (29.09.2026).</b> At 08:20 on build 293 seven of ten bots asked through the
        // door carried bandages and no potion of either kind, and the supplies line turned 4803 asks away for bottles; the
        // bottles stood behind the tools for the same reason the bandages did. A draught is what works while something is
        // hitting the bot.
        // Then the bottles, and they are replaced one for one rather than at half: there are only ever one or
        // two, and a bot with none has nothing at all that works while something is hitting it. The engine sells
        // only the lesser tier anywhere in this era — the better ones are an alchemist's, and there is no
        // alchemist yet.
        var bottles = BotOutfit.PotionsFor(klass);

        for (var i = 0; i < bottles.Count; i++)
        {
            var (kind, count) = bottles[i];
            var held = pack.GetAmount(kind);

            if (held >= count)
            {
                continue;
            }

            wanted = kind;
            amount = count - held;

            return true;
        }

        for (var i = 0; i < tools.Count; i++)
        {
            // Held or worn: a pickaxe in a hand is still a pickaxe. The pack and the layers are asked separately
            // rather than trusting one of the two.
            //
            // <b>And kept means one with some life left in it.</b> A tool all but spent is bought again now, while it
            // still works — see BotOutfit.SpareAt — or its end comes inside a job, and the job fails for it.
            if (Kept(body, pack, tools[i], out var spent) || BotAskFirst.Parked(body.Serial, tools[i]))
            {
                continue;
            }

            wanted = tools[i];
            amount = 1;

            // <b>A trade's own tool gone altogether is as urgent as one about to go.</b> The spare price (BotRestock.Spare) was
            // paid only while a worn tool was still in the hand; once it broke, the trip fell back to the shopping price and
            // lost to every trade the bot still had. Faron, the one tailor on the shard who could sew leather armour, wore
            // out his sewing kit at 01:48:58 on 26.09.2026 and did not buy a 3gp kit for seven and a half hours, while the
            // board's orders for leather armour climbed to 1505gp and froze thirty thousand gold in escrow; the first kit he
            // bought, at 09:22:27, filled 18 of them for 19,736gp inside ten minutes. The universal tools — scissors, a
            // skinning knife, a skillet — keep the shopping price.
            // <b>Scissors are a tailor's trade tool (29.09.2026).</b> They are universal on the list, so they were never
            // "trade" for anybody and a restock of them scored 8 a minute — a crafter back from its death went without
            // for the rest of the day, and the weaver (BotWeave) refused it 1240 times in two hours for having none,
            // while the population bought 191 pairs for everybody else. Whoever carries a sewing kit is short of a
            // trade tool without them.
            spare = spent || Trade(klass, tools[i]) || tools[i] == typeof(Scissors) && Trade(klass, typeof(SewingKit));

            return true;
        }


        // Anybody whose build includes magic at all, not only those called mages: a healer's cures are spells
        // too. The class's own kit is the test, so this follows the build without being told about it.
        //
        // <b>And anybody holding a mortar, whose build says nothing about magic at all.</b> A brewer needs the
        // same eight herbs and its kit declares none of them, so its reagents were nobody's errand: the
        // gatherer picked them, the market held them, and the one bot that could turn them into something
        // read "had the glass but no herbs" 57 times in five minutes while 1936 of them sat on stalls it was
        // never sent to. The tool decides, which is the rule the cook and the smith are already offered work
        // by — a bot that picks up a pestle tomorrow is a brewer tomorrow.
        //
        // <b>The floor's herbs first, and the rest only with money above the keep (08.10.2026, cast30 review B F1).</b> The eight were
        // asked in a fixed order — ash, pearl, garlic, ginseng — whatever each one's floor, and a bot under its keep is sold none of
        // a herb with no floor (ByPurse answers nought, KeptBack), so the next look picked the same herb again: a poor bot stuck at
        // its first floorless shortage for good. In the save of 01:35 on 08.10.2026 37 of 45 Mages carried none of their floor's
        // herbs, 10 of 14 poor Lightning Mages no mandrake (last in the order) at all, and Mages had bought 970 ash to 206 mandrake.
        // So a herb under its floor is asked for first, in the same order; the rest of the kit only when the purse is above the keep,
        // and a poor bot with its floors met goes on down the list instead of standing at a herb it will never be sold.
        if (kit.Reagents > 0)
        {
            for (var i = 0; i < Reagents.Length; i++)
            {
                if (BotProvision.Floor(body, Reagents[i]) <= pack.GetAmount(Reagents[i])
                    || !Lacking(pack, Reagents[i], kit.Reagents, out amount) || BotAskFirst.Parked(body.Serial, Reagents[i]))
                {
                    continue;
                }

                wanted = Reagents[i];

                return true;
            }

            // Past the keep by at least one of that reagent, or the purse would buy none and the look would come back to it
            // every time (07.10.2026, second pass of cast30 review B: a bot at 101–104gp stuck on pearl at 5gp).
            for (var i = 0; BotYield.Spendable(body) > Reserve && i < Reagents.Length; i++)
            {
                if (BotYield.Spendable(body) <= Reserve + Stand(Reagents[i])
                    || !Lacking(pack, Reagents[i], kit.Reagents, out amount) || BotAskFirst.Parked(body.Serial, Reagents[i]))
                {
                    continue;
                }

                wanted = Reagents[i];

                return true;
            }
        }

        // <b>The band's order, which is the keeper's other half.</b> Patrick's second order of 17.09.2026: the fence
        // "looks after the band's goods and orders". The goods leave the chest through BotFetch; the orders come back
        // through here, because the fence is the one member of The Shadow that a ward will let into a town. Asked
        // after everything the fence needs for itself, on purpose: a keeper out of its own bandages is no use to
        // anybody. What it buys goes into the chest on its next walk out — see BotStash.
        if (BotFence.Is(body) && BotFence.Shortest(body, out var ordered, out var lot))
        {
            wanted = ordered;
            amount = lot;
            band = true;

            return true;
        }

        // <b>And the brewer's two, which are not the caster's eight.</b> Asking for the whole list was the
        // first cut of this and it was worse than useless: the list is in casting order — ash and pearl
        // first — the shopper buys one kind an errand at about two errands a bot in twenty minutes, and the
        // only draughts on this shard are heal and cure. So a brewer spent its first four trips on reagents
        // it can never use while the summary read "had the glass but no herbs" on a rising share, 35% of new
        // asks in one window and 58% in the next. BotFlask.Needs reads the recipe table, so a third draught
        // family adds its reagent there and nowhere else.
        if (BotFlask.Kit(body) == null)
        {
            wanted = null;

            return false;
        }

        var brewing = BotFlask.Needs;

        for (var i = 0; i < brewing.Count; i++)
        {
            if (!Lacking(pack, brewing[i], BotFlask.Herbs, out amount) || BotAskFirst.Parked(body.Serial, brewing[i]))
            {
                continue;
            }

            wanted = brewing[i];

            return true;
        }

        wanted = null;
        amount = 0;

        return false;
    }

    /// <summary>
    /// Whether this bot has one of these with more than <c>BotOutfit.SpareAt</c> uses left, in the pack or in a hand;
    /// <paramref name="spent"/> is true when it has some, but every one of them is near its end.
    /// </summary>
    private static bool Kept(Mobile body, Container pack, Type kind, out bool spent)
    {
        spent = false;

        foreach (var item in pack.FindItemsByType(kind))
        {
            if (!BotOutfit.Spent(item))
            {
                return true;
            }

            spent = true;
        }

        var worn = body?.Items;

        if (worn == null)
        {
            return false;
        }

        for (var i = 0; i < worn.Count; i++)
        {
            if (!kind.IsInstanceOfType(worn[i]))
            {
                continue;
            }

            if (!BotOutfit.Spent(worn[i]))
            {
                return true;
            }

            spent = true;
        }

        return false;
    }

    /// <summary>Whether this bot has one of these in a hand or on its body rather than in the pack.</summary>
    private static bool Held(Mobile bot, Type kind)
    {
        if (bot == null)
        {
            return false;
        }

        var worn = bot.Items;

        for (var i = 0; i < worn.Count; i++)
        {
            if (kind.IsInstanceOfType(worn[i]))
            {
                return true;
            }
        }

        return false;
    }

    private static bool Lacking(Container pack, Type kind, int born, out int wanted)
    {
        wanted = 0;

        if (born <= 0)
        {
            return false;
        }

        var held = pack.GetAmount(kind);

        if (held >= born * Short)
        {
            return false;
        }

        wanted = born - held;

        return wanted > 0;
    }

    /// <summary>
    /// Nothing on any shelf and nothing on any stall: ask the population for it.
    ///
    /// <para>
    /// <b>The end of this method used to be a shrug.</b> It wrote one error line and returned nothing, so a
    /// caster on a shard whose shopkeepers do not stock sulphurous ash simply stopped casting, for ever, and
    /// the only trace was a single line at boot. <see cref="BotSeeker"/> has ended the other way since it was
    /// written — no shop, no stall, so put it on the board — and there was never a reason for supplies to be
    /// the exception. A reagent nobody sells is a reagent somebody can pick: the foragers put them out at a
    /// few gold apiece, and a funded order is what reaches them.
    /// </para>
    ///
    /// <para>
    /// One order per bot per kind, as everywhere else: the want tops itself up and raises its own offer, and
    /// asking again would turn one order into nine.
    /// </para>
    /// </summary>
    /// <summary>
    /// Whether any craft system on this shard can make the thing at all.
    ///
    /// <para>
    /// <b>The caller's own comment has always said Board refuses when nobody can make the thing, and Board
    /// never asked.</b> An order for something only a shopkeeper produces cannot be filled by anybody, ever:
    /// it holds escrow, raises its own offer every StaleMs and charges the buyer more for it each time, and
    /// the goods it is waiting for do not exist. Patrick rolled back exactly this for glass on 04.09.2026 —
    /// see the note in that day's handover — and bandages walked into the same hole from the other side:
    /// nothing in Engines/Craft makes a Bandage, so when the healer's shelf ran dry at 08:00 on 05.09.2026
    /// the shopper put the shortage on the board instead, and the board raised it from five gold to
    /// forty-eight while 7,752 bots stood hurt with nothing to bind a wound with. Twenty-six thousand gold
    /// of escrow against thirteen thousand in every purse on the island.
    /// </para>
    ///
    /// <para>
    /// Asked of the shard's own craft systems rather than of a list kept here, for the reason every other
    /// file in this assembly gives for the same choice: a list is a second copy of the truth and it drifts.
    /// Cached by type, because the answer cannot change while the server is up.
    /// </para>
    /// </summary>
    /// <summary>What craft makes a thing, which skill it is made with, and how much of that skill it takes.</summary>
    /// <summary>
    /// What it takes to make one of a thing: the craft, the trade on a bot's menu, the skill and its
    /// threshold, and the material the recipe consumes.
    ///
    /// <c>Material</c> is the first resource line, which for every recipe on this shard is the one that
    /// matters — ingots for armour, leather for a bustier, boards for a shaft. The others are trimmings.
    /// </summary>
    public readonly record struct BotRecipeFact(
        string Craft,
        string Trade,
        SkillName Skill,
        double MinSkill,
        string Material,
        int Needs
    );

    private static readonly Dictionary<Type, BotRecipeFact?> _recipes = [];

    /// <summary>
    /// Which craft makes this thing, and what it takes.
    ///
    /// <para>
    /// <b>Written because a mind guessed, and guessed wrong within a minute of being given a board.</b> On
    /// 07.09.2026 a thinking crafter read "2 Mind Blast Scrolls, already paid for" and announced it would
    /// <i>forge</i> them. Nothing in what it was shown said which craft makes a scroll, so it reasoned from
    /// the name of its own class — and a want it could have filled in ten minutes became an afternoon at an
    /// anvil that will never produce one.
    /// </para>
    ///
    /// <para>
    /// The skill and its requirement come from the same place as the answer to "can this be made at all":
    /// the engine's own craft systems. Neither is written down here, because a table of recipes maintained
    /// by hand would be a second, slowly diverging copy of the shard's rules — and the divergence would show
    /// up as bots confidently attempting things the engine refuses.
    /// </para>
    /// </summary>
    public static BotRecipeFact? MadeBy(Type wanted)
    {
        if (wanted == null)
        {
            return null;
        }

        if (_recipes.TryGetValue(wanted, out var known))
        {
            return known;
        }

        // The third column is what the trade is called on a bot's own menu of work, and it is here rather
        // than left to be inferred. A mind shown "made by smithing" and offered a list containing Smith,
        // Tailor and Fletcher will still choose Tailor if tailoring is the only craft it feels able to do:
        // that happened within an hour of the board being written, four crafters in a row announcing they
        // would sew ringmail. Naming the trade closes the gap between what must be made and what to pick.
        (string Craft, string Trade, CraftSystem System)[] systems =
        [
            ("smithing", "Smith", BotAnvil.System),
            ("tailoring", "Tailor", BotThread.System),
            ("alchemy", "Alchemist", BotFlask.System),
            ("fletching", "Fletcher", BotFletching.System),
            ("scribing", "Scribe", BotQuill.System)
        ];

        BotRecipeFact? found = null;

        for (var i = 0; i < systems.Length && found == null; i++)
        {
            var recipes = systems[i].System?.CraftItems;

            if (recipes == null)
            {
                continue;
            }

            for (var r = 0; r < recipes.Count; r++)
            {
                var recipe = recipes[r];

                if (recipe?.ItemType != wanted)
                {
                    continue;
                }

                // The first skill line is the one the craft is made with; the others are extras the engine
                // checks alongside it. Taking the lowest of them would answer a different question.
                var skill = recipe.Skills?.Count > 0 ? recipe.Skills[0] : null;

                var resource = recipe.Resources?.Count > 0 ? recipe.Resources[0] : null;

                found = new BotRecipeFact(
                    systems[i].Craft,
                    systems[i].Trade,
                    skill?.SkillToMake ?? SkillName.Blacksmith,
                    skill?.MinSkill ?? 0.0,
                    resource?.ItemType?.Name,
                    resource?.Amount ?? 0
                );

                break;
            }
        }

        // Cached only once the systems exist, for the reason Makeable carries: an answer taken before
        // content initialisation would be remembered for the life of the shard.
        if (systems[0].System != null)
        {
            _recipes[wanted] = found;
        }

        return found;
    }

    public static bool Makeable(Type wanted)
    {
        if (wanted == null)
        {
            return false;
        }

        if (_makeable.TryGetValue(wanted, out var known))
        {
            return known;
        }

        var systems = new[]
        {
            BotAnvil.System, BotThread.System, BotFlask.System, BotFletching.System, BotQuill.System
        };

        var made = false;

        for (var i = 0; i < systems.Length && !made; i++)
        {
            var recipes = systems[i]?.CraftItems;

            if (recipes == null)
            {
                continue;
            }

            for (var r = 0; r < recipes.Count; r++)
            {
                if (recipes[r]?.ItemType == wanted)
                {
                    made = true;

                    break;
                }
            }
        }

        // Only cached once the systems exist. Content initialisation builds them, and an answer of "nobody
        // can make anything" taken before that would be remembered for the life of the shard.
        if (systems[0] != null)
        {
            _makeable[wanted] = made;
        }

        return made;
    }

    private static readonly System.Collections.Generic.Dictionary<Type, bool> _makeable = [];

    /// <summary>Shortages left off the board because nothing on this shard can make the thing.</summary>
    public static long Unmakeable { get; private set; }

    private static BotDeed Board(IBotWilful bot, Mobile body, Map map, Type wanted, int amount)
    {
        if (BotMarket.Wanted(bot, wanted) != null)
        {
            return null;
        }

        // An order nobody can fill is worse than an empty board: it freezes the buyer's money and teaches
        // every seeker that orders are not worth walking to. See Makeable.
        if (!Makeable(wanted))
        {
            Unmakeable++;

            return null;
        }

        var offer = BotMarket.Worth(wanted, Stand(wanted));

        // Pack and bank together, because every seller on this shard is paid by deposit and a bot keeps only
        // a working float on it. Asking the pack alone is the mistake BotArmourer paid for.
        if (BotYield.Spendable(body) - offer * amount <= Reserve)
        {
            return null;
        }

        Asked++;

        return BotOrder.For(map, body.Location, bot, wanted, offer, amount);
    }

    private static void Missing(Type wanted, Map map, Mobile body)
    {
        if (_saidNoShops)
        {
            return;
        }

        _saidNoShops = true;

        // <b>Said of the bot that asked, because that is all this knows.</b> The answer above came from a
        // search made from ONE bot's position within ITS reach; the sentence used to promote that into a
        // claim about the map and the whole population — "nobody will restock it" — at error level. Its
        // sister line in BotTailor did the same and was measured wrong on 02.09.2026: "no shopkeeper sells
        // cloth, so nobody will sew" was printed four times in a session during which thirty-six bots
        // finished sewing and cloth was bought from two named vendors. A per-bot fact reported as a
        // population-wide one is this shard's most-repeated defect, and here it was in the shard's own
        // diagnostics, sending anybody who read the log after the wrong thing entirely.
        logger.Error(
            "{Name} at {Where} on {Map} found no shopkeeper selling {Item} within its own reach; it cannot restock from a counter here",
            body?.Name ?? "a bot",
            body?.Location ?? Point3D.Zero,
            map,
            wanted.Name
        );
    }

    /// <summary>Lets the complaint be made again after a world reload, which may have shops in it.</summary>
    public static void Forget() => _saidNoShops = false;
}
