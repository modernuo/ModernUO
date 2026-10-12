using System;
using System.Collections.Generic;
using Server.Items;
using Server.Logging;
using Server.Mobiles;

namespace Server.BotAI.V2;

/// <summary>
/// Going to the counter when the pack is getting heavy: coin into the account, everything spare into the bank box — except
/// what somebody near has said it wants, which stays in the pack for a carrier's walk.
///
/// <para>
/// <b>Being unable to move is the one failure a bot cannot work its way out of.</b> Past the engine's
/// overweight line every step costs five stamina and more, and with none left the step is refused outright —
/// so the cure for a full pack is a walk to the bank, and a full pack is exactly what stops the walk. Nothing
/// in this project acted on that: the fact was readable and nobody read it.
/// </para>
///
/// <para>
/// So it is caught early, before the line rather than after it. What goes is decided by what a bot is for:
/// coin belongs in an account, surplus in the bank box where the bot's own shop by the bank sells it and a peddle takes what
/// nobody asks for to a shopkeeper, and the kit, the supplies and the tools of the trade stay exactly where they are — a bot
/// that unloaded its own bandages to make room for a rusty sword has not solved anything.
/// </para>
///
/// <para>
/// <b>Nothing goes on a market from here any more (01.10.2026, Patrick's order of 30.09: the auction removed, every purchase
/// hand to hand).</b> The porter used to fill wants and list the rest on the auction's stalls from wherever it stood — 4 things
/// straight to a standing order and 409 trips "because the board wanted something in the pack" in the five minutes to 16:52 on
/// 30.09.2026. A want is answered by a walk now (<see cref="BotCourier"/>), so what somebody within a carrier's reach wants is
/// kept in the pack for that walk (<see cref="Filled"/>) and everything else is put away.
/// </para>
/// </summary>
public sealed class BotUnload : BotDeed
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotUnload));

    /// <summary>The ledger key.</summary>
    public const string Trade = "unload";

    /// <summary>
    /// The share of what it can carry at which a bot heads for the counter.
    ///
    /// Seven tenths, so the walk begins while the bot can still walk briskly. Waiting for the line itself
    /// means starting the journey in the state the journey exists to end.
    /// </summary>
    public static double Heavy { get; set; } = 0.7;

    /// <summary>
    /// How much coin in the pocket is by itself a reason to walk to a counter.
    ///
    /// <para>
    /// The order of 24.08.2026 was "over a hundred goes to the bank", and the pair of numbers that carries it
    /// out is two hundred and fifty here against a hundred kept back — see <c>BotPurse.Float</c>. Setting the
    /// trip at a hundred and one instead would be literal and useless: a bot would walk across the map to
    /// deposit a single coin and be entitled to do it again immediately. Going at two hundred and fifty means
    /// every trip banks at least a hundred and fifty and the bot comes away with the hundred it works on.
    /// </para>
    ///
    /// <para>
    /// The two numbers are one decision and must be read together: a threshold to go that is lower than what
    /// is kept back is a bot that walks to a counter, banks nothing, and sets out again.
    /// </para>
    /// </summary>
    public static int Purse { get; set; } = 250;

    /// <summary>
    /// How much a bot is willing to have on it that somebody could take off its corpse.
    ///
    /// <para>
    /// <b>Weight is about walking, coin is about the market, and this is about risk — the third question,
    /// and it had never been asked.</b> Patrick's order of 08.09.2026: guilds are to fall out, declare war
    /// and loot each other's dead, so "the bots must stop carrying everything they own" stops being a
    /// tidiness point and becomes the difference between a skirmish and a shard-wide redistribution of
    /// everything anybody has made all evening.
    /// </para>
    ///
    /// <para>
    /// Deliberately far below <see cref="Heavy"/>. Seven tenths of a carrying ceiling is a great deal of
    /// loot: a bot could be at a third of its weight, perfectly comfortable, and be walking about with two
    /// thousand gold of ingots that a single lost fight hands to somebody else. This is the number that says
    /// how much of an evening's work one death may cost.
    /// </para>
    ///
    /// <para>
    /// It is measured with <see cref="Sellable"/> — the same reading the counter itself makes — so what the
    /// bot is keeping for its own trade does not count against it. A smith carrying its own hammer and the
    /// ingots for the order it is working is not exposed; a smith carrying forty ingots nobody has asked for
    /// is.
    /// </para>
    ///
    /// <para>
    /// <b>And it is counted in lots, not in gold, which is the correction of 10.09.2026.</b> Everything above
    /// argues in coin - "two thousand gold of ingots", "what one death may cost" - and the number was four
    /// hundred, while <see cref="Sellable"/> has always answered with the <em>number of lots</em> the market
    /// would take. A backpack holds a hundred and twenty-five items, so four hundred was a gate no pack in
    /// any state could open, and the summary read "0 trips were begun because a pack was worth more than a
    /// bot should be carrying about" for the whole of an eight-hour session. Eight lots of other people's
    /// goods is the honest reading of the same intent: a bot with eight kinds of surplus on it is carrying a
    /// shop around. See <see cref="Hoard"/>, which asks the same question by weight - the two together are
    /// "heavy junk" and "much junk", and neither one sees the other's case.
    /// </para>
    /// </summary>
    public static int Risk { get; set; } = 8;

    /// <summary>
    /// What emptying the pack is reckoned at per minute. High, and it should be: a bot that cannot move earns
    /// nothing at all, so this is worth more than whatever it interrupted.
    /// </summary>
    // Lowered from 120 on 16.09.2026: the overstated alarm measured 21/min over 171 outcomes. A bot that cannot move
    // keeps the old number through Stuck, which is what the 120 was for. Patrick's decision, that evening.
    public static double Prior { get; set; } = 30.0;

    /// <summary>
    /// What emptying the pack is reckoned at when the bot cannot move or is jammed: the old prior, kept as the wager
    /// it always was rather than the promise it was measured as. See the number-that-is-both-promise-and-wager
    /// class in DECISIONS.
    /// </summary>
    public static double Stuck { get; set; } = 120.0;

    public static double WorkMinutes { get; set; } = 2.0;

    /// <summary>
    /// How near the counter the bot has to stand. One question, asked once — see
    /// <see cref="BotDig.CounterReach"/>, which is where the reasoning and the engine fact live.
    /// </summary>
    public static int Reach => BotDig.CounterReach;

    private readonly Map _map;

    private readonly Point3D _counter;

    private int _banked;

    private int _stored;

    /// <summary>What the porter counted as worth leaving when it decided to set out. See <see cref="Sellable"/>.</summary>
    private readonly int _expected;

    public BotUnload(Map map, Point3D counter, int expected = 0, bool immobile = false, bool jammed = false)
    {
        _map = map;
        _counter = counter;
        _expected = expected;
        _immobile = immobile;
        _jammed = jammed;
    }

    /// <summary>Whether the bot this was offered to cannot walk at all. See <see cref="Unpaid"/>.</summary>
    private readonly bool _immobile;

    /// <summary>Whether the bot this was offered to has no room in its pack for a coin. See <see cref="Unpaid"/>.</summary>
    private readonly bool _jammed;

    /// <summary>
    /// A trip offered to a bot that cannot walk is not judged on what unloading has paid.
    ///
    /// <para>
    /// <b>Kerrin the Gatherer, 03:52 on 14.09.2026: overloaded at 1582,1493 for twenty-three minutes, thirty-one
    /// errands given up, 340gp down to 133gp.</b> The proposer offered the unload every review — the
    /// immobile branch is exactly for this — and every review the appraisal put it through the ledger,
    /// which knew what unloading had paid this bot at that counter and answered nought or less, so the
    /// offer was refused and a cook at 297/min a hundred and seventy tiles away was taken instead, and
    /// failed on the spot. For a bot that cannot take a step, this trip's worth is that afterwards it can;
    /// the takings are beside the point, and the auction's own rule already puts no-step work first once
    /// the offer is viable at all.
    /// </para>
    ///
    /// <para>
    /// <b>And to a bot whose pack has no room for a coin, for the same reason.</b> A pack at the engine's cap can neither
    /// pay nor be paid over a counter, so every trade that does either is refused to it (see
    /// <see cref="BotDeed.AtCounter"/>), and this trip is what its living waits on. Hale on 14.09.2026 was walking
    /// freely at 255 of 310 stones with 197 oil cloths in the pack, and this was refused to it as expected to pay -47.5
    /// a minute.
    /// </para>
    /// </summary>
    public override bool Unpaid => _immobile || _jammed;

    public override string Kind => Trade;

    /// <summary>
    /// A full pack on its way to a counter is seen through. See <see cref="BotDeed.Steadfast"/>. The only clock
    /// here is the wedge list, which is about the bot rather than the trip, so nothing is put right on resuming.
    /// </summary>
    public override bool Steadfast => true;

    public override Map Map => _map;

    public override Point3D Where => _counter;

    public override double Expects => _immobile || _jammed ? Stuck : Prior;

    public override double Minutes => WorkMinutes;

    public override SkillName? Trains => null;

    public override int Outlay => 0;

    /// <summary>Nothing is earned by putting things away; what it buys is the ability to go on working.</summary>
    public override double Coin => 0.0;

    /// <summary>
    /// Nothing produced. What goes on the market was counted as produced when it was picked up, and counting
    /// it again here would pay the bot twice for one rusty sword.
    /// </summary>
    public override int Made => 0;

    public override string Stage =>
        _banked > 0 || _stored > 0
            ? $"put {_banked}gp away and {_stored} lots in the box"
            : "taking a full pack to the counter";

    /// <summary>
    /// The way to the counter turned out not to exist.
    ///
    /// <para>
    /// <b>Written under the counter's name, which is the word <c>BotGround.Counter</c> asks in.</b>
    /// <c>BotWill.Settle</c> files every failure under the undertaking's name — "unload" — and the counter
    /// lookup has never asked that question, so the nearest counter was chosen on distance alone however many
    /// times it had just been missed. One counter in Britain collected 56 of those in an hour.
    /// </para>
    /// </summary>
    public override bool Bend(IBotWilful bot)
    {
        if (_counter == Point3D.Zero)
        {
            return false;
        }

        bot?.Resolve?.Ledger?.Beware(BotGround.CounterKind, _map, _counter);

        return false;
    }

    /// <summary>
    /// The one errand a bot too heavy to move can still finish: the engine puts a thing in a bot's own bank box from wherever it
    /// stands (<c>Banker.Deposit</c>'s way), so nothing here needs a step. See BotDeed.Standing.
    /// </summary>
    public override bool Standing => true;

    public override BotDoing Advance(IBotWilful bot)
    {
        var body = bot?.Self;

        if (body == null || _map == null || _map == Map.Internal)
        {
            return BotDoing.Failed("no body");
        }

        if (_counter == Point3D.Zero)
        {
            // Offered without a counter only to a bot too heavy to walk (see the proposal), so the only
            // thing left to do is the thing that needs no counter.
            if (BotLadder.Load(body) > BotLadder.Ceiling(body))
            {
                if (Refilling(body))
                {
                    return BotDoing.Failed(Treadmilled(body));
                }

                // The bank needs no counter, so the bot that cannot reach one can still empty its pack into it — wanted goods
                // too: a bot that cannot walk cannot carry them to anybody. See Store.
                var freed = Store(bot, body, true);

                Shed += freed;
                Stranded++;

                if (freed > 0)
                {
                    return BotDoing.Done(
                        $"{freed} lots put in the bank box from where it stood, too heavy to walk and no counter known"
                    );
                }

                // <b>And the dumping belongs here too, which it did not.</b> The branch below reaches it and
                // this one did not, so the same bot in the same state was cured or abandoned according to
                // whether the shard happened to have surveyed a counter - which is a fact about how far
                // other bots have walked, not about this one. Both branches now end the same way.
                return Ditch(bot, body);
            }

            return BotDoing.Failed("nowhere known to put it");
        }

        // <b>Past the line the walk cannot happen, so the goods go into the bank box from where the bot is
        // standing.</b> This file's own opening says it: "the cure for a full pack is a walk to the bank, and
        // a full pack is exactly what stops the walk". Heavy starts the journey at seven tenths so it should
        // never come to this — and it does. Calla the Crafter sat at 293 of 222 stones from 00:36 to 00:42 on
        // 05.09.2026, taking this errand and failing it over and over: "no way through", "it got no nearer
        // than 212 tiles", "it had stopped getting anywhere", then the same errand again a quarter of a
        // minute later. An errand that is offered because the bot cannot move and then requires the bot to
        // move is a treadmill, and this one had no way off it.
        //
        // Storing needs no counter: the bank box is the one place an immobilised bot can still reach. Only the coin needs the
        // counter, and coin is not what is holding it down.
        var stuck = BotLadder.Load(body) > BotLadder.Ceiling(body);

        if (!stuck && !body.InRange(_counter, Reach))
        {
            // The distance the work itself asks for, on the line above. See BotArrival.Beside.
            return BotDoing.Walk(_map, _counter, BotArrival.Within(Reach), "to the counter with a full pack");
        }

        if (stuck && !body.InRange(_counter, Reach))
        {
            if (Refilling(body))
            {
                return BotDoing.Failed(Treadmilled(body));
            }

            var shed = Store(bot, body, true);

            Shed += shed;

            if (shed > 0)
            {
                return BotDoing.Done($"{shed} lots put in the bank box from where it stood, too heavy to walk");
            }

            // <b>Nothing sellable, and it cannot move: the one state on this shard with no way out of
            // itself.</b> Failing here puts the errand straight back on offer — the porter's own test still
            // says the pack holds something worth leaving — and the bot takes it, fails in a fifth of a
            // minute, and takes it again. Quill did that 143 times in five minutes on 09.09.2026 and dragged
            // the whole shard's completion band from 75% to 47% on its own, which is how the alarm found it.
            //
            // So it puts something down. A bot that cannot walk is worth more than the ore nobody will buy,
            // and everything the sale would have taken is exactly what may be dropped: never the kit, never
            // what its trade is bound to, never coin.
            return Ditch(bot, body);
        }

        var carried = body.Backpack?.Items.Count ?? 0;
        var held = Filled;

        _banked = Bank(body);
        _stored = Store(bot, body, false);

        var forWants = Filled - held;

        // <b>A trip that left nothing is not finished work (30.09.2026, build 364).</b> Called finished, it was offered again at
        // once — the porter's count still said the pack held something worth leaving — and the breaker, which rests work a bot
        // keeps failing, never saw it: Adela the Architect "finished unload … nothing to leave here: 122 things in the pack —
        // 18 its own kit or supplies, 0 gold or fixed in place, 104 the market would not take; the porter counted 104 worth
        // leaving" 4868 times in 55 minutes of 363 at Britain's bank, some ninety a minute, and the shard's finished share read
        // 97 % for it (93 % without). Failed, it is rested after the breaker's six, and it no longer counts as work done.
        return _banked > 0 || _stored > 0
            ? BotDoing.Done($"{_banked}gp banked, {_stored} lots put away in the bank, {forWants} things kept for a carrier to take to somebody's want")
            : BotDoing.Failed(
                $"nothing to leave here: {carried} things in the pack, {forWants} of them kept for somebody's want and the rest its own "
                + $"kit, supplies, gold or what the box would not take; the porter counted {_expected} worth leaving when it set out"
            );
    }

    /// <summary>
    /// The surplus over the working float, and <b>not</b> every coin.
    ///
    /// <para>
    /// <b>"Coin is the one thing exactly as useful in an account as in a pocket" is false, and it cost the
    /// shard its whole economy.</b> <c>BaseVendor.OnBuyItems</c> pays for a purchase out of the backpack and
    /// only reaches for the bank when the bill comes to two thousand gold or more — and a bot's purchases are
    /// bandages at five, garlic at three, cloth at a few coins the yard. So a bot that banked every coin on
    /// its first trip to a counter could never buy anything again, however rich it was.
    /// </para>
    ///
    /// <para>
    /// Nothing said so. <see cref="BotYield.Wealth"/> counts the account and the pocket together, so the bot
    /// believed it could afford what it was refused, and the shop's answer was logged as the shop's business.
    /// On the night of 25.08.2026 that read as 1929 failed restocking trips in half an hour, not one bandage
    /// bought in a whole session, and a population that fought, got hurt, could not heal and died — with four
    /// hundred gold apiece in the bank. Three reasonable decisions in three files: bank it all, count both,
    /// spend only the pocket.
    /// </para>
    ///
    /// <para>
    /// The float left behind is <see cref="BotPurse.Float"/> — the same hundred that <see cref="BotPurse"/>
    /// already keeps back on the other path to a counter, asked for here rather than written again, so the
    /// two cannot drift apart.
    /// </para>
    ///
    /// <para>
    /// The coin leaves the pack before the account is credited and comes straight back if the deposit is
    /// refused. The engine's deposit adds to an account without touching what the depositor carries, so any
    /// other order makes gold out of nothing.
    /// </para>
    /// </summary>
    private static int Bank(Mobile body)
    {
        var pack = body.Backpack;
        var carried = pack?.GetAmount(typeof(Gold)) ?? 0;

        // Asked of BotPurse rather than of its float, because "what a bot keeps on it" is a question with
        // more than one answer now and this is the second place that asks it. See BotPurse.Keeps.
        var purse = carried - BotPurse.Keeps(body);

        if (purse <= 0 || !pack.ConsumeTotal(typeof(Gold), purse))
        {
            return 0;
        }

        if (Banker.Deposit(body, purse))
        {
            return purse;
        }

        // A box with no pile of gold and no free place takes no coin either: Ysolde, Sarendil and Malachi carried 770–870gp with
        // nought in the bank at 00:21 on 06.10.2026. Counted (06.10.2026, build next), and Sellable no longer counts such coin.
        pack.DropItem(new Gold(purse));
        Unbanked += purse;

        return 0;
    }

    /// <summary>Coin over the float the bank refused because the box had no pile of gold and no free place. See <see cref="Bank"/>.</summary>
    public static long Unbanked { get; private set; }

    /// <summary>
    /// Whether the bank would take coin from this bot: no box yet (the counter makes one), a pile of gold already in it, or a free
    /// place. The same answer <c>Banker.Deposit</c> gives for any sum under a pile's sixty thousand.
    /// </summary>
    private static bool Bankable(Container box)
    {
        if (box == null || box.Deleted || box.MaxItems == 0 || box.TotalItems < box.MaxItems)
        {
            return true;
        }

        var items = box.Items;

        for (var i = 0; i < items.Count; i++)
        {
            if (items[i] is Gold { Amount: < 50000 })
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Everything over what the bot keeps, put away in the bank instead of ridden around for the rest of the session — except, for
    /// a bot that can walk, what a buyer within a carrier's reach has said it wants (<see cref="BotCourier.Deliverable"/>), which
    /// stays in the pack for the walk. <paramref name="all"/> for a bot too heavy to walk: it cannot carry anything to anybody.
    ///
    /// <para>
    /// <b>Patrick's order of 10.09.2026, and the shard had no other exit for these goods at all.</b> A
    /// surplus left a pack by exactly two doors: a stall on the auction, which refused anything under
    /// two gold, and the ground, which only a bot that cannot walk is ever allowed to use. A
    /// thing that is worth something and worth less than two gold therefore had no door - so it stayed, and
    /// kept staying: 4,738 stones of the population's 6,288 were surplus by the eighth hour of 10.09.2026,
    /// three quarters of everything every bot was carrying, and Ulla stood at 244 of 243 stones with six
    /// hundred and thirty-nine bandages over her allowance.
    /// </para>
    ///
    /// <para>
    /// The bank box is the right place and the engine makes it easy: it needs no counter and no banker -
    /// <c>Banker.Deposit</c> is the same - so this works for the one bot that most needs it, the one too
    /// heavy to walk anywhere. Dropped with <c>TryDropItem</c> and its stacking left on, so six packets of
    /// ash become one lot rather than six of a hundred and twenty-five slots.
    /// </para>
    ///
    /// <para>
    /// Since 01.10.2026 it is the porter's whole errand: the bank box is where a bot's goods wait for its own shop by the bank or a
    /// peddle to a shopkeeper. Anything the bank itself refuses goes straight back in the pack.
    /// </para>
    /// </summary>
    private static int Store(IBotWilful bot, Mobile body, bool all)
    {
        var pack = body?.Backpack;
        var box = body?.BankBox;

        if (pack == null || box == null || box.Deleted)
        {
            return 0;
        }

        // The stacks a full box holds lying apart are joined first: the places they free are what this store needs. See BotSurplus.
        BotSurplus.TidyIfFull(body);

        var keep = Needed(bot);
        Dictionary<Type, int> seen = [];
        Dictionary<Type, int> held = [];
        var put = 0;

        // A snapshot: storing takes things out of the pack, which mutates the list being read.
        List<Item> carried = [.. pack.Items];

        for (var i = 0; i < carried.Count; i++)
        {
            var item = carried[i];

            if (item == null || item.Deleted || !item.Movable || item is Gold)
            {
                continue;
            }

            if (BotBinding.IsBound(item, bot.Bond))
            {
                continue;
            }

            var over = Over(keep, seen, item);

            if (over <= 0)
            {
                continue;
            }

            // Somebody near has asked for it: as many as the island asks for ride in the pack to them (BotCourier), the rest is
            // put away like anything else. Capped (01.10.2026) or one want for five bandages would keep six hundred in a pack.
            if (!all)
            {
                var hold = Hold(bot, body, item.GetType(), over, held, true);

                if (hold > 0)
                {
                    Filled += hold;
                    over -= hold;

                    if (over <= 0)
                    {
                        continue;
                    }
                }
            }

            var lot = item;

            if (over < Math.Max(1, item.Amount))
            {
                // The engine's own split, read the same way the sale reads it: the original keeps the amount
                // asked for and the remainder is put back beside it, so this one is the surplus. A type with
                // no parameterless constructor hands back nothing, and then the stack is left whole.
                if (Mobile.LiftItemDupe(item, over) == null)
                {
                    continue;
                }
            }

            // What it is and how many, read before the drop: a lot that joins a stack is deleted by the join.
            var kind = lot.GetType();
            var units = Math.Max(1, lot.Amount);
            var capped = keep.TryGetValue(kind, out var cap) && cap > 0 && cap < int.MaxValue;

            // A member near its guild's store gives its capped surplus to the store before the box (07.10.2026, officers20). See
            // BotGuildStore.Take: whole lots only, and only what the store has room for; anything else goes on to the box below.
            if (capped && BotGuildStore.Take(body, lot))
            {
                put++;

                continue;
            }

            if (box.TryDropItem(body, lot, false))
            {
                put++;
                Stowed += units;

                if (capped)
                {
                    BotSurplus.Count(BotSurplus.Box, kind, units);
                }

                continue;
            }

            // The box is full or over weight. Back where it came from - a bot's own surplus is not something
            // to leave in a bank's doorway.
            Boxless++;
            pack.DropItem(lot);

            if (capped)
            {
                BotSurplus.Count(BotSurplus.Refused, kind, units);
            }
        }

        Stored += put;

        return put;
    }

    /// <summary>
    /// How long surplus may ride in a pack for a want somebody near has said (06.10.2026, build next; Patrick's order of the night:
    /// "all the surplus they must keep in the bank").
    ///
    /// <para>
    /// <b>A want answered by nobody kept the goods in the pack for as long as it stood, and wants on this island are re-said
    /// as fast as they lapse.</b> Rhiannel asked for heal potions 182 times in the 11 hours of 04.10.2026 and nobody brought
    /// one; every bot within a carrier's reach of her kept its spare draughts in the pack for her the whole time. Fifteen
    /// minutes is three of the market's own five-minute waits: a carrier that has not taken them by then is not going to.
    /// </para>
    /// </summary>
    public static int HoldMs { get; set; } = 900000;

    /// <summary>Units the porter put away although a want near asked for them, because they had waited past <see cref="HoldMs"/>.</summary>
    public static long Overheld { get; private set; }

    /// <summary>When each bot began keeping a kind in its pack for somebody's want. Absence means it is not keeping one.</summary>
    private static readonly Dictionary<(Serial, Type), long> _holdingSince = [];

    /// <summary>
    /// How many of <paramref name="over"/> stay in the pack for a want near (<see cref="BotMarket.Holding"/>), for no longer than
    /// <see cref="HoldMs"/>. <paramref name="storing"/> when the goods are about to go in the box: the units let go are counted
    /// then, and the clock starts afresh for whatever is held next.
    /// </summary>
    private static int Hold(IBotWilful bot, Mobile body, Type kind, int over, Dictionary<Type, int> held, bool storing)
    {
        var hold = BotMarket.Holding(bot, kind, over, held);
        var key = (body.Serial, kind);

        if (hold <= 0)
        {
            _holdingSince.Remove(key);

            return 0;
        }

        var now = Core.TickCount;

        if (!_holdingSince.TryGetValue(key, out var since))
        {
            if (_holdingSince.Count > 8192)
            {
                _holdingSince.Clear();
            }

            _holdingSince[key] = now;

            return hold;
        }

        if (now - since < HoldMs)
        {
            return hold;
        }

        if (storing)
        {
            Overheld += hold;
            _holdingSince.Remove(key);
        }

        return 0;
    }

    /// <summary>Lots put away in a bank box by the porter. See <see cref="Store"/>.</summary>
    public static long Stored { get; private set; }

    /// <summary>Things the porter did not count because the box had no room for them. See <see cref="Sellable(IBotWilful, Mobile, out double)"/>.</summary>
    public static long Unlotted { get; private set; }

    /// <summary>Whether the bank box could take this lot. No box yet is room: the counter makes one.</summary>
    private static bool Boxable(Container box, Item item) =>
        box == null || box.Deleted || box.MaxItems == 0 || box.TotalItems + item.TotalItems + 1 <= box.MaxItems;

    /// <summary>
    /// The same, and a lot that joins a stack the box already holds (<paramref name="piles"/>, see <see cref="BotSurplus.Piles"/>)
    /// needs no place (06.10.2026, box16). The engine's <c>TryDropItem</c> joins a stack before it asks the count, and a bank box
    /// has no weight limit, so twenty bandages go into a full box holding bandages; this said no, and the porter never set out.
    /// </summary>
    private static bool Boxable(Container box, Item item, HashSet<(Type, int, int)> piles) =>
        Boxable(box, item) || item.Stackable && piles != null && piles.Contains((item.GetType(), item.ItemID, item.Hue));

    /// <summary>The stacks a full box holds, read once for a walk of the pack; null for a box with room (every lot fits).</summary>
    private static HashSet<(Type, int, int)> PilesIfFull(Container box) =>
        box is { Deleted: false, MaxItems: > 0 } && box.TotalItems >= box.MaxItems ? BotSurplus.Piles(box) : null;

    /// <summary>And how many units that came to, which is the number that shrinks a pack.</summary>
    public static long Stowed { get; private set; }

    /// <summary>Times the bank box itself would not take any more. Its own bucket: a full box is a fault.</summary>
    public static long Boxless { get; private set; }

    /// <summary>
    /// Things kept in the pack at the counter because a buyer within a carrier's reach wants them. For the summary. Until 01.10.2026
    /// these were handed straight to a standing order on the board from where the porter stood.
    /// </summary>
    public static long Filled { get; private set; }

    /// <summary>
    /// Times the errand was finished with no counter known at all, by a bot too heavy to walk.
    ///
    /// Its own number rather than being folded into <c>Shed</c>: those are bots that could not reach a
    /// counter, these are bots for whom the shard could not name one, and a shard that starts naming none
    /// is a different fault from bots that keep getting stuck out of reach.
    /// </summary>
    public static long Stranded { get; private set; }

    /// <summary>Lots put in the bank box on the spot by a bot too heavy to walk to a counter. For the summary.</summary>
    public static long Shed { get; private set; }

    /// <summary>Things dropped on the ground by a bot that could neither walk nor sell. See <see cref="Dump"/>.</summary>
    public static long Dumped { get; private set; }

    /// <summary>Trips begun because the pack was worth more than the bot should be carrying. See <see cref="Risk"/>.</summary>
    public static long Exposed { get; private set; }

    /// <summary>Counts one such trip.</summary>
    public static void Expose() => Exposed++;

    /// <summary>Times the porter offered the trip because the pack had no room for a coin: offers, one a review while it lasts, not trips. See <see cref="BotYield.Pocket"/> and <see cref="Unpaid"/>.</summary>
    public static long Jammed { get; private set; }

    /// <summary>Counts one such trip.</summary>
    public static void Jam() => Jammed++;

    /// <summary>
    /// Trips offered to a bot with nothing the market wants, purely so that the dumping could be reached.
    ///
    /// <para>
    /// <b>The cure was locked behind a gate meant for a different bot.</b> <see cref="Sellable"/> skipped
    /// anything the auction had condemned as worthless, and <see cref="Dump"/> is written to drop exactly that
    /// - so a pack full of heavy junk answered "nothing worth a trip" at the proposal and never reached the
    /// one method on this shard able to do something about it. Garrow the Mage sat at 173 stones of 131 with
    /// no stamina on 09.09.2026, having sold six and eight things at a time from where it stood, and spent
    /// the gaps taking peddling errands it could not walk. The gate is right for a bot that can walk, since
    /// an empty trip to a counter is the thing this file spent an hour being cured of, and wrong for one
    /// that cannot, since for that bot the errand is not a trip at all.
    /// </para>
    /// </summary>
    public static long Cornered { get; private set; }

    /// <summary>
    /// Of those, the ones where nothing could be dropped either. The genuinely wedged.
    ///
    /// Reading above a handful means bots are reaching a state neither selling nor dropping can undo -
    /// worn kit alone over the ceiling, most likely - and it is the stall watch that has to carry them out,
    /// not this file.
    /// </summary>
    public static long Immovable { get; private set; }

    /// <summary>
    /// How long a bot that could neither sell nor drop is left alone by the porter.
    ///
    /// <b>Not decoration: without it this errand is a treadmill with the brake taken off.</b> The load now
    /// outranks price rather than discounting it (see <c>BotWill.Auction</c>), so an errand that fails
    /// instantly is an errand that wins again instantly. Quill took one 143 times in five minutes on
    /// 09.09.2026 and pulled the whole shard's completion band from 75% to 47%, and that was while the load
    /// was still only a discount. Two minutes is long enough that the loop cannot form and short enough that
    /// a bot which picks up something saleable is served on its next review but one.
    /// </summary>
    public static int WedgedMs { get; set; } = 120000;

    /// <summary>When each bot last proved it could neither sell nor drop. Absence means never.</summary>
    private static readonly Dictionary<Serial, long> _wedged = [];

    /// <summary>
    /// How long a bot whose stand-still pass changed nothing durable is left alone by the porter (06.10.2026, work15).
    ///
    /// <para>
    /// <b>A pass that finishes and leaves the bot exactly as heavy as before is a loop, not work.</b> On 04.10.2026 Aldric
    /// (Crafter, The Lantern) "finished" this errand 587 times, 574 of them in five minutes, each "1 lots put in the bank box from
    /// where it stood, too heavy to walk" in 0.2 minutes: the pass put one lot away, the bot was still over its ceiling, and the
    /// errand was offered again in the same second. The breaker rests work a bot keeps failing and never sees work that finishes,
    /// and <see cref="Wedged"/> rests only a bot with nothing the market would take. What must be true after one pass is that the
    /// bot is lighter than when the last one began; when it is not, the pass fails and the porter leaves it alone this long.
    /// </para>
    /// </summary>
    public static int TreadmillMs { get; set; } = 120000;

    /// <summary>How long after a stand-still pass the next one is compared with it.</summary>
    public static int PassMs { get; set; } = 60000;

    /// <summary>Stand-still passes allowed inside <see cref="PassMs"/> even when each leaves the bot a little lighter: one lot a pass is not a way off the ceiling.</summary>
    public static int MostPasses { get; set; } = 3;

    /// <summary>Stand-still passes refused because the last one, under <see cref="PassMs"/> before, had left the bot no lighter.</summary>
    public static long Treadmills { get; private set; }

    /// <summary>When each bot last began a stand-still pass, how heavy it was then, and how many passes in the window.</summary>
    private static readonly Dictionary<Serial, (long Tick, int Load, int Count)> _passes = [];

    /// <summary>When each bot was last found on the treadmill. See <see cref="TreadmillMs"/>.</summary>
    private static readonly Dictionary<Serial, long> _treadmill = [];

    /// <summary>Whether this bot was lately found putting things away from where it stood without getting any lighter.</summary>
    public static bool Treadmill(Mobile body) =>
        body != null && _treadmill.TryGetValue(body.Serial, out var when) && Core.TickCount - when < TreadmillMs;

    /// <summary>Whether the last stand-still pass, a moment ago, left the bot no lighter than it is now. Records this pass.</summary>
    private static bool Refilling(Mobile body)
    {
        var now = Core.TickCount;
        var load = BotLadder.Load(body);

        var count = 1;

        if (_passes.TryGetValue(body.Serial, out var last) && now - last.Tick < PassMs)
        {
            // No lighter than the last pass left it, or the third pass inside the window: either way not a way off the ceiling.
            if (load >= last.Load || last.Count + 1 >= MostPasses)
            {
                _passes.Remove(body.Serial);
                _treadmill[body.Serial] = now;
                Treadmills++;

                return true;
            }

            count = last.Count + 1;
        }

        if (_passes.Count > 4096)
        {
            _passes.Clear();
        }

        _passes[body.Serial] = (now, load, count);

        return false;
    }

    private static string Treadmilled(Mobile body) =>
        $"still too heavy to walk at {BotLadder.Load(body)} of {BotLadder.Ceiling(body)} stones, no lighter than when the last pass "
        + "put things away from where it stood; left alone for a while";

    /// <summary>Whether this bot lately proved there is nothing this errand can do for it.</summary>
    public static bool Wedged(Mobile body) =>
        body != null && _wedged.TryGetValue(body.Serial, out var when) && Core.TickCount - when < WedgedMs;

    /// <summary>Counts one trip offered only because the bot cannot walk.</summary>
    public static void Corner() => Cornered++;

    /// <summary>
    /// How many of a tool a bot keeps. One to work with and one to take over when it wears through.
    ///
    /// <para>
    /// <b>This was <c>int.MaxValue</c>, and that is not a cap, it is a promise never to let go.</b> A tool is
    /// bought whenever the pack holds none of a kind and is never sold at any count, so every kit a bot ever
    /// picked up off a corpse, was handed at birth, or bought because the one it held was momentarily
    /// elsewhere stayed with it for the life of the shard. Patrick found a bot on 09.09.2026 carrying ten
    /// sewing kits. Ten kits is five hundred acts of tailoring in one pack, twenty stones of it, and nine of
    /// them were invisible to the market because this list said they were not merchandise.
    /// </para>
    ///
    /// <para>
    /// Two rather than one, because a tool that wears through mid-errand with no spare is a trade that stops
    /// until somebody walks to town, and that is the failure this list was written against. The surplus is
    /// not destroyed - it becomes sellable, which puts it on a stall where the next bot short of one buys it
    /// instead of walking to Britain.
    /// </para>
    /// </summary>
    public static int SpareTools { get; set; } = 2;

    /// <summary>
    /// How many stones of things a bot is not allowed to keep is by itself a reason to walk to a counter.
    ///
    /// <para>
    /// <b>The fifth reason to set out, and its absence was worth seven hundred stones of ore.</b> The other
    /// four ask whether the bot is uncomfortable (<see cref="Heavy"/>), rich (<see cref="Purse"/>), wanted
    /// (<c>Wanted</c>) or exposed (<see cref="Risk"/>). None of them asks the plainest question there is:
    /// how much of what it is carrying is not its own. Ore is heavy and cheap, so it trips none of the
    /// value thresholds, and a miner holds it until seven tenths of its ceiling - which on 10.09.2026 was
    /// 652 stones of iron and 122 of copper spread through the population, every ounce of it surplus by the
    /// shard's own rules, sitting between "enough to be worth selling" and "heavy enough to have to".
    /// </para>
    ///
    /// <para>
    /// Forty, which is a load nobody carries by accident: the whole population averaged nineteen stones of
    /// surplus apiece when this was written, and the miners carrying five times that are exactly who it is
    /// for. Measured in stones and not in items on purpose - three hundred reagents and eight ingots are the
    /// same sentence and not the same fault.
    /// </para>
    /// </summary>
    public static int Hoard { get; set; } = 40;

    /// <summary>
    /// How many units of capped kinds over their number send a bot to the box (06.10.2026, build next). Three: "none over by more
    /// than a couple of units" is the target, and a restock tops up to the number exactly, so three over is never a rounding.
    /// </summary>
    public static int KitSlack { get; set; } = 3;

    /// <summary>How near the counter (tiles) a bot with such a surplus must be for the trip to be on its way. Sixty: in town.</summary>
    public static int KitNear { get; set; } = 60;

    /// <summary>How long such a surplus may ride before the trip is made from wherever the bot is. Ten minutes.</summary>
    public static int KitWaitMs { get; set; } = 600000;

    /// <summary>How near a banker (tiles) a bot stands "at a bank": anything over its allowance goes in on the same visit.</summary>
    public static int BankBy { get; set; } = 12;

    /// <summary>Trips offered because capped kinds rode over their number (<see cref="KitSlack"/>): offers, one a review.</summary>
    public static long KitTrips { get; private set; }

    /// <summary>Counts one such offer.</summary>
    public static void Kitted() => KitTrips++;

    /// <summary>Trips offered because the bot stood at a bank with anything over its allowance (<see cref="BankBy"/>).</summary>
    public static long BankTrips { get; private set; }

    /// <summary>Counts one such offer.</summary>
    public static void AtBank() => BankTrips++;

    /// <summary>Since when each bot has carried capped kinds over <see cref="KitSlack"/>. Absence means it does not.</summary>
    private static readonly Dictionary<Serial, long> _kitSince = [];

    /// <summary>Whether this bot's capped surplus has ridden longer than <see cref="KitWaitMs"/>; starts its clock if it had none.</summary>
    public static bool KitLong(Mobile body)
    {
        var now = Core.TickCount;

        if (!_kitSince.TryGetValue(body.Serial, out var since))
        {
            if (_kitSince.Count > 4096)
            {
                _kitSince.Clear();
            }

            _kitSince[body.Serial] = now;

            return false;
        }

        return now - since >= KitWaitMs;
    }

    /// <summary>The bot no longer carries a capped surplus: its clock stops.</summary>
    public static void KitGone(Mobile body) => _kitSince.Remove(body.Serial);

    /// <summary>Trips begun because the pack held more than <see cref="Hoard"/> stones of somebody else's goods.</summary>
    public static long Hoarding { get; private set; }

    /// <summary>Counts one such trip.</summary>
    public static void Hoarded() => Hoarding++;

    /// <summary>
    /// Whether there is anything in this pack the counter could actually take.
    ///
    /// <para>
    /// <b>The porter weighed the whole pack and the counter can only take part of it, and those are two
    /// different numbers on one shelf.</b> A warrior in plate with a spellbook, bandages and reagents sits at
    /// seventy per cent of what it can carry all day with nothing whatever to sell — so it was told its pack
    /// was full, walked across Britain, put nothing down and came back: 23 of 45 trips to a counter on
    /// 26.08.2026 ended "0gp banked, 0 things put on the market". Not a loop that runs away, because the
    /// ledger notices a trade that pays nothing — but half of every porter's afternoon, spent on nothing.
    /// </para>
    ///
    /// <para>
    /// Asked only after the cheap weight test has already passed, which is this project's usual order: the
    /// cheap necessary condition first, the pack walk only for the bots it lets through.
    /// </para>
    /// </summary>
    public static int Sellable(IBotWilful bot, Mobile body) => Sellable(bot, body, out _);

    /// <summary>
    /// The same count, and what it weighs.
    ///
    /// The weight comes out of the same walk of the pack rather than a second one: the porter asks both
    /// questions on every proposal, and the pack is the expensive part of asking either.
    /// </summary>
    public static int Sellable(IBotWilful bot, Mobile body, out double stones) => Sellable(bot, body, out stones, out _);

    /// <summary>
    /// The same count, what it weighs, and how many units of it are of a capped kind (<paramref name="kit"/>): a kind the bot keeps
    /// a number of, over that number — the draughts, bandages, reagents and tools Patrick saw by the handful (06.10.2026, build
    /// next). See <see cref="KitSlack"/>.
    /// </summary>
    public static int Sellable(IBotWilful bot, Mobile body, out double stones, out int kit)
    {
        stones = 0.0;
        kit = 0;

        var pack = body?.Backpack;

        if (pack == null)
        {
            return 0;
        }

        var box = body.FindBankNoCreate();

        // The same question again, and the third place to ask it. A bot saving for a horse carries seven
        // hundred that it is not going to bank, and read against the plain float that is a porter setting out
        // for a counter to put down nothing — the empty walk this file spent an hour being cured of.
        // And only coin the bank would take (06.10.2026, build next): a full box with no pile of gold refuses it. See Bankable.
        var worth = pack.GetAmount(typeof(Gold)) > BotPurse.Keeps(body) && Bankable(box) ? 1 : 0;

        var keep = Needed(bot);
        Dictionary<Type, int> seen = [];
        Dictionary<Type, int> held = [];
        var piles = PilesIfFull(box);

        for (var i = 0; i < pack.Items.Count; i++)
        {
            var item = pack.Items[i];

            if (item == null || item.Deleted || !item.Movable || item is Gold)
            {
                continue;
            }

            // The same reading the sale itself makes, and it has to be the same reading or the porter sets
            // out for a counter with nothing to put down: a stack over the kit's number is merchandise here
            // too, and only the part above the number counts.
            if (BotBinding.IsBound(item, bot.Bond))
            {
                continue;
            }

            var over = Over(keep, seen, item);

            if (over <= 0)
            {
                continue;
            }

            // <b>The same questions the counter will ask on arrival, asked before setting out.</b> Two numbers that disagreed sent
            // porters to banks to leave nothing — 279,067 walks in eight hours on 27.08.2026, and 4869 in the hour of 363 for
            // Adela the Architect alone, her box full. Since 01.10.2026 the counter asks two things: does somebody near want it
            // (then it stays in the pack for a carrier, see Store), and does the box have room for it.
            //
            // <b>And only what the counter will not keep for that want (06.10.2026, build next).</b> This skipped every kind a
            // want near had asked for, whatever the count, while Store keeps only as many as the island asks for: Malachi's 71
            // spare heal potions against a want for two read as nothing worth a trip. Asked the way Store asks it, for no longer
            // than HoldMs.
            over -= Hold(bot, body, item.GetType(), over, held, false);

            if (over <= 0)
            {
                continue;
            }

            if (!Boxable(box, item, piles))
            {
                Unlotted++;

                continue;
            }

            worth++;

            if (keep.TryGetValue(item.GetType(), out var cap) && cap > 0)
            {
                kit += over;
            }

            // Only the part above the allowance weighs against the bot, exactly as only that part is for
            // sale. PileWeight rather than TotalWeight - see BotUnload.Weighed for the night that cost.
            var many = Math.Max(1, item.Amount);

            stones += over * (item.PileWeight + item.TotalWeight) / (double)many;
        }

        return worth;
    }

    /// <summary>
    /// Of a capped kind (a number kept, not all and not none), the units this bot carries over its allowance that its bank box
    /// has no place for and no stack to join, by kind into <paramref name="into"/> (06.10.2026, box16). Nought when the box has
    /// room. The peddler's question: such a surplus has no way into the box, so the shopkeeper's counter is its way out.
    ///
    /// <para>
    /// <b>A full box shut both doors.</b> <see cref="Sellable(IBotWilful, Mobile)"/> leaves a lot the box cannot take out of its count, so
    /// the porter never sets out; and the peddler offered a pack only what is not on the keep list, so a draught, a bandage or a
    /// reagent over the allowance was never a load. Malachi (Gatherer, The Hammer), box 388 things of 125, carried 66 units
    /// over from 02:05 to 05:36 on 06.10.2026: seven peddles of other goods, not one unload.
    /// </para>
    /// </summary>
    public static int Refused(IBotWilful bot, Mobile body, Dictionary<Type, int> into)
    {
        var pack = body?.Backpack;
        var box = body?.FindBankNoCreate();

        if (pack == null || box == null || box.Deleted || box.MaxItems <= 0 || box.TotalItems < box.MaxItems)
        {
            return 0;
        }

        var keep = Needed(bot);
        Dictionary<Type, int> seen = [];
        var piles = BotSurplus.Piles(box);
        var units = 0;

        for (var i = 0; i < pack.Items.Count; i++)
        {
            var item = pack.Items[i];

            if (item == null || item.Deleted || !item.Movable || item is Gold || BotBinding.IsBound(item, bot.Bond))
            {
                continue;
            }

            var over = Over(keep, seen, item);

            if (over <= 0 || !keep.TryGetValue(item.GetType(), out var cap) || cap <= 0 || cap >= int.MaxValue
                || Boxable(box, item, piles))
            {
                continue;
            }

            into[item.GetType()] = into.GetValueOrDefault(item.GetType()) + over;
            units += over;
        }

        return units;
    }

    /// <summary>
    /// Of <paramref name="over"/> units of a kind, how many a want near keeps in the pack for a carrier, for no longer than
    /// <see cref="HoldMs"/>: the porter's own reading, for the peddler. Starts the clock; never lets the units go (the store does).
    /// </summary>
    public static int Holds(IBotWilful bot, Type kind, int over) =>
        bot?.Self == null ? 0 : Hold(bot, bot.Self, kind, over, [], false);

    /// <summary>
    /// What a bot must not sell out from under itself: the tools of its trade, what it shoots, and the
    /// supplies every bot lives on.
    ///
    /// Taken from the same lists birth issued from and shopping restocks from, so a class that changes is
    /// right here without anybody remembering to come back.
    /// </summary>
    /// <summary>
    /// What the last trip to the counter left, and why not more.
    ///
    /// <para>
    /// <b>"0gp banked, 0 things put on the market" is three different sentences and it said none of them.</b>
    /// A porter that arrives and puts nothing down has either brought nothing but its own kit, or brought
    /// things the market would not take, or brought a pack that emptied itself on the road — and 20 of 59
    /// trips on 26.08.2026 ended that way with no means of telling which. Weighing only what a counter can
    /// take (see <see cref="Sellable"/>) cut it from half to a third and stopped there, which is exactly
    /// the point at which guessing has to stop and the nought has to be named.
    /// </para>
    /// </summary>
    /// <summary>
    /// Puts things on the ground until the bot can walk again, heaviest first.
    ///
    /// <para>
    /// <b>The last resort, and it only ever runs for a bot that is over its carrying ceiling with nothing the
    /// market will take.</b> Everything it may drop is what the sale would have sold: the kit stays, whatever
    /// the trade is bound to stays, coin stays — coin weighs almost nothing and is not what is holding it
    /// down. What goes is merchandise nobody wants, and it goes because a frozen bot earns nothing at all
    /// while a dropped ingot is worth what somebody would have paid for it, which by construction is nothing.
    /// </para>
    ///
    /// <para>
    /// Stops the moment the bot is under the ceiling rather than emptying the pack: this is a cure for being
    /// stuck, not a policy about carrying things.
    /// </para>
    /// </summary>
    /// <summary>
    /// The last thing an immobilised bot can try, and the note it leaves if even that fails.
    ///
    /// <para>
    /// One method rather than two copies because there are two ways into this state - a counter is known and
    /// out of reach, or none is known at all - and they are the same state. What differed between them was
    /// only which of the two had been written most recently.
    /// </para>
    /// </summary>
    private static BotDoing Ditch(IBotWilful bot, Mobile body)
    {
        var dumped = Dump(bot, body);

        if (dumped > 0)
        {
            Dumped += dumped;

            return BotDoing.Done($"{dumped} things nobody would buy left on the ground, to be able to walk again");
        }

        // Neither sold nor dropped: there is nothing this errand can do for this bot, and saying so once is
        // worth more than saying it four times a minute for the rest of the night. See WedgedMs.
        Immovable++;
        _wedged[body.Serial] = Core.TickCount;

        return BotDoing.Failed("too heavy to walk, and nothing on it the market would take or the ground would hold");
    }

    private static int Dump(IBotWilful bot, Mobile body)
    {
        var pack = body?.Backpack;

        if (pack == null)
        {
            return 0;
        }

        var keep = Needed(bot);
        Dictionary<Type, int> seen = [];
        var loose = new List<Item>();

        for (var i = 0; i < pack.Items.Count; i++)
        {
            var item = pack.Items[i];

            if (item == null || item.Deleted || !item.Movable || item is Gold)
            {
                continue;
            }

            if (BotBinding.IsBound(item, bot.Bond))
            {
                continue;
            }

            if (Over(keep, seen, item) <= 0)
            {
                continue;
            }

            loose.Add(item);
        }

        // Heaviest first, so the fewest things go down for the most relief.
        loose.Sort((a, b) => b.TotalWeight.CompareTo(a.TotalWeight));

        var put = 0;

        for (var i = 0; i < loose.Count; i++)
        {
            if (BotLadder.Load(body) <= BotLadder.Ceiling(body))
            {
                break;
            }

            // A capped kind's surplus on the ground is counted by kind with the other routes. See BotSurplus.
            if (keep.TryGetValue(loose[i].GetType(), out var cap) && cap > 0 && cap < int.MaxValue)
            {
                BotSurplus.Count(BotSurplus.Ground, loose[i].GetType(), Math.Max(1, loose[i].Amount));
            }

            loose[i].MoveToWorld(body.Location, body.Map);
            put++;
        }

        if (put > 0)
        {
            logger.Warning(
                "{Name} could neither walk nor sell, and has put {Put} things on the ground at {X},{Y} to move again",
                body.Name,
                put,
                body.X,
                body.Y
            );
        }

        return put;
    }

    /// <summary>
    /// How many kinds the pack census names before it stops. The heaviest, so the tail is the cheap tail.
    /// </summary>
    public static int Census { get; set; } = 6;

    /// <summary>
    /// What the whole population is carrying, in stones, and how much of it is surplus.
    ///
    /// <para>
    /// <b>Nothing on this shard has ever printed what is in the packs, and "the bots are carrying a ton of
    /// rubbish" was therefore an impression rather than a number.</b> Every other question about weight has
    /// an instrument - the carrying ceiling has one, the unloading has five - and all of them are about the
    /// bots that are already in trouble. This one is about the ones that are not: weight below the ceiling
    /// costs nothing and is invisible, and it is exactly where a pack quietly fills with things nobody will
    /// ever want.
    /// </para>
    ///
    /// <para>
    /// Reported in stones rather than in items, because that is the unit the problem is in: three hundred
    /// reagents and eight ingots are the same sentence and not the same fault. Surplus is measured against
    /// the same allowance the sale uses, so a line reading "40 of 54 stones surplus" is a claim that can be
    /// checked by selling and watching the number fall.
    /// </para>
    ///
    /// <para>
    /// Walks every pack, so it is on the five-minute summary and nowhere else. Coin and bound kit are
    /// skipped for the same reason <c>BotHand</c>'s reader skips them: they are not merchandise at any
    /// count, and counting them as surplus is an instrument raising an alarm about nothing.
    /// </para>
    /// </summary>
    public static string Weighed()
    {
        var bots = BotPopulation.Bots;
        Dictionary<Type, double> stones = [];
        Dictionary<Type, double> spare = [];
        var carried = 0.0;
        var surplus = 0.0;
        var looked = 0;

        // The capped kinds in units, and the boxes (06.10.2026, build next). See Capped and Boxed.
        Dictionary<Type, int> units = [];
        Dictionary<Type, int> slots = [];
        var overBots = 0;
        var overUnits = 0;
        var worstUnits = 0;
        var worst1 = "";
        var boxes = 0;
        var full = 0;

        // What the full boxes keep out of them now, and the metal every box holds (06.10.2026, box16). See Refused and BotAnvil.Boxed.
        Dictionary<Type, int> blocked = [];
        var blockedBots = 0;
        _metal.Clear();

        for (var i = 0; i < bots.Count; i++)
        {
            var body = bots[i];
            var pack = body?.Backpack;

            if (body == null || body.Deleted || pack == null)
            {
                continue;
            }

            looked++;

            var keep = Needed(body);
            Dictionary<Type, int> seen = [];
            var mine = 0;

            Boxes(body, ref boxes, ref full, slots);

            if (Refused(body, body, blocked) > 0)
            {
                blockedBots++;
            }

            for (var j = 0; j < pack.Items.Count; j++)
            {
                var item = pack.Items[j];

                if (item == null || item.Deleted || item is Gold || BotBinding.IsBound(item, body.Bond))
                {
                    continue;
                }

                var kind = item.GetType();
                var many = Math.Max(1, item.Amount);
                // <b>PileWeight and not TotalWeight, and getting that wrong made this print noughts.</b>
                // Item.TotalWeight is what an item *contains* - it is nought for everything that is not a
                // container - while PileWeight is ceil(Weight x Amount), the stack's own weight. The first
                // census reported "49 packs hold 0 stones" beside bots visibly carrying tools and reagents,
                // which is the instrument lying before the shard does, twice in one night. Both are added,
                // so a bag of something inside a pack is weighed with what is in it.
                var weight = (double)(item.PileWeight + item.TotalWeight);

                carried += weight;
                stones[kind] = (stones.TryGetValue(kind, out var had) ? had : 0.0) + weight;

                // Counted against a running total per kind, because an allowance is about the pack and not
                // about the stack: five bolts of ten under a cap of twenty is thirty over, and asking the
                // question stack by stack would answer nought every time.
                var before = seen.TryGetValue(kind, out var already) ? already : 0;

                seen[kind] = before + many;

                var allowed = keep.TryGetValue(kind, out var cap) ? cap : 0;

                if (allowed >= int.MaxValue)
                {
                    continue;
                }

                var over = Math.Min(many, Math.Max(0, before + many - allowed));

                if (over <= 0)
                {
                    continue;
                }

                var each = weight / many;

                surplus += over * each;
                spare[kind] = (spare.TryGetValue(kind, out var was) ? was : 0.0) + over * each;

                // A capped kind: one the bot keeps a number of. Counted in units, which is how Patrick counted them.
                if (allowed > 0)
                {
                    mine += over;
                    units[kind] = units.GetValueOrDefault(kind) + over;
                }
            }

            if (mine > 0)
            {
                overBots++;
                overUnits += mine;

                if (mine > worstUnits)
                {
                    worstUnits = mine;
                    worst1 = body.Name;
                }
            }
        }

        if (looked == 0)
        {
            return "no packs to weigh";
        }

        BotAnvil.Boxed(_metal);

        var capped = Capped(overBots, overUnits, units, worst1, worstUnits) + "; " + Boxed(boxes, full, slots) + "; "
                     + Blocked(blockedBots, blocked) + "; " + BotSurplus.Describe() + "; " + Metal();

        List<(Type Kind, double Stones)> worst = [];

        foreach (var (kind, weight) in stones)
        {
            worst.Add((kind, weight));
        }

        worst.Sort((a, b) => b.Stones.CompareTo(a.Stones));

        List<string> named = [];
        var many2 = Math.Min(Census, worst.Count);

        for (var i = 0; i < many2; i++)
        {
            var (kind, weight) = worst[i];
            var over = spare.TryGetValue(kind, out var s) ? s : 0.0;

            named.Add(over > 0.0 ? $"{kind.Name} {weight:F0} ({over:F0} surplus)" : $"{kind.Name} {weight:F0}");
        }

        return $"{looked} packs hold {carried:F0} stones, {surplus:F0} of it over what the bots are allowed to "
               + $"keep ({surplus / Math.Max(1.0, carried) * 100.0:F0}%); heaviest: {string.Join(", ", named)}; {capped}";
    }

    /// <summary>
    /// The clause Patrick reads for "5–10 potions though their limit is 2" (06.10.2026, build next): how many bots carry more of a
    /// capped kind than they keep, how many units over by kind (the six largest), and the worst pack by name.
    /// </summary>
    private static string Capped(int bots, int units, Dictionary<Type, int> byKind, string worst, int worstUnits)
    {
        if (bots == 0)
        {
            return "no bot carries more of a capped kind than it keeps";
        }

        List<(Type Kind, int Units)> kinds = [];

        foreach (var (kind, many) in byKind)
        {
            kinds.Add((kind, many));
        }

        kinds.Sort((a, b) => b.Units.CompareTo(a.Units));

        List<string> named = [];

        for (var i = 0; i < kinds.Count && i < Census; i++)
        {
            named.Add($"{kinds[i].Kind.Name} {kinds[i].Units}");
        }

        return $"over the allowance in a capped kind: {bots} bots, {units} units ({string.Join(", ", named)}), the worst {worst} by {worstUnits}";
    }

    /// <summary>How long one bot's <see cref="OverUnits"/> is kept before its pack is read again. The web asks every two seconds.</summary>
    public static int OverMs { get; set; } = 30000;

    private static readonly Dictionary<Serial, (int Units, long Tick)> _overs = [];

    /// <summary>Units this bot carries over its allowance in capped kinds, the same reading as the summary's clause; for /api/state.</summary>
    public static int OverUnits(BotMobile bot)
    {
        var pack = bot?.Backpack;

        if (pack == null)
        {
            return 0;
        }

        var now = Core.TickCount;

        if (_overs.TryGetValue(bot.Serial, out var known) && now - known.Tick < OverMs)
        {
            return known.Units;
        }

        var keep = Needed(bot);
        Dictionary<Type, int> seen = [];
        var units = 0;

        for (var i = 0; i < pack.Items.Count; i++)
        {
            var item = pack.Items[i];

            if (item == null || item.Deleted || item is Gold || BotBinding.IsBound(item, bot.Bond))
            {
                continue;
            }

            if (keep.TryGetValue(item.GetType(), out var cap) && cap > 0)
            {
                units += Over(keep, seen, item);
            }
        }

        if (_overs.Count > 4096)
        {
            _overs.Clear();
        }

        _overs[bot.Serial] = (units, now);

        return units;
    }

    /// <summary>One bot's bank box into the census: whether it is full, and if so what fills its places, by kind.</summary>
    private static void Boxes(Mobile body, ref int boxes, ref int full, Dictionary<Type, int> slots)
    {
        var box = body.FindBankNoCreate();

        if (box == null || box.Deleted)
        {
            return;
        }

        boxes++;

        // The metal every box holds, full or not: the stock a smith's order may draw on (06.10.2026, box16). See BotAnvil.Boxed.
        var held = box.Items;

        for (var i = 0; i < held.Count; i++)
        {
            if (held[i] is BaseIngot { Deleted: false } bar)
            {
                _metal[bar.GetType()] = _metal.GetValueOrDefault(bar.GetType()) + bar.Amount;
            }
        }

        if (box.MaxItems <= 0 || box.TotalItems < box.MaxItems)
        {
            return;
        }

        full++;

        var items = box.Items;

        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];

            if (item != null && !item.Deleted)
            {
                var kind = item.GetType();

                slots[kind] = slots.GetValueOrDefault(kind) + 1 + item.TotalItems;
            }
        }
    }

    /// <summary>The bars in every bot's bank box by metal, as the last census read them. See <see cref="Metal"/>.</summary>
    private static readonly Dictionary<Type, int> _metal = [];

    /// <summary>
    /// What the full boxes keep out of them now (06.10.2026, box16): units of capped kinds over the allowance riding in packs whose
    /// box has no place for them and no stack to join, by kind, and how many bots carry them. A stock, read every census: the
    /// figure that must fall. See <see cref="Refused"/>.
    /// </summary>
    private static string Blocked(int bots, Dictionary<Type, int> byKind)
    {
        if (bots == 0)
        {
            return "no capped surplus is kept out of a pack's own box by a full box";
        }

        List<(Type Kind, int Units)> kinds = [];
        var units = 0;

        foreach (var (kind, many) in byKind)
        {
            kinds.Add((kind, many));
            units += many;
        }

        kinds.Sort((a, b) => b.Units.CompareTo(a.Units));

        List<string> named = [];

        for (var i = 0; i < kinds.Count && i < Census; i++)
        {
            named.Add($"{kinds[i].Kind.Name} {kinds[i].Units}");
        }

        return $"{bots} bots carry {units} units of capped surplus their full box has no place for ({string.Join(", ", named)})";
    }

    /// <summary>The bars in all bank boxes by metal, most first: the stock no shopkeeper buys but iron, which a smith's order may draw on.</summary>
    private static string Metal()
    {
        if (_metal.Count == 0)
        {
            return "no bars in any bank box";
        }

        List<(Type Kind, int Units)> kinds = [];
        var units = 0;

        foreach (var (kind, many) in _metal)
        {
            kinds.Add((kind, many));
            units += many;
        }

        kinds.Sort((a, b) => b.Units.CompareTo(a.Units));

        List<string> named = [];

        for (var i = 0; i < kinds.Count; i++)
        {
            named.Add($"{BotAnvil.Named(kinds[i].Kind)} {kinds[i].Units}");
        }

        return $"bars in bank boxes: {units} ({string.Join(", ", named)})";
    }

    /// <summary>
    /// How many bank boxes are full and what fills them. A full box refuses every lot and every coin with no pile to join (see
    /// <see cref="Bankable"/>), and 62,204 refusals in the eleven hours of 04.10.2026 were that, against 10,255 lots stored.
    /// </summary>
    private static string Boxed(int boxes, int full, Dictionary<Type, int> slots)
    {
        if (full == 0)
        {
            return $"{boxes} bank boxes, none full";
        }

        List<(Type Kind, int Places)> kinds = [];

        foreach (var (kind, places) in slots)
        {
            kinds.Add((kind, places));
        }

        kinds.Sort((a, b) => b.Places.CompareTo(a.Places));

        List<string> named = [];

        for (var i = 0; i < kinds.Count && i < Census; i++)
        {
            named.Add($"{kinds[i].Kind.Name} {kinds[i].Places}");
        }

        return $"{full} of {boxes} bank boxes full (their places: {string.Join(", ", named)})";
    }

    /// <summary>
    /// What this bot is allowed to keep, by kind, for whoever wants to look rather than to sell.
    ///
    /// <b>The list that decides what a pack fills up with had no reader outside this file.</b> "Why is that
    /// bot carrying ten sewing kits" was a question about this table, and answering it meant reading the
    /// source rather than the shard - so a number that was wrong for a month looked exactly like a number
    /// that was right. See <c>BotHand</c>'s "pack" verb, which is the whole reason this is public.
    /// </summary>
    public static Dictionary<Type, int> Keeps(IBotWilful bot) => bot == null ? [] : Needed(bot);

    /// <summary>
    /// How many of this stack are surplus, counting what has already been passed over in the same walk.
    ///
    /// <para>
    /// <b>The allowance is about the pack and every place that read it asked about the stack.</b> The check
    /// was <c>item.Amount &lt;= allowed</c>, item by item — so a bot with ten sewing kits kept all ten,
    /// because a sewing kit does not stack and each one is an amount of one against an allowance of two;
    /// and a tailor with five bundles of ten leather kept all fifty against an allowance of twenty, because
    /// no single bundle was over it. The intent is written three lines above the sale in this same file —
    /// "keeping to that number and listing the rest is the whole of the difference between a population of
    /// hoarders and a market" — and the code had never done it.
    /// </para>
    ///
    /// <para>
    /// Found on 10.09.2026 by the pack census disagreeing with the seller: <c>Weighed</c> counts a running
    /// total per kind and printed "Pickaxe 253 (121 surplus)" while <c>Sell</c> passed over every one of
    /// them. Two readings of one allowance, and the instrument happened to be the one that was right.
    /// </para>
    ///
    /// <para>
    /// <c>seen</c> is the running total and this method advances it, so it must be called exactly once per
    /// item per walk of a pack. Returns nought for anything kept whole, and never more than the stack holds.
    /// </para>
    /// </summary>
    private static int Over(Dictionary<Type, int> keep, Dictionary<Type, int> seen, Item item)
    {
        var kind = item.GetType();
        var many = Math.Max(1, item.Amount);
        var before = seen.TryGetValue(kind, out var had) ? had : 0;

        seen[kind] = before + many;

        if (!keep.TryGetValue(kind, out var allowed))
        {
            return many;
        }

        if (allowed >= int.MaxValue)
        {
            return 0;
        }

        return Math.Clamp(before + many - allowed, 0, many);
    }

    private static Dictionary<Type, int> Needed(IBotWilful bot)
    {
        var body = bot.Self;
        var kit = bot.Class?.Kit;

        // How many of each, and not merely which. A cap of nought means the bot has no use for the thing at
        // all and every one of them is merchandise; anything left uncapped below is kept whole as before.
        var reagents = kit?.Reagents ?? 0;

        Dictionary<Type, int> keep = new()
        {
            [typeof(Bandage)] = kit?.Bandages ?? 0,
            [typeof(SulfurousAsh)] = reagents,
            [typeof(BlackPearl)] = reagents,
            [typeof(Garlic)] = reagents,
            [typeof(Ginseng)] = reagents,
            [typeof(SpidersSilk)] = reagents,
            [typeof(Nightshade)] = reagents,
            [typeof(Bloodmoss)] = reagents,
            [typeof(MandrakeRoot)] = reagents
        };

        // Paper is left uncapped on purpose: a scribe's stock is the one supply here whose right quantity is
        // "as much as it can write", the kit names no number for it, and a guess would be a threshold nobody
        // could defend. It is cheap and it is on a shelf.
        keep[typeof(BlankScroll)] = int.MaxValue;

        // <b>Glass is stock to an alchemist and rubbish to everybody else, and it was kept by everybody.</b>
        // A potion leaves its bottle behind, so a bot that drinks all day accumulates empties it will never
        // have a use for — protected from sale by a list that was written for the one class that fills them.
        // Fifteen bots carrying somebody else's raw material is weight nobody is paid for, and a brewer short
        // of glass buying more while fifteen packs hold it is the market failing at the one thing it is for.
        if (BotOutfit.Brews(bot.Class))
        {
            keep[typeof(Bottle)] = int.MaxValue;
        }

        // <b>A crafter's raw material is stock, not merchandise, and it was being sold out from under every
        // one of them.</b> This list held supplies and tools and knew nothing about what a trade eats, so a
        // smith walked to a counter and put its own iron on a stall, a fletcher its own wood and feathers, a
        // tailor its own hide. The market's own counters named it and nobody was reading them: at 13:31 on
        // 04.09.2026 the smith's ordering line read "157 have their own out on a stall" — a hundred and
        // fifty-seven refusals to order iron because the bot was already selling iron it needed — and the
        // materials board read "153 have their own out on a stall" beside it.
        //
        // Kept to a working quantity and no further, exactly as the supplies above are: what is over a
        // batch is genuinely surplus and belongs on the market, which is where another crafter will find it.
        // Asked of the tool in the pack, so a bot that takes up a trade tomorrow keeps its stock tomorrow.
        if (BotFletching.Kit(body) != null)
        {
            keep[typeof(Feather)] = BotFletching.LeastArrows;
            keep[typeof(Log)] = BotFletching.LeastArrows;
            keep[typeof(Shaft)] = BotFletching.LeastArrows;
        }

        if (BotThread.Kit(body) != null)
        {
            keep[typeof(Leather)] = BotSew.Bolt;
        }

        if (BotAnvil.Kit(body) != null)
        {
            // Every metal the smith could work, not iron alone — see BotAnvil.Keep for why the two lists had
            // to become one before BotSmith was allowed to fetch its own stock back.
            // And as much as the dearest order it passed over takes, while it remembers one (03.10.2026, build next). See BotSmith.Short.
            BotAnvil.Keep(body, keep, Math.Max(BotBullion.Enough, BotSmith.Short(body)));
        }

        if (BotFlask.Kit(body) != null)
        {
            keep[typeof(Bottle)] = int.MaxValue;
        }

        // <b>A tinker's own spare kits are its stock (01.10.2026).</b> BotTinkering.Choose makes tinker's tools first whenever the
        // pack holds fewer than Spares; the stint kept those back from the stall, and with the stall gone the porter would put them
        // in the box and the next stint would make more — Leofwin's 103 stints of tinker's tools in 55 minutes of build 361.
        if (BotTinkering.Kit(body) != null)
        {
            keep[typeof(TinkerTools)] = Math.Max(keep.GetValueOrDefault(typeof(TinkerTools)), BotTinkering.Spares);
        }

        // A tool is not a supply, and a crafter with none has no trade at all — see BotShopper.Wanting, which
        // calls that the whole of what stands between "tools wear out" and "trades quietly end". So it is
        // kept, and it is kept to a number: see SpareTools for the ten sewing kits that were the cost of not
        // having one.
        var tools = BotOutfit.ToolsFor(bot.Class);

        for (var i = 0; i < tools.Count; i++)
        {
            keep[tools[i]] = SpareTools;
        }

        // <b>The class already says how many of each draught it wants and this threw the number away.</b>
        // BotOutfit.PotionsFor returns the kind and the count together, and the count is klass.PotionLimit -
        // the same number a bot is born with and the same one BotArsenal tops it up to. Writing MaxValue over
        // it meant every potion a bot ever bought, brewed or looted was kept for ever: eight heals in one
        // pack, against a class limit that had said what it wanted all along.
        var bottles = BotOutfit.PotionsFor(bot.Class);

        for (var i = 0; i < bottles.Count; i++)
        {
            var (kind, count) = bottles[i];

            // Less the draughts it brewed on a buyer's advance and owes it (03.10.2026, econ13): those are the buyer's, not the kit's,
            // so the carrier's look offers them to the want that paid for their glass. See BotAdvance.Owing.
            keep[kind] = Math.Max(0, Math.Max(1, count) - BotAdvance.Owing(body, kind));
        }

        // <b>The attack scrolls BotArmoury lays in were the one stock with no entry here, so every one of them was
        // merchandise.</b> The armoury bought three, the next trip to a counter listed all three, and the armoury
        // bought three more: the potions' mistake above, with the number held by the buyer and a nought by the
        // seller. Asked of BotArmoury.Kept, the question the shopping asks, so the two cannot name different
        // scrolls — and a scroll the bot has since outgrown goes to market, as it should.
        var thrown = BotArmoury.Kept(body, out _);

        if (thrown != null)
        {
            keep[thrown] = Math.Max(keep.TryGetValue(thrown, out var already) ? already : 0, BotArmoury.Stock);
        }

        // <b>And the resurrection scroll a caster carries for the one of its company that falls (02.10.2026).</b> See BotLifeline.
        if (BotRaise.Reads(body))
        {
            keep[typeof(ResurrectionScroll)] = Math.Max(keep.GetValueOrDefault(typeof(ResurrectionScroll)), BotRaise.Stock);
        }

        var ammunition = bot.Bond?.Weapon?.Ammunition;

        if (ammunition != null)
        {
            keep[ammunition] = int.MaxValue;
        }

        // A trader's goods bought against the orders in its book are promised, not surplus (01.10.2026, build 385). See BotTrading.Keep.
        BotTrading.Keep(bot, keep);

        return keep;
    }
}

/// <summary>
/// Offers the trip to the counter to any bot whose pack is filling up.
///
/// <para>
/// Only when it is actually heavy, so it costs nothing the rest of the time — and when it is offered it wins,
/// because a bot that cannot walk cannot do anything else either.
/// </para>
/// </summary>
public sealed class BotPorter : IBotProposer
{
    public string Name => "Porter";

    public BotStanding Rung => BotStanding.Free;

    public BotDeed Propose(IBotWilful bot)
    {
        var body = bot?.Self;
        var map = body?.Map;

        if (map == null || map == Map.Internal || !body.Alive)
        {
            return null;
        }

        // <b>Two reasons to walk to a counter, and only the first one existed.</b> A heavy pack is the
        // obvious one and it is what this measured — but a coin weighs next to nothing, so a bot could carry
        // eight hundred gold across the map for hours and never once qualify as loaded. The population's
        // whole takings sat in fifteen backpacks, went into the ground with whoever died, and the bank line
        // in every summary read nought banked while gold was plainly arriving. Money in a pocket is money one
        // bad fight from being gone, and money in a pocket is also money the market cannot see.
        var laden = BotLadder.Load(body) >= BotLadder.Ceiling(body) * BotUnload.Heavy;
        var flush = (body.Backpack?.TotalGold ?? 0) >= BotUnload.Purse;

        // (A third reason — a want on the board for something in the pack — sent the porter to a counter to fill it from there. A
        // want is answered by a carrier's walk to the buyer now: see BotCourier, 01.10.2026.)

        // <b>Whether the bot can take a step at all, and it is read here rather than further down now.</b>
        // Every gate below this line was written to spare a walking bot a pointless trip; not one of them
        // makes sense for a bot that is not going to walk anywhere, because for that bot this errand is not
        // a trip. See BotUnload.Cornered.
        var immobile = BotLadder.Load(body) > BotLadder.Ceiling(body);

        // <b>A sixth, about the pack's slots rather than its weight.</b> See BotYield.Pocket: a pack at the engine's
        // cap can neither pay nor be paid at a counter, so it is refused every trade that does either, and the only
        // way back to them is through here.
        var jammed = !immobile && !BotYield.Pocket(body);

        // Weight is a necessary condition and not a sufficient one. See BotUnload.Sellable - and it is asked
        // before the gate now rather than after it, because the fourth reason to set out is a fact about what
        // the pack is worth. Costs a walk of the backpack per proposal; the alternative is a number that
        // cannot be read until one of the other three has already fired, which is the same as not having it.
        // A full box's stacks lying apart are joined before the count, so the room they free is counted (06.10.2026, box16): the
        // box holds its bot's goods from wherever it stands, as the store does. Once in BotSurplus.TidyMs a bot. See BotSurplus.
        BotSurplus.TidyIfFull(body);

        var worth = BotUnload.Sellable(bot, body, out var spare, out var kit);

        // A bot that cannot walk is offered this with an empty answer here, because the thing it needs is
        // not the sale. Sellable counts what the market would take and Dump exists to put down exactly what
        // the market would not, so for this one bot the gate excluded precisely the case the errand was
        // last extended to cover.
        if (worth <= 0 && !immobile)
        {
            return null;
        }

        // ...unless it has just proved there is nothing to put down either. Without this the errand fails in
        // a fifth of a minute and wins the next review outright, for ever. See BotUnload.WedgedMs.
        if (worth <= 0 && BotUnload.Wedged(body))
        {
            return null;
        }

        // Nor to a bot that cannot walk whose last stand-still pass left it no lighter. See BotUnload.TreadmillMs.
        if (immobile && BotUnload.Treadmill(body))
        {
            return null;
        }

        // <b>The fourth reason, and the only one that is about what a death would cost.</b> See BotUnload.Risk.
        var exposed = worth >= BotUnload.Risk;

        // <b>The fifth, and the only one that asks how much of the load is not the bot's own.</b> See
        // BotUnload.Hoard: ore is heavy and cheap, so it slips past every threshold above and rides around
        // in a miner's pack until sheer weight forces the trip.
        var hoarding = spare >= BotUnload.Hoard;

        var counter = Point3D.Zero;
        var looked = false;

        // <b>The seventh and the eighth (06.10.2026, build next; Patrick: "5–10 potions though their limit is 2").</b> Ten draughts
        // weigh ten stones and are two lots, so no reason above ever fired for them. Both are made false by the trip itself: Store
        // puts every unit over the allowance in the box, except what a want near keeps for HoldMs; and what the box has no place
        // for is not counted by Sellable (Unlotted), so a full box cannot send the bot back.
        var kitted = false;
        var banked = false;
        var kitLong = false;

        if (kit >= BotUnload.KitSlack)
        {
            kitLong = BotUnload.KitLong(body);
        }
        else
        {
            BotUnload.KitGone(body);
        }

        if (!laden && !flush && !exposed && !hoarding && !immobile && !jammed && worth > 0)
        {
            counter = BotGround.Counter(bot, body.Location);
            looked = true;

            if (counter != Point3D.Zero)
            {
                var tiles = Math.Max(Math.Abs(counter.X - body.X), Math.Abs(counter.Y - body.Y));

                // (a) Standing at a bank already, for whatever reason: anything over the allowance goes in on the same visit. Goods,
                // not coin alone: a keeper on its shift by the bank takes coin all the time, and Purse is the coin's own reason.
                var goods = worth - (body.Backpack.TotalGold > BotPurse.Keeps(body) ? 1 : 0);

                banked = goods > 0 && tiles <= BotUnload.BankBy;

                // (b) A capped kind over its number by more than a couple: on the way when a counter is near, on its own once the
                // surplus has ridden KitWaitMs.
                kitted = !banked && kit >= BotUnload.KitSlack && (tiles <= BotUnload.KitNear || kitLong);
            }
        }

        // <b>The ninth: the pack refused goods its bot had asked for (06.10.2026, deliver16).</b> Whatever the box can take goes in now,
        // so the next carrier's lot finds a place; made false by the trip itself, and the mark clears once the pack shows room.
        // See BotRoom.
        var roomy = !laden && !flush && !exposed && !hoarding && !immobile && !jammed && !kitted && !banked && BotRoom.Pressing(body);

        if (!laden && !flush && !exposed && !hoarding && !immobile && !jammed && !kitted && !banked && !roomy)
        {
            return null;
        }

        if (!looked)
        {
            counter = BotGround.Counter(bot, body.Location);
        }

        // <b>A bot that cannot walk is offered this even when no counter is known, and the alternative was a
        // monument.</b> Joss the Gatherer stood on one tile for thirteen minutes on 07.09.2026 at 247 of 236
        // stones with no stamina, outside any named region, taking and dropping thirty-two errands - every
        // one of which needed a step it could not take. The counter list is built by bots walking past
        // counters and is not saved, so a shard fresh from a restart can easily know none; and the errand
        // that cures an unwalkable pack was refused for want of somewhere to walk to. Storing on the spot
        // needs no counter at all - the bank box takes a thing from wherever its owner stands - so the one bot
        // that most needs this errand is not the one that cannot have it.
        if (counter == Point3D.Zero && !immobile)
        {
            return null;
        }

        if (worth <= 0)
        {
            BotUnload.Corner();
        }

        if (exposed && !laden && !flush)
        {
            BotUnload.Expose();
        }

        if (hoarding && !laden && !flush && !exposed)
        {
            BotUnload.Hoarded();
        }

        if (jammed)
        {
            BotUnload.Jam();
        }

        if (kitted)
        {
            BotUnload.Kitted();
        }

        if (banked)
        {
            BotUnload.AtBank();
        }

        if (roomy)
        {
            BotRoom.Tripped();
        }

        return new BotUnload(map, counter, worth, immobile, jammed);
    }
}
