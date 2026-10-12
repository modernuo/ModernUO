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
                // <b>And the dumping belongs here too, which it did not.</b> The branch below reaches it and
                // this one did not, so the same bot in the same state was cured or abandoned according to
                // whether the shard happened to have surveyed a counter - which is a fact about how far
                // other bots have walked, not about this one. Both branches end in the same pass now. See StandStill.
                return StandStill(bot, body, false);
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
            // <b>Nothing sellable, and it cannot move: the one state on this shard with no way out of
            // itself.</b> Failing here puts the errand straight back on offer — the porter's own test still
            // says the pack holds something worth leaving — and the bot takes it, fails in a fifth of a
            // minute, and takes it again. Quill did that 143 times in five minutes on 09.09.2026 and dragged
            // the whole shard's completion band from 75% to 47% on its own, which is how the alarm found it.
            //
            // So it puts something down. A bot that cannot walk is worth more than the ore nobody will buy. What
            // goes, and in what order, is StandStill's: piles joined, coin banked, surplus boxed, then the ground,
            // and the kept things last of all, loudly.
            return StandStill(bot, body, true);
        }

        // Piles of one kind lying apart weigh more than the same things in one pile (see Gather); joined before the counter is
        // asked anything, so the lots it counts are the lots there are.
        Gather(body, true);

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

        // <b>From where it stands, the wares go to the box first (12.10.2026, build 434's check).</b> A box with a few places
        // left fills in the pack's own order otherwise, and logs could take the last place a crafter's arrows needed: what was
        // made to sell, then the dearest a stone, so whatever the box cannot take is the cheapest to put down. See Dump.
        if (all)
        {
            carried.Sort(
                (a, b) =>
                {
                    var made = b.PlayerConstructed.CompareTo(a.PlayerConstructed);

                    return made != 0 ? made : WorthPerStone(b).CompareTo(WorthPerStone(a));
                }
            );
        }

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

    /// <summary>
    /// Whether the last stand-still pass, a moment ago, left the bot no lighter than it is now, or this is the
    /// <see cref="MostPasses"/>th inside <see cref="PassMs"/>. <paramref name="lighter"/> tells the second case: refused, and
    /// not a pass that got nowhere, so the breaker does not count it (12.10.2026, Fable's review). Records this pass.
    /// </summary>
    private static bool Refilling(Mobile body, out bool lighter)
    {
        var now = Core.TickCount;
        var load = BotLadder.Load(body);

        var count = 1;

        lighter = false;

        if (_passes.TryGetValue(body.Serial, out var last) && now - last.Tick < PassMs)
        {
            // No lighter than the last pass left it, or the third pass inside the window: either way not a way off the ceiling.
            if (load >= last.Load || last.Count + 1 >= MostPasses)
            {
                lighter = load < last.Load;
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

    private static string Treadmilled(Mobile body, bool lighter) =>
        lighter
            ? $"still too heavy to walk at {BotLadder.Load(body)} of {BotLadder.Ceiling(body)} stones after {MostPasses} passes inside "
              + $"{PassMs / 1000}s, each a little lighter; left alone for a while"
            : $"still too heavy to walk at {BotLadder.Load(body)} of {BotLadder.Ceiling(body)} stones, no lighter than when the last "
              + "pass put things away from where it stood; left alone for a while";

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
    // ---- The stand-still pass: a way down from over the ceiling (12.10.2026) ---------------------------------------------------

    /// <summary>
    /// Whether the stand-still pass may let go of a bot's kept things — its supplies and tools, never a kind bound to it by count
    /// (its quiver) and never what weighs nothing — when they alone hold it over its ceiling: into its bank box, or onto the
    /// ground when the box will not take them.
    ///
    /// <para>
    /// <b>The kit was the one load with no way down.</b> <see cref="Store"/> and <see cref="Dump"/> both pass over everything
    /// within the allowance, and ammunition is kept without limit, so a bot whose kept things alone weighed more than it can
    /// carry stood where it was until it died: Baern the Tamer, 225–276 stones of 222–229 from 09:28 on 10.10.2026 to 11:11 on
    /// 12.10.2026, 1 035 of the 5 920 failures of 11.10 (17.5 %), cured only by the death that put his arrows into one pile.
    /// A kit a bot cannot carry is a wrong kit, and a bot that cannot take a step earns nothing with it; the pass says so
    /// (a warning naming the heaviest things) and lets go of it, least worth a stone first, until the bot can walk.
    /// </para>
    /// </summary>
    public static bool Releases { get; set; } = true;

    /// <summary>
    /// Stand-still passes that left a bot no lighter, inside <see cref="StuckWindowMs"/>, before the porter stops offering it
    /// the pass at all until its weight changes. See <see cref="SetAside"/>.
    ///
    /// <para>
    /// <b>The rests already there were clocks, and a clock ends.</b> <see cref="WedgedMs"/> and <see cref="TreadmillMs"/> each
    /// rested Baern for two minutes, and two minutes later the porter offered the same pass, which outbid whatever he was doing
    /// ("dislodged by unload") and failed in a fifth of a minute: 454 unload failures in the 10.2 hours of 11.10, 45 an hour,
    /// and every one of them a decision the auction made for nothing. Three in ten minutes is three of those rests in a row:
    /// a pass that has failed that often from the same weight will fail again from it.
    /// </para>
    /// </summary>
    public static int StuckFails { get; set; } = 3;

    /// <summary>The window <see cref="StuckFails"/> is counted over. Ten minutes.</summary>
    public static int StuckWindowMs { get; set; } = 600000;

    /// <summary>
    /// How often a heavy bot that can still walk has its pack gone through for piles of one kind lying apart. A bot over its
    /// ceiling is gone through every time. See <see cref="Gather"/>.
    /// </summary>
    public static int JoinMs { get; set; } = 60000;

    /// <summary>Piles of one kind lying apart in a pack, joined onto one pile of their kind. See <see cref="Gather"/>.</summary>
    public static long Joined { get; private set; }

    /// <summary>Packs in which piles were joined.</summary>
    public static long JoinedPacks { get; private set; }

    /// <summary>Stones the joins took off the packs: what the piles weighed apart over what they weigh together.</summary>
    public static long JoinedStones { get; private set; }

    /// <summary>Coin over its float a bot too heavy to walk put in its account from where it stood. See <see cref="StandStill"/>.</summary>
    public static long StoodBanked { get; private set; }

    /// <summary>Stand-still passes that found the kept things alone holding the bot over its ceiling: the kit was wrong.</summary>
    public static long KitOver { get; private set; }

    /// <summary>Kept things let go by a bot that could not carry them. See <see cref="Release"/>.</summary>
    public static long Released { get; private set; }

    /// <summary>Of those, into the bot's own bank box.</summary>
    public static long ReleasedBoxed { get; private set; }

    /// <summary>Of those, onto the ground, because the box would not take them.</summary>
    public static long ReleasedGround { get; private set; }

    /// <summary>What everything a stand-still pass left on the ground was worth at the market's reckoning, surplus and kept.</summary>
    public static long GroundWorth { get; private set; }

    /// <summary>Bots set aside by the breaker: <see cref="StuckFails"/> no-lighter passes in <see cref="StuckWindowMs"/>.</summary>
    public static long StuckTrips { get; private set; }

    /// <summary>Offers of the pass withheld from a bot the breaker had set aside: offers, one a review while it lasts.</summary>
    public static long StuckWithheld { get; private set; }

    /// <summary>Bots over their ceiling and set aside by the breaker at the last pack census. For an alarm to read.</summary>
    public static int StuckNow { get; private set; }

    /// <summary>When each bot's run of no-lighter passes began, how many, and the load they all left it at. Absence means none.</summary>
    private static readonly Dictionary<Serial, (long Since, int Count, int Load)> _noLighter = [];

    /// <summary>The load at which the breaker set each bot aside. Absence means it is not set aside.</summary>
    private static readonly Dictionary<Serial, int> _stuckAt = [];

    /// <summary>When each bot's pack was last gone through for piles lying apart. See <see cref="JoinMs"/>.</summary>
    private static readonly Dictionary<Serial, long> _gatheredAt = [];

    /// <summary>What one top-level thing in a pack weighs: its own pile and, for a container, what is in it.</summary>
    private static int Stones(Item item) => item.PileWeight + item.TotalWeight;

    /// <summary>
    /// Everything a bot over its ceiling can do without taking a step, in one pass, in this order, stopping the moment it is
    /// under: piles of one kind lying apart joined (<see cref="Gather"/>), coin over its float into its account
    /// (<see cref="Bank"/>), surplus into its bank box (<see cref="Store"/>), what the box refused onto the ground
    /// (<see cref="Dump"/>), and last the kept things themselves (<see cref="Release"/>), loudly.
    ///
    /// <para>
    /// <b>One pass, all the way down (12.10.2026).</b> The pass used to stop at the first step that did anything: "1 lots put
    /// in the bank box from where it stood" nineteen times on 12.10.2026 for Baern, each lot a stone or two, while the 177
    /// arrows that weighed 177 stones were never reached. And the coin: <see cref="Sellable"/> counts coin over the float as
    /// worth the trip, and this pass never banked it, though <c>Banker.Deposit</c> needs no counter — so the porter's own
    /// test kept offering a pass that could not do the one thing it had counted. Coin gone, the porter's count is honest.
    /// </para>
    /// </summary>
    private BotDoing StandStill(IBotWilful bot, Mobile body, bool counterKnown)
    {
        if (Refilling(body, out var lighter))
        {
            // A pass refused for coming too soon after passes that were each lighter is not a pass that got nowhere.
            if (lighter)
            {
                _noLighter.Remove(body.Serial);
            }
            else
            {
                NoLighter(bot, body);
            }

            return BotDoing.Failed(Treadmilled(body, lighter));
        }

        // Counted where it always was: a pass run with no counter known, not a refusal (12.10.2026, Fable's review).
        if (!counterKnown)
        {
            Stranded++;
        }

        var before = BotLadder.Load(body);
        var joined = Gather(body, true);
        var banked = 0;
        var shed = 0;
        var dumped = 0;
        var released = 0;
        var stocked = 0;

        // <b>Not a red's coin (12.10.2026, Fable's review).</b> A red's purse cannot reach a counter and goes to its hideout's
        // chest, and a catch takes everything off it (BotStash); banking it from where it stands would step round that rule.
        // A red over its ceiling goes on to the box and the ground like anybody else.
        if (BotLadder.Overloaded(body) && !BotOutlaw.IsRed(body))
        {
            banked = Bank(body);
            StoodBanked += banked;
        }

        if (BotLadder.Overloaded(body))
        {
            shed = Store(bot, body, true);
            Shed += shed;
        }

        // Raw surplus first, least worth a stone first; the bot's own wares wait (12.10.2026, build 434's check). See Ware.
        if (BotLadder.Overloaded(body))
        {
            dumped = Dump(bot, body, false);
            Dumped += dumped;
        }

        if (BotLadder.Overloaded(body) && Releases)
        {
            released = Release(bot, body);
        }

        // The very last step: its own wares, when nothing else is left to put down, counted apart and said with their worth.
        if (BotLadder.Overloaded(body))
        {
            stocked = Dump(bot, body, true);
            Dumped += stocked;
        }

        _banked = banked;
        _stored = shed;

        var after = BotLadder.Load(body);
        var ceiling = BotLadder.Ceiling(body);
        var did = $"{joined} piles joined, {banked}gp banked, {shed} lots boxed, {dumped} surplus things left on the ground, "
                  + $"{released} kept things let go, {stocked} lots of its own wares left on the ground";

        if (after <= ceiling)
        {
            _noLighter.Remove(body.Serial);

            return BotDoing.Done($"can walk again at {after} of {ceiling} stones, from {before}, without a step: {did}");
        }

        if (after < before)
        {
            // Lighter is not a pass that got nowhere: the breaker's run starts again (12.10.2026, Fable's review).
            _noLighter.Remove(body.Serial);

            return BotDoing.Done($"{before - after} stones lighter from where it stood ({did}), still over at {after} of {ceiling}");
        }

        // Nothing on it that the pass may part it from: bound, worn, or kept with Releases off. Said once by the breaker, loudly,
        // rather than four times a minute. See WedgedMs and StuckFails.
        Immovable++;
        _wedged[body.Serial] = Core.TickCount;
        NoLighter(bot, body);

        return BotDoing.Failed(
            $"too heavy to walk at {after} of {ceiling} stones, and nothing on it could be joined, banked, boxed or put down; "
            + $"the heaviest: {Heaviest(bot, body, 3)}"
        );
    }

    /// <summary>
    /// Joins the piles of one kind lying apart in this bot's pack, top level only, onto the first pile of their kind, look,
    /// colour, weight and loot type, by the engine's own <c>StackWith</c>: nothing made, nothing lost. <paramref name="force"/>
    /// for a bot over its ceiling or at a counter; otherwise once in <see cref="JoinMs"/>. Returns the piles joined.
    ///
    /// <para>
    /// <b>The engine weighs a pile, not a thing, and rounds every pile up to a whole stone (12.10.2026).</b> <c>PileWeight</c> is
    /// ceil(weight x amount), and <c>Container.DropItem</c> — the way 31 places in this project put things in a pack — never
    /// stacks. So a missed arrow picked up off the ground is a pile of one weighing a stone where ten arrows in a pile weigh one,
    /// and a few coins off a corpse are a stone where fifty in a pile are one. Baern carried 177 arrows in that state: kept
    /// arrows in the census fell by 187 stones at 11:11 on 12.10.2026, the minute he died and his death put them in one pile
    /// of 150 (noise between censuses ±20); and his gold paid 468gp and 586gp for lessons and came off 16 and 17 stones,
    /// where one pile would have been 9 and 12.
    /// </para>
    ///
    /// <para>
    /// The weight and the loot type are part of the kind on purpose: a quiver's granted arrows are weightless and kept through
    /// death (<see cref="BotBinding.TrimAmmunition"/>), and joining picked-up arrows onto them would make the extra arrows
    /// weightless too, which binding never promised.
    /// </para>
    /// </summary>
    public static int Gather(Mobile body, bool force)
    {
        var pack = body?.Backpack;

        if (pack == null || pack.Deleted || pack.Items.Count < 2)
        {
            return 0;
        }

        if (!force)
        {
            var now = Core.TickCount;

            if (_gatheredAt.TryGetValue(body.Serial, out var at) && now - at < JoinMs)
            {
                return 0;
            }

            if (_gatheredAt.Count > 4096)
            {
                _gatheredAt.Clear();
            }

            _gatheredAt[body.Serial] = now;
        }

        var bond = (body as BotMobile)?.Bond;
        var before = body.TotalWeight;
        Dictionary<(Type, int, int, double, LootType), Item> first = [];

        // A snapshot: joining deletes piles out of the list being read.
        List<Item> items = [.. pack.Items];
        var joined = 0;

        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];

            if (item == null || item.Deleted || !item.Stackable || !item.Movable || item is Container || item.Parent != pack
                || BotBinding.IsBound(item, bond))
            {
                continue;
            }

            var key = (item.GetType(), item.ItemID, item.Hue, item.Weight, item.LootType);

            if (!first.TryGetValue(key, out var pile) || pile.Deleted || pile.Parent != pack)
            {
                first[key] = item;

                continue;
            }

            if (pile.StackWith(body, item, false))
            {
                joined++;

                continue;
            }

            // The first pile is at the engine's sixty thousand, or named apart: this one becomes the pile to join.
            first[key] = item;
        }

        if (joined > 0)
        {
            Joined += joined;
            JoinedPacks++;
            JoinedStones += Math.Max(0, before - body.TotalWeight);
        }

        return joined;
    }

    /// <summary>
    /// Puts <paramref name="item"/> in <paramref name="pack"/> onto a pile of its kind, weight and loot type if the pack holds
    /// one, beside them otherwise: <c>DropItem</c> with the stacking a player's drop has. For a thing picked up a few at a time,
    /// whose piles would otherwise each weigh a whole stone. See <see cref="Gather"/>.
    /// </summary>
    public static void IntoPack(Mobile body, Container pack, Item item)
    {
        if (pack == null || item == null || item.Deleted)
        {
            return;
        }

        if (item.Stackable)
        {
            var bond = (body as BotMobile)?.Bond;
            var items = pack.Items;

            for (var i = 0; i < items.Count; i++)
            {
                var pile = items[i];

                if (pile is { Deleted: false } && pile is not Container && pile != item && pile.Weight == item.Weight
                    && pile.LootType == item.LootType && !BotBinding.IsBound(pile, bond) && pile.StackWith(body, item, false))
                {
                    return;
                }
            }
        }

        pack.DropItem(item);
    }

    /// <summary>
    /// Lets go of the kept things of a bot that is still over its ceiling with nothing else left to part with, least worth a
    /// stone first, into its bank box, or onto the ground when the box will not take them, until it can walk. Says so first, as
    /// a warning naming what it carries. Returns the things let go. See <see cref="Releases"/>.
    /// </summary>
    private static int Release(IBotWilful bot, Mobile body)
    {
        var pack = body?.Backpack;

        if (pack == null)
        {
            return 0;
        }

        // What is kept, read the way the sale reads it: surplus still here is the bot's own wares, which the last step of the
        // pass puts down only if it must (12.10.2026, build 434's check). See Dump.
        var keep = Needed(bot);
        Dictionary<Type, int> seen = [];
        List<Item> kept = [];

        for (var i = 0; i < pack.Items.Count; i++)
        {
            var item = pack.Items[i];

            if (item == null || item.Deleted || !item.Movable || item is Gold || BotBinding.IsBound(item, bot.Bond))
            {
                continue;
            }

            if (Over(keep, seen, item) > 0)
            {
                continue;
            }

            // <b>Nor the quiver (12.10.2026, both of Fable's reviews).</b> A granted stack is bound by count, not by serial
            // (BotBinding.BindStack records bond.Ammunition, only Bind fills bond.Items), so IsBound does not see it. It weighs
            // nothing, sorted last and was reached only by a bot still over on what it wears, which it could not relieve; and
            // in the box it was gone for good, since TrimAmmunition counts pack and corpse. Nothing of a count-bound kind goes,
            // and nothing that weighs nothing.
            if (Stones(item) <= 0 || BotBinding.BoundCount(item.GetType(), bot.Bond) > 0)
            {
                continue;
            }

            kept.Add(item);
        }

        if (kept.Count == 0)
        {
            return 0;
        }

        KitOver++;

        logger.Warning(
            "{Name} is over its ceiling at {Load} of {Ceiling} stones on what it is allowed to keep, which is a kit it cannot carry: {Heaviest}; its kept things go to its bank box, or the ground, least worth a stone first, until it can walk",
            body.Name,
            BotLadder.Load(body),
            BotLadder.Ceiling(body),
            Heaviest(bot, body, Census)
        );

        // Least worth a stone first, as the ground drop goes, and between equals the heavier: a crafter's twenty logs before its
        // tools and draughts (12.10.2026, build 434's check). See Dump.
        kept.Sort(
            (a, b) =>
            {
                var order = WorthPerStone(a).CompareTo(WorthPerStone(b));

                return order != 0 ? order : Stones(b).CompareTo(Stones(a));
            }
        );

        var box = body.BankBox;
        var put = 0;

        for (var i = 0; i < kept.Count; i++)
        {
            if (!BotLadder.Overloaded(body))
            {
                break;
            }

            var item = kept[i];

            // Never what weighs nothing (filtered above; kept here as the guard): putting it down helps nothing.
            if (Stones(item) <= 0)
            {
                break;
            }

            var kind = item.GetType();
            var units = Math.Max(1, item.Amount);

            // Its own counter, not Stowed: the Needs line's "lots of things went into bank boxes" is Store's (Fable's review).
            if (box is { Deleted: false } && box.TryDropItem(body, item, false))
            {
                ReleasedBoxed++;
                put++;

                continue;
            }

            var worth = (long)units * BotMarket.Worth(kind, 1);

            item.MoveToWorld(body.Location, body.Map);
            ReleasedGround++;
            GroundWorth += worth;
            put++;

            logger.Warning(
                "{Name} put its own {Units} {Kind} on the ground at {X},{Y}, worth about {Worth}gp: kept things it could not carry and its bank box would not take",
                body.Name,
                units,
                kind.Name,
                body.X,
                body.Y,
                worth
            );
        }

        Released += put;

        return put;
    }

    /// <summary>
    /// The heaviest things a bot carries, for a warning: the pack's by kind, with the piles they lie in and whether bound, and
    /// what it wears, each in stones. Coin and bound things included, because those are what every other reading of a pack
    /// leaves out, and a bot nobody could see the load of is how 35 hours went by.
    /// </summary>
    private static string Heaviest(IBotWilful bot, Mobile body, int many)
    {
        Dictionary<Type, (int Units, int Piles, int Stones, bool Bound)> kinds = [];
        var pack = body.Backpack;

        if (pack != null)
        {
            for (var i = 0; i < pack.Items.Count; i++)
            {
                var item = pack.Items[i];

                if (item == null || item.Deleted)
                {
                    continue;
                }

                var kind = item.GetType();
                var had = kinds.TryGetValue(kind, out var was) ? was : default;

                // Bound by serial, or a count-bound kind (the quiver: BotBinding.BindStack), which IsBound does not see.
                kinds[kind] = (had.Units + Math.Max(1, item.Amount), had.Piles + 1, had.Stones + Stones(item),
                    had.Bound || BotBinding.IsBound(item, bot?.Bond) || BotBinding.BoundCount(kind, bot?.Bond) > 0);
            }
        }

        List<(string Said, int Stones)> heavy = [];

        foreach (var (kind, (units, piles, stones, bound)) in kinds)
        {
            var lying = piles > 1 ? $" in {piles} piles" : "";
            var tied = bound ? ", bound" : "";

            heavy.Add(($"{kind.Name} x{units}{lying}{tied}", stones));
        }

        var worn = body.Items;

        for (var i = 0; i < worn.Count; i++)
        {
            var item = worn[i];

            if (item == null || item.Deleted || item.IsVirtualItem || item.Layer == Layer.Backpack || item is Container)
            {
                continue;
            }

            var stones = Stones(item);

            if (stones > 0)
            {
                heavy.Add(($"{item.GetType().Name} worn{(BotBinding.IsBound(item, bot?.Bond) ? ", bound" : "")}", stones));
            }
        }

        if (heavy.Count == 0)
        {
            return "nothing that weighs";
        }

        heavy.Sort((a, b) => b.Stones.CompareTo(a.Stones));

        List<string> named = [];

        for (var i = 0; i < heavy.Count && i < many; i++)
        {
            named.Add($"{heavy[i].Said} {heavy[i].Stones} stones");
        }

        return string.Join(", ", named);
    }

    /// <summary>
    /// Counts one stand-still pass that left the bot no lighter; the <see cref="StuckFails"/>th inside
    /// <see cref="StuckWindowMs"/> sets the bot aside at the weight it is (see <see cref="SetAside"/>) and says so once, loudly.
    /// </summary>
    private static void NoLighter(IBotWilful bot, Mobile body)
    {
        var now = Core.TickCount;
        var load = BotLadder.Load(body);
        var since = now;
        var count = 1;

        // The run is of passes that got nowhere from one load: a load that changed in between, either way, starts it again —
        // the same rule that lifts a bot the breaker set aside (12.10.2026, Fable's review). See SetAside.
        if (_noLighter.TryGetValue(body.Serial, out var was) && now - was.Since < StuckWindowMs && was.Load == load)
        {
            since = was.Since;
            count = was.Count + 1;
        }

        if (count < StuckFails)
        {
            if (_noLighter.Count > 4096)
            {
                _noLighter.Clear();
            }

            _noLighter[body.Serial] = (since, count, load);

            return;
        }

        _noLighter.Remove(body.Serial);

        if (_stuckAt.Count > 4096)
        {
            _stuckAt.Clear();
        }

        _stuckAt[body.Serial] = load;
        StuckTrips++;

        logger.Warning(
            "{Name} got no lighter in {Count} stand-still passes in {Minutes} minutes at {Load} of {Ceiling} stones and is not offered the pass again until its weight changes; it carries {Heaviest}",
            body.Name,
            count,
            (now - since) / 60000,
            load,
            BotLadder.Ceiling(body),
            Heaviest(bot, body, Census)
        );
    }

    /// <summary>
    /// Whether the breaker has set this bot aside and its weight is what it was then, still over the ceiling: the pass would
    /// fail again from here. Any change of weight, either way, lifts it, because a different load is a different pass.
    /// </summary>
    public static bool SetAside(Mobile body)
    {
        if (body == null || !_stuckAt.TryGetValue(body.Serial, out var load))
        {
            return false;
        }

        if (BotLadder.Load(body) == load && BotLadder.Overloaded(body))
        {
            return true;
        }

        _stuckAt.Remove(body.Serial);

        return false;
    }

    /// <summary>Counts one offer of the pass withheld from a bot the breaker had set aside.</summary>
    public static void Withhold() => StuckWithheld++;

    /// <summary>
    /// Lots of a bot's own wares — what it made, or what a want near waits for — that the ground drop passed over while it put
    /// raw surplus down instead (12.10.2026, build 434's check). See <see cref="Ware"/>.
    /// </summary>
    public static long StockSpared { get; private set; }

    /// <summary>Lots of a bot's own wares put on the ground as the very last step, nothing else being left to put down.</summary>
    public static long StockGround { get; private set; }

    /// <summary>What those wares were worth at the market's reckoning.</summary>
    public static long StockGroundWorth { get; private set; }

    /// <summary>
    /// Whether this surplus lot is one of the bot's wares rather than raw surplus: something the engine's craft system made
    /// (<c>PlayerConstructed</c>, stamped on every crafted item, and kept only while a pile holds nothing but crafted things),
    /// or something a want within a carrier's reach waits for (<see cref="BotMarket.Holding"/>). A ware is what the bot's
    /// shop by the bank and the carriers live on; raw surplus is what a craft or a harvest left over.
    ///
    /// <para>
    /// <b>Build 434, 22:14:41 on 12.10.2026:</b> Yorick Ashdown (Crafter) was 3 stones over at 250 of 247 with 105 arrows he
    /// had fletched to sell, 20 logs and 20 leather he had just bought; his bank box was full, so nothing was boxed, and the
    /// drop, heaviest first, put the arrows on the road (about 105gp) where 3 logs would have done. At 22:26 his shop by the
    /// bank sold 290 things out of that box, and from 22:31 it took his lots again.
    /// </para>
    /// </summary>
    private static bool Ware(IBotWilful bot, Item item, int over, Dictionary<Type, int> held) =>
        item.PlayerConstructed || BotMarket.Holding(bot, item.GetType(), over, held) > 0;

    /// <summary>
    /// Gold a unit of this lot is worth for each stone it weighs: the order the ground takes things in, least first. A thing
    /// that weighs nothing is worth infinitely much a stone, which is the same as never, since putting it down helps nothing.
    /// A container goes after every other thing that weighs: its own price says nothing of what is in it.
    /// </summary>
    private static double WorthPerStone(Item item)
    {
        var weight = item.Weight;

        if (weight <= 0.0 && item.TotalWeight <= 0)
        {
            return double.MaxValue;
        }

        return item is Container ? double.MaxValue / 2 : BotMarket.Worth(item.GetType(), 1) / Math.Max(weight, 0.01);
    }

    /// <summary>
    /// Puts surplus on the ground until the bot can walk again, least worth a stone first: raw surplus, and with
    /// <paramref name="wares"/> the bot's own wares (<see cref="Ware"/>), which only the very last step of the pass touches.
    ///
    /// <para>
    /// <b>The last resort for what the market would take, and it only ever runs for a bot that is over its carrying ceiling
    /// after the box has had its turn.</b> Everything it may drop is what the sale would have sold, and only the part of a
    /// stack over the allowance: the kit stays, whatever the trade is bound to stays, coin stays. It is logged with its worth.
    /// </para>
    ///
    /// <para>
    /// <b>Least worth a stone first, and the wares apart (12.10.2026, build 434's check).</b> The first cut of this pass went
    /// heaviest first, which put Yorick Ashdown's 105 fletched arrows (about 105gp, 11 stones) on the road ahead of his logs
    /// and leather (a gold or less for two stones) — a crafter's stock thrown away while its raw surplus stayed. Before that,
    /// it sorted by <c>TotalWeight</c>, nought for anything that is not a container. Raw surplus by worth a stone puts the
    /// fewest coins on the ground for each stone of relief; the wares wait for the box, and go down only when nothing else is
    /// left (<see cref="StockGround"/>), counted apart and said with their worth.
    /// </para>
    ///
    /// <para>
    /// Stops the moment the bot is under the ceiling rather than emptying the pack: this is a cure for being
    /// stuck, not a policy about carrying things.
    /// </para>
    /// </summary>
    private static int Dump(IBotWilful bot, Mobile body, bool wares)
    {
        var pack = body?.Backpack;

        if (pack == null)
        {
            return 0;
        }

        var keep = Needed(bot);
        Dictionary<Type, int> seen = [];
        Dictionary<Type, int> held = [];
        List<(Item Item, int Over, double PerStone)> loose = [];
        var spared = 0;

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

            var over = Over(keep, seen, item);

            if (over <= 0)
            {
                continue;
            }

            if (Ware(bot, item, over, held) != wares)
            {
                spared += wares ? 0 : 1;

                continue;
            }

            loose.Add((item, over, WorthPerStone(item)));
        }

        // Least worth a stone first; between equals, the heavier lot, so the fewest things go down for the most relief.
        loose.Sort(
            (a, b) =>
            {
                var order = a.PerStone.CompareTo(b.PerStone);

                return order != 0 ? order : Stones(b.Item).CompareTo(Stones(a.Item));
            }
        );

        var put = 0;
        var worth = 0L;
        List<string> what = [];

        for (var i = 0; i < loose.Count; i++)
        {
            if (BotLadder.Load(body) <= BotLadder.Ceiling(body))
            {
                break;
            }

            var (lot, over, perStone) = loose[i];

            // A thing that weighs nothing: nothing after it in this order can help either.
            if (perStone >= double.MaxValue)
            {
                break;
            }

            if (lot.Deleted || lot.Parent != pack)
            {
                continue;
            }

            // The part over the allowance only, split off the way the store splits it: the original keeps the amount asked for
            // and the remainder is put back beside it. A type with no parameterless constructor is left whole in the pack.
            if (over < Math.Max(1, lot.Amount) && Mobile.LiftItemDupe(lot, over) == null)
            {
                continue;
            }

            var kind = lot.GetType();
            var units = Math.Max(1, lot.Amount);

            // A capped kind's surplus on the ground is counted by kind with the other routes. See BotSurplus.
            if (keep.TryGetValue(kind, out var cap) && cap > 0 && cap < int.MaxValue)
            {
                BotSurplus.Count(BotSurplus.Ground, kind, units);
            }

            worth += (long)units * BotMarket.Worth(kind, 1);
            what.Add($"{units} {kind.Name}");

            lot.MoveToWorld(body.Location, body.Map);
            put++;
        }

        if (!wares)
        {
            StockSpared += spared;
        }

        if (put > 0)
        {
            GroundWorth += worth;

            if (wares)
            {
                StockGround += put;
                StockGroundWorth += worth;

                logger.Warning(
                    "{Name} could neither walk nor sell, had nothing left to put down but its own wares, and has put {Put} lots of them on the ground at {X},{Y}, worth about {Worth}gp: {What}",
                    body.Name,
                    put,
                    body.X,
                    body.Y,
                    worth,
                    string.Join(", ", what)
                );
            }
            else
            {
                logger.Warning(
                    "{Name} could neither walk nor sell, and has put {Put} things on the ground at {X},{Y} to move again, worth about {Worth}gp, least worth a stone first ({Spared} lots of its own wares kept): {What}",
                    body.Name,
                    put,
                    body.X,
                    body.Y,
                    worth,
                    spared,
                    string.Join(", ", what)
                );
            }
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

        // Piles lying apart, and the bots over their ceiling (12.10.2026). See Loose and Ceilinged.
        Dictionary<(Type, int, int, double, LootType), (int Piles, int Units, int Stones)> piles = [];
        Dictionary<Type, int> looseKinds = [];
        var loosePiles = 0;
        var looseStones = 0;
        var loosePacks = 0;
        var overNow = 0;
        var stuckNow = 0;
        List<string> stuckNames = [];
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

            // Over the ceiling now, and set aside by the breaker (12.10.2026). See StandStill and SetAside.
            if (BotLadder.Overloaded(body))
            {
                overNow++;

                if (SetAside(body))
                {
                    stuckNow++;
                    stuckNames.Add($"{body.Name} {BotLadder.Load(body)}/{BotLadder.Ceiling(body)}");
                }
            }

            piles.Clear();

            for (var j = 0; j < pack.Items.Count; j++)
            {
                var item = pack.Items[j];

                if (item == null || item.Deleted)
                {
                    continue;
                }

                // Piles of one kind lying apart, coin and bound ammunition included: what each pile weighs is rounded up to a
                // whole stone, so the census of the piles is the only reading of what that costs. See Gather.
                if (item.Stackable && item is not Container)
                {
                    var key = (item.GetType(), item.ItemID, item.Hue, item.Weight, item.LootType);
                    var heap = piles.TryGetValue(key, out var p) ? p : default;

                    piles[key] = (heap.Piles + 1, heap.Units + Math.Max(1, item.Amount), heap.Stones + item.PileWeight);
                }

                if (item is Gold || BotBinding.IsBound(item, body.Bond))
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

            var apart = false;

            foreach (var ((kind, _, _, each, _), (count, amount, weighs)) in piles)
            {
                if (count < 2)
                {
                    continue;
                }

                var extra = weighs - (int)Math.Ceiling(each * amount);

                apart = true;
                loosePiles += count - 1;
                looseStones += extra;
                looseKinds[kind] = looseKinds.GetValueOrDefault(kind) + extra;
            }

            if (apart)
            {
                loosePacks++;
            }
        }

        StuckNow = stuckNow;

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
               + $"keep ({surplus / Math.Max(1.0, carried) * 100.0:F0}%); heaviest: {string.Join(", ", named)}; {capped}; "
               + $"{Loose(loosePiles, loosePacks, looseStones, looseKinds)}; {Ceilinged(overNow, stuckNow, stuckNames)}";
    }

    /// <summary>
    /// The clause for piles of one kind lying apart in packs (12.10.2026): how many piles more than one of each kind there are,
    /// in how many packs, and the stones they weigh over the same things in one pile, by kind. The figure that must fall.
    /// See <see cref="Gather"/>.
    /// </summary>
    private static string Loose(int piles, int packs, int stones, Dictionary<Type, int> byKind)
    {
        if (piles == 0)
        {
            return "no pack holds piles of one kind lying apart";
        }

        List<(Type Kind, int Stones)> kinds = [];

        foreach (var (kind, extra) in byKind)
        {
            kinds.Add((kind, extra));
        }

        kinds.Sort((a, b) => b.Stones.CompareTo(a.Stones));

        List<string> named = [];

        for (var i = 0; i < kinds.Count && i < Census; i++)
        {
            named.Add($"{kinds[i].Kind.Name} {kinds[i].Stones}");
        }

        return $"loose piles: {piles} piles lying apart in {packs} packs weigh {stones} stones over the same things in one pile "
               + $"({string.Join(", ", named)}); {Joined} piles joined in {JoinedPacks} packs so far, {JoinedStones} stones off";
    }

    /// <summary>
    /// The clause for bots over their ceiling (12.10.2026): how many are now, how many of those the breaker has set aside —
    /// "stuck over the ceiling: N bots", by name and load, for the alarm to read — and what the stand-still pass has done.
    /// </summary>
    private static string Ceilinged(int over, int stuck, List<string> names)
    {
        var who = stuck > 0 ? $" ({string.Join(", ", names)})" : "";

        return $"over the ceiling now: {over} bots, stuck over the ceiling: {stuck} bots{who}; the stand-still pass: "
               + $"{StoodBanked}gp banked from where a bot stood, {KitOver} packs over on their kept things alone ({Released} kept "
               + $"things let go, {ReleasedBoxed} to the box and {ReleasedGround} to the ground), {GroundWorth}gp of things left on the "
               + $"ground; wares: {StockSpared} lots spared by the ground drop, {StockGround} lots put down last ({StockGroundWorth}gp); "
               + $"{StuckTrips} bots set aside by the breaker ({StuckFails} passes no lighter in {StuckWindowMs / 60000} "
               + $"minutes), {StuckWithheld} offers withheld (one a review)";
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

        // Not to a bot the breaker has set aside: StuckFails stand-still passes in StuckWindowMs left it no lighter, and the
        // pass is not offered again until its weight changes (12.10.2026). Asked first, before any join or walk of the pack:
        // a set-aside bot's piles were joined by its own passes, and anything new it picks up changes its weight and lifts it
        // (Fable's review). See BotUnload.SetAside.
        if (laden && BotUnload.SetAside(body))
        {
            BotUnload.Withhold();

            return null;
        }

        // <b>Piles of one kind lying apart are joined before anything is weighed (12.10.2026).</b> Each weighs a whole stone
        // however little is in it, so a pack can read heavy, or over the ceiling, on piles alone: Baern's 177 arrows weighed
        // 177 stones. Joining needs no errand and no step. Once a minute for a bot that can walk, every look for one that
        // cannot. See BotUnload.Gather.
        if (laden && BotUnload.Gather(body, BotLadder.Overloaded(body)) > 0)
        {
            laden = BotLadder.Load(body) >= BotLadder.Ceiling(body) * BotUnload.Heavy;
        }

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
