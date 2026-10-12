using System;
using System.Collections.Generic;
using Server.Items;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// A crafter short of the raw material of its trade, putting the order to the population.
///
/// <para>
/// <b>Two materials, and they are the two that no counter on this island sells at any price.</b> A feather
/// exists because somebody killed a bird; a hide exists because somebody killed a beast. Everything else a
/// crafter eats — cloth, logs, blank scrolls, reagents — is on a shelf, so a bot short of those has an
/// errand rather than a problem. Metal is the third of this kind and has had its own since 24.08.2026; see
/// <see cref="BotBullion"/>, which this is deliberately a copy of. The smith's question is <em>which</em>
/// metal and needs a file of its own; these two only ask whether.
/// </para>
///
/// <para>
/// <b>Without this the arrow chain has no seed and cannot start at all.</b> The links were all built and
/// every one of them was waiting on the one before it: a fletcher will not work without feathers, a hunter
/// values a bird only when the board is asking for what it carries — <see cref="BotQuarry.Sought"/> — and
/// the board only ever asked for arrows when nobody sold one, which is never, because every bowyer in
/// Britain keeps a stack. So the chain was a ring: 84 fletchers passed over for want of feathers in eleven
/// minutes, 194 woodcutters told nobody was asking, and in seven hours of logs the word "feather" does not
/// appear once. A crafter that can say "I need feathers" out loud is the cut in that ring.
/// </para>
///
/// <para>
/// <b>On the beat and not in the auction, and that is the difference between working and not.</b> Written
/// first as a proposer, it made the offer 62 times in six minutes and was chosen none of them: an order is
/// half a minute of paperwork worth perhaps twenty gold a minute, and it stood against a tailor's own bench
/// at three hundred and forty. That is the auction being right — Calla should sew — and it is also the
/// reason the trade she is sewing for can never be supplied. The same lesson is written out in
/// <see cref="BotUpkeep"/> about collecting goods already paid for, and in <see cref="BotOrder"/> in as many
/// words: <em>an errand that costs nothing and takes no time cannot win an auction against work that pays,
/// and it never did.</em> This lives on the population's beat for that reason.
/// </para>
///
/// <para>
/// <b>Logs were left out on purpose and the measurement took it back.</b> The reasoning was that a carpenter
/// sells them, so a fletcher short of wood has a shopping trip rather than a problem. It was wrong about this
/// island: once the feathers began arriving, 94 of 138 fletchers were passed over at 11:31 on 04.09.2026 with
/// "could not find wood" — <c>BotShops.Nearest(bot, typeof(Log))</c> answering null, because no carpenter
/// stands within any of their reach. So wood is the third thing nobody sells here, and it is asked for the
/// same way. It also opens the one trade on the shard that has never done a day's work: 323 woodcutters in
/// the same half hour, every one of them told that nobody was asking for wood or arrows.
/// </para>
///
/// <para>
/// Feathers first and wood second, for a fletcher that carries both gates. A fletcher with wood and no
/// feathers has nothing to make; a fletcher with feathers and no wood is one errand from working.
/// </para>
/// </summary>
public static class BotStores
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotStores));

    /// <summary>Coin a crafter keeps back rather than putting into materials: <see cref="BotPurse.KeepBack"/> unless dialled apart. It was 150.</summary>
    public static int Reserve
    {
        get => _reserve ?? BotPurse.KeepBack;
        set => _reserve = value;
    }

    private static int? _reserve;

    /// <summary>
    /// How much of a material is enough to be working, below which more is worth ordering.
    ///
    /// <para>
    /// Twenty, which is what the trades already mean by a working quantity: a bolt of cloth is twenty, the
    /// smith's batch is twenty, and the leather in a chest piece is between eight and sixteen. For feathers
    /// it is also very close to a quiver — the fletcher makes arrows in tens.
    /// </para>
    /// </summary>
    public static int Enough { get; set; } = 20;

    /// <summary>How many are ordered at a time when the purse allows it.</summary>
    public static int Batch { get; set; } = 20;

    /// <summary>
    /// How many raw ribs a cook orders at a time. Five (03.10.2026, build next): what one carrier hands over now, and fifty
    /// minutes of meals.
    ///
    /// <para>
    /// It was ten (02.10.2026): what one carrier brought a cook on build 392 (389 ribs in 41 deliveries, 9.5 each), when the
    /// traders' books still carried ribs from the butcher. With the material's ceiling the butcher is no source, and a rib
    /// comes from a carrier that carved it: on live 395 (03.10.2026, 03:08–03:35) carriers handed 134 ribs in 30 hand-overs,
    /// 4.5 each, and the 56 rib lines said after the start asked 521 units and were handed 58. A meal lasts ten minutes
    /// (<see cref="BotMeal.LastsMs"/>), so ten ribs are a hundred minutes of suppers — more than a line's hour, and two
    /// carriers' walks where one would do. A rib is supper, not a working stock.
    /// </para>
    /// </summary>
    public static int MeatBatch { get; set; } = 5;

    /// <summary>
    /// <b>Raw meat is asked for no faster than the island brings it (03.10.2026, build next).</b> A want for meat is not said while
    /// the units of that meat already asked for across the board would pass <see cref="StockShare"/> of what carriers, traders and
    /// shops handed over against it in the last hour (<see cref="BotMarket.Brought"/>), or <see cref="StockFloor"/> when that is
    /// more. A cook's supper is a stock, not a need: on live 395 the rib wants said after the start asked 521 units in 27 minutes,
    /// about 1,150 an hour, against 380 an hour carried to every rib want on the island — two lines in three could not be met in
    /// their hour by anybody, and every one of them was a line said. The bot asks again a minute later (<see cref="BotNeeds"/>),
    /// and is heard once the board has room.
    ///
    /// <para>
    /// <b>Half an hour of what the island brings, not the whole hour.</b> What stands on the board is a queue, and a queue an hour
    /// long fails its own last line. The gate binds only while the board waits longer than that for a carrier: a line met in a
    /// quarter of an hour keeps the board short and the gate open, so it cannot strangle the trade it measures (what the board is
    /// brought rises with what it asks, until carriers run out).
    /// </para>
    /// </summary>
    public static bool SupplyGated { get; set; } = true;

    /// <summary>The share of the last hour's deliveries of a meat the board may stand asking for. A half: a half-hour queue.</summary>
    public static double StockShare { get; set; } = 0.5;

    /// <summary>The units of a meat the board may ask for whatever the last hour brought: a start, or a quiet hour, is not a veto.</summary>
    public static int StockFloor { get; set; } = 30;

    /// <summary>
    /// The units of a meat the board may stand asking for now. See <see cref="SupplyGated"/>.
    ///
    /// <para>
    /// <b>Sized to the pace, not to the hour's sum (06.10.2026, ribs17).</b> It read <see cref="BotMarket.Brought"/>, the last hour's
    /// hand-overs, which follows a fall an hour late: on build 412 the room was still 573 units at 16:21 and 423 at 16:36 while
    /// carriers brought 90 and then 25 ribs in ten minutes, and the lines said into that room failed at their hour. Now it reads
    /// <see cref="BotMarket.Supply"/>: the lesser of the hour's sum and the last quarter-hour's decaying pace.
    /// </para>
    /// </summary>
    public static int StockRoom(Type kind) => Math.Max(StockFloor, (int)(BotMarket.Supply(kind) * Math.Max(0.0, StockShare)));

    /// <summary>
    /// The fewest raw meat a cook's line asks for when it is sized down (06.10.2026, ribs17): three ribs at the meal's 3–5gp are
    /// 9–15gp, over a carrier's least (8gp), and half an hour of suppers.
    /// </summary>
    public static int MeatLeast { get; set; } = 3;

    /// <summary>
    /// <b>A cook asks for <see cref="MeatLeast"/>, not <see cref="MeatBatch"/>, while the island's pace has fallen under this share of
    /// the hour's sum (06.10.2026, ribs17).</b> Of the rib lines that failed on build 411, 36 of 117 were "partial", 32 of them 2 or
    /// 3 of 5 handed: a five-rib line waits for a second carrier when ribs come in ones and twos, and they come so when the pace
    /// has just fallen. Nought switches it off.
    /// </summary>
    public static double ThinShare { get; set; } = 0.5;

    /// <summary>Whether the island brings a meat at under <see cref="ThinShare"/> of its hour's pace now. See <see cref="ThinShare"/>.</summary>
    public static bool Thin(Type kind) => ThinShare > 0 && BotMarket.Pace(kind) < BotMarket.Brought(kind) * ThinShare;

    /// <summary>Lines for raw meat sized down: to the room left on the board, or to <see cref="MeatLeast"/> for a thin pace; and the units fewer.</summary>
    public static long SizedToRoom { get; private set; }

    public static long SizedThin { get; private set; }

    public static long SizedUnits { get; private set; }

    /// <summary>
    /// <b>A cook's want for ribs is taken back when its own hunting brought what it asked for (06.10.2026, ribs17).</b> A bot asks for
    /// ribs holding fewer than two; the same bot then hunts, carves a cow (8) or a hind (5), and its want stood on: the line waited
    /// for a carrier the bot no longer needed, and the bot's own ribs could not go to anybody else, because a carrier that wants a
    /// kind itself is passed over for every want of it (<c>BotCourier</c>, "wanted by the carrier itself": 21,540 in the first five
    /// minutes of build 412 and 1,202,134 by 17:26). Of the rib lines that failed or were met late, the buyer carved the line's units
    /// itself before the end in 34 of 102 on build 411 and 26 of 81 on build 412 (<c>ribs17-tools/rib_sated.py</c>).
    ///
    /// <para>
    /// Taken back as "had it another way" (<c>BotWithdraw.Have</c>), counted as any taking back is: kept out inside its first ten
    /// minutes, failed after them (<c>BotLines.GraceMs</c>). Nothing about what counts changes. Not while a carrier is bringing
    /// some (<c>BotWant.Coming</c>): that walk is the carrier's and the buyer's bargain. Asked once a minute per bot holding ribs.
    /// </para>
    /// </summary>
    public static bool TakeBackSated { get; set; } = true;

    /// <summary>Raw meat wants taken back because the buyer held what they asked for, and of them inside the line's first ten minutes.</summary>
    public static long Sated { get; private set; }

    public static long SatedEarly { get; private set; }

    public static long SatedUnits { get; private set; }

    private static readonly Dictionary<Serial, long> _satedLook = [];

    /// <summary>See <see cref="TakeBackSated"/>.</summary>
    private static void TakeBack(IBotWilful bot, Mobile body)
    {
        if (!TakeBackSated || BotOven.Kit(body) == null)
        {
            return;
        }

        var ribs = BotOven.Amount(body, typeof(RawRibs));

        if (ribs < BotOven.Worthwhile)
        {
            return;
        }

        var now = Core.TickCount;

        if (_satedLook.TryGetValue(body.Serial, out var last) && now - last < 60000)
        {
            return;
        }

        if (_satedLook.Count > 4096)
        {
            _satedLook.Clear();
        }

        _satedLook[body.Serial] = now;

        var want = BotMarket.Wanted(bot, typeof(RawRibs));

        if (want == null || want.Coming > 0 || ribs < want.Amount)
        {
            return;
        }

        var early = now - want.PostedTick < BotLines.GraceMs;
        var units = BotMarket.Withdrawn(bot, typeof(RawRibs), BotWithdraw.Have);

        if (units <= 0)
        {
            return;
        }

        Sated++;
        SatedUnits += units;

        if (early)
        {
            SatedEarly++;
        }

        logger.Information(
            "{Name} took back its want for {Units} Raw Ribs: it holds {Ribs} of its own, and its ribs are for other buyers now",
            body.Name,
            units,
            ribs
        );
    }

    /// <summary>
    /// Asks for raw meat not said because the board already asked for more than its room (<see cref="StockRoom"/>): every ask, a bot
    /// held back asking again each minute (<see cref="BotNeeds"/>) included.
    /// </summary>
    public static long Outrun { get; private set; }

    /// <summary>
    /// The same counted once a hold: a bot and a kind held back, from the first ask refused until the bot says the want or stops
    /// asking for <see cref="HoldLapseMs"/> — and the units that first ask would have said. What <see cref="Outrun"/> counts sixty
    /// times an hour (review of build next, F2).
    /// </summary>
    public static long OutrunBots { get; private set; }

    public static long OutrunUnits { get; private set; }

    /// <summary>How long after its last refused ask a hold is over: a bot asks once a minute while it wants, so five minutes.</summary>
    public static int HoldLapseMs { get; set; } = 300000;

    private static readonly Dictionary<(Serial Bot, Type Kind), long> _holding = [];

    /// <summary>Bots held back now: refused within <see cref="HoldLapseMs"/>.</summary>
    public static int HeldNow()
    {
        var now = Core.TickCount;
        var held = 0;

        foreach (var (_, tick) in _holding)
        {
            if (now - tick < HoldLapseMs)
            {
                held++;
            }
        }

        return held;
    }

    /// <summary>Asks for raw meat whose offer was brought down to what the meal is worth. See <see cref="BotOven.MealWorth"/>.</summary>
    public static long MealCapped { get; private set; }

    /// <summary>
    /// The fewest worth putting on the board at all.
    ///
    /// Five: enough that filling the order is a real errand rather than handing over a pocketful, and small
    /// enough that a crafter down to its last hundred gold is still in the market. Every multiplier on this
    /// shard needs a floor; without one an empty purse is a veto.
    /// </summary>
    public static int Least { get; set; } = 5;

    /// <summary>What a feather is offered at when nothing on the shard has ever priced one.</summary>
    public static int GuessFeather { get; set; } = 3;

    /// <summary>What a leather is offered at when nothing on the shard has ever priced one.</summary>
    public static int GuessLeather { get; set; } = 6;

    /// <summary>What a log is offered at when nothing on the shard has ever priced one. The carpenter's ask.</summary>
    public static int GuessLog { get; set; } = 3;

    /// <summary>What an empty bottle is offered at when nothing has priced one. The alchemist's own ask.</summary>
    public static int GuessGlass { get; set; } = 5;

    /// <summary>
    /// What a raw rib opens at. Three, which is what the market has actually been settling them at.
    ///
    /// <para>
    /// <b>Meat passes the rule glass failed.</b> Nothing may be ordered by the armful unless somebody's
    /// living is fetching it — that is the lesson the bottle cost this shard four-fifths of its trade for
    /// half an hour. Meat has the best producer on the island: every hunt ends in a carcass, hunting is the
    /// commonest thing anybody does, and carving is already folded into going through a corpse. What was
    /// missing was the ask. The cook could only ever cook what it happened to be holding, so 94% of every
    /// look at the skillet answered "no meat worth cooking" while the hunters who had it walked past.
    /// </para>
    ///
    /// <para>
    /// Ribs rather than the other four: 57 of the 67 raw meats this shard traded in a session were ribs, and
    /// an order for a kind nothing drops is an order that freezes the money behind it.
    /// </para>
    /// </summary>
    public static int GuessMeat { get; set; } = 3;

    /// <summary>What this is reckoned at per minute. Low: it is one beat of paperwork and then waiting.</summary>
    public static double Prior { get; set; } = 20.0;

    private static bool _said;

    /// <summary>
    /// Scratch for the materials one bot is asked about, reused rather than allocated.
    ///
    /// This runs on every bot's beat, and game logic is one thread — see the threading model — so a shared
    /// three-slot buffer is safe and costs nothing. A managed type cannot be stackalloc'd.
    /// </summary>
    private static readonly Type[] _wanted = new Type[5];

    /// <summary>Every gate apart, and the denominator with them. There is no bucket called "other".</summary>
    public static long Asked { get; private set; }

    public static long NoTrade { get; private set; }

    /// <summary>Crafters turned away for coming again inside the minute. Bots, not materials. See BotNeeds.</summary>
    public static long Soon { get; private set; }

    public static long Stocked { get; private set; }

    public static long Shelved { get; private set; }

    public static long Standing { get; private set; }

    public static long Poor { get; private set; }

    public static long Ordered { get; private set; }

    /// <summary>The fattest purse among those that could not afford one. See BotShopper.Richest for why.</summary>
    public static long Richest { get; private set; }

    /// <summary>
    /// Asked of every bot on its own beat. Nearly every answer is "no kit", which costs a null and a return.
    /// </summary>
    public static void Keep(IBotWilful bot)
    {
        var body = bot?.Self;
        var map = body?.Map;

        if (map == null || map == Map.Internal || !body.Alive)
        {
            return;
        }

        // A cook whose own hunting brought what its rib want asks for takes the want back (06.10.2026, ribs17). See TakeBackSated.
        TakeBack(bot, body);

        // Asked of the tool in the pack rather than of the class name, exactly as the smith's is: a bot that
        // picks up a fletching kit tomorrow is a fletcher tomorrow, and this file does not have to hear of it.
        var count = Materials(body, _wanted);

        if (count == 0)
        {
            NoTrade++;

            return;
        }

        // Once a minute per bot. See BotNeeds: this runs on the bot's own beat, five times a second, and
        // whether a fletcher is short of feathers cannot change in a fifth of a second.
        //
        // <b>Below the trade check and not above it, which is where it went first and where the number it
        // produced was nonsense.</b> Above, every bot on the shard is asked on every beat whether it may
        // reconsider — including the forty who have no crafting kit and no business here — so the bucket
        // counted beats rather than asks: 295,278 turned away against 96 asked, at 03:12 on 05.09.2026. A
        // throttle placed before the cheap test it is meant to protect also pays a dictionary lookup for
        // every bot it was never going to help. Here it shares a denominator with the gates below it, which
        // is the only position from which the line reconciles.
        if (!BotNeeds.Due(body, "stores"))
        {
            Soon++;

            return;
        }

        // <b>Every material the trade is short of, and not merely the first one.</b> This asked about one and
        // stopped, so a fletcher holding five feathers with an order already out for more never got as far as
        // the question about wood — and wood was what it was actually standing still for: 135 of 180
        // fletchers "could not find wood" at 12:01 on 04.09.2026 while 9381 answers here were "already have
        // one out". A bot waiting on one delivery is not a bot with nothing else to ask for.
        for (var i = 0; i < count; i++)
        {
            if (Ask(bot, body, _wanted[i]))
            {
                return;
            }
        }
    }

    /// <summary>One material. True when an order went on the board, false when this one had nothing to ask.</summary>
    private static bool Ask(IBotWilful bot, Mobile body, Type kind)
    {
        // <b>Counted here and not once per bot, so the buckets and the denominator are the same question.</b>
        // A fletcher is asked about feathers and about wood on one beat, so a per-bot denominator produced
        // "17401 asked … 31074 already have one out" on 04.09.2026 — a share of more than one, which is not
        // a share of anything. This counts material questions, and every figure below answers one.
        Asked++;

        if ((body.Backpack?.GetAmount(kind) ?? 0) >= Enough)
        {
            Stocked++;

            return false;
        }

        // Its own feathers are feathers. Standing at a bank with the thing in its own box, it takes it out rather than asking the
        // island for it (01.10.2026: the box is reached only at a bank, where the stall it replaces was reached from anywhere).
        if (BotMarket.FromBank(bot, kind, Enough - (body.Backpack?.GetAmount(kind) ?? 0)) > 0)
        {
            Shelved++;

            return false;
        }

        // <b>Asked here and not left to the market, because the market's answer to a want that already
        // stands is to top it up.</b> BotMarket.Ask tops an open want up, and each topping up asks the purse for the whole of it
        // again — so a beat that asked every second would empty a crafter's account into one order for
        // feathers in about a minute. This gate is the whole of what makes it safe to run on a beat.
        if (BotMarket.Wanted(bot, kind) != null)
        {
            Standing++;

            return false;
        }

        var offer = BotMarket.Worth(kind, Guess(kind));

        // <b>The cook's own sum (02.10.2026, build next): raw meat is never offered for more than the meal it makes.</b> See
        // BotOven.MealWorth. The market would clamp it as well; clamped here, the purse is asked for what will really be offered.
        var meal = BotOven.MealWorth(kind);
        var batch = Batch;
        var least = Least;
        var capped = false;
        var thin = false;

        if (meal > 0)
        {
            batch = MeatBatch;

            // Three, not five, while the island's pace has fallen under half its hour (06.10.2026, ribs17). See ThinShare.
            if (SupplyGated && MeatLeast < batch && Thin(kind))
            {
                batch = Math.Max(1, MeatLeast);
                least = Math.Min(least, batch);
                thin = true;
            }

            capped = offer > meal;

            if (capped)
            {
                offer = meal;
            }
        }

        // Everything the bot owns, not what happens to be in the pocket: the deposit is taken by
        // BotMarket.Charge at the meeting, which spends the pack and then reaches into the account.
        var wealth = BotYield.Spendable(body);

        // <b>As many as the purse will carry, down to a floor, rather than a batch of twenty or nothing.</b>
        // A fixed batch against a fixed reserve is a multiplier with no floor, which is this shard's most
        // expensive shape: at 12:01 on 04.09.2026 it read "3478 cannot afford one (the fattest purse among
        // them held 132gp)" — a population of crafters locked out of its own materials by twenty logs at
        // three gold, when five would have kept every one of them working.
        var afford = offer <= 0 ? 0 : (wealth - Reserve) / offer;
        var units = Math.Min(batch, afford);

        if (units < least)
        {
            Poor++;

            if (wealth > Richest)
            {
                Richest = wealth;
            }

            return false;
        }

        // Meat no faster than the island brings it (build next). See SupplyGated. A hold is counted once, however often it asks.
        // A line that would pass the room is first sized down to what is left of it, to MeatLeast (06.10.2026, ribs17).
        var left = meal > 0 && SupplyGated ? StockRoom(kind) - BotMarket.Wanting(kind) : int.MaxValue;
        var asked = units;

        if (units > left && left >= Math.Max(1, MeatLeast))
        {
            units = left;
        }

        if (meal > 0 && SupplyGated && units > left)
        {
            Outrun++;

            var key = (body.Serial, kind);
            var now = Core.TickCount;

            if (!_holding.TryGetValue(key, out var last) || now - last >= HoldLapseMs)
            {
                OutrunBots++;
                OutrunUnits += units;
            }

            if (_holding.Count > 4096)
            {
                _holding.Clear();
            }

            _holding[key] = now;

            return false;
        }

        _holding.Remove((body.Serial, kind));

        // Counted for an ask the market is about to hear, not for every ask a gate above then refused (review of build next, F2).
        if (capped)
        {
            MealCapped++;
        }

        // The market does the rest and says so in the log: it says the want aloud, refuses when the board is
        // full and hands the money straight back, and settles the case of a bot that turns out to be selling
        // the thing it wants. There is nothing here for a bot to stand still for.
        if (BotMarket.Ask(bot, kind, units, offer) == null)
        {
            // The board was full, or the market settled it another way. Counted with the ones already
            // waiting, because from the trade's side the state is the same: the material is asked for and
            // this bot is waiting. Inventing a bucket for it would be inventing a bucket called "other".
            Standing++;

            return false;
        }

        Ordered++;
        Once(body, kind);

        // Lines said smaller than the cook's batch, counted only once the market heard them (06.10.2026, ribs17).
        if (units < asked)
        {
            SizedToRoom++;
        }

        if (thin)
        {
            SizedThin++;
        }

        if (meal > 0 && units < MeatBatch && (thin || units < asked))
        {
            SizedUnits += MeatBatch - units;
        }

        return true;
    }

    /// <summary>
    /// The one material this bot's trade eats and nobody sells, or null.
    ///
    /// <para>
    /// A bot may carry both kits; the fletcher is asked first because feathers are the scarcer half by a long
    /// way — hide comes off nearly everything the population kills and a feather off almost nothing.
    /// </para>
    /// </summary>
    private static int Materials(Mobile body, Type[] into)
    {
        var count = 0;

        if (BotFletching.Kit(body) != null)
        {
            // Feathers first: they are the half nobody sells and the binding one. Wood second, because a
            // fletcher with feathers and no wood is one delivery away from working.
            into[count++] = typeof(Feather);

            // Shafts count as wood: they are what a log becomes, and a fletcher holding twenty of them is
            // not short of anything. Asking otherwise would order wood on top of wood already cut.
            if (BotFletching.Logs(body) + BotFletching.Shafts(body) < Enough)
            {
                into[count++] = typeof(Log);
            }
        }

        if (BotThread.Kit(body) != null)
        {
            into[count++] = typeof(Leather);
        }

        // The cook, last, because it is the newest of these and the cheapest to go without. A funded want for
        // ribs is also the only way a hunter is ever paid for meat — and the board already steers a kill by
        // what the carcass carries, so the ask does two things at once.
        if (BotOven.Kit(body) != null && BotOven.Amount(body, typeof(RawRibs)) < BotOven.Worthwhile)
        {
            into[count++] = typeof(RawRibs);
        }

        // <b>Glass was added here on 04.09.2026 and taken out again half an hour later, and the reason is
        // worth keeping.</b> The argument was sound on its face — an alchemist sells a bottle for five gold
        // and most of the population cannot reach one, exactly as with wood — and the shard got measurably
        // worse: trade between bots fell from 3683gp a window to 490, sales from 38 to 12, fills from 63 to
        // 23, and the richest locked-out crafter from 158gp to 89.
        //
        // The difference is that <b>nobody makes bottles as a trade.</b> A feather, a log and a hide each
        // have somebody whose living it is to go and fetch one; glass only trickles back a bottle at a time
        // from whoever happens to drink a potion. So an order for twenty could not be filled — twelve raised,
        // one filled — and every one of them froze a hundred gold of a brewer's purse in escrow until the
        // market gave up on it an hour later. Money in escrow is money not spent, and three of this shard's
        // classes brew.
        //
        // The rule this leaves behind: <b>a material with no producer must not be ordered by the armful.</b>
        // The brewer's answer to glass is a counter or a bot's shop by the bank, not the board.

        return count;
    }

    /// <summary>What one of these opens at before the shard has ever traded one.</summary>
    private static int Guess(Type kind)
    {
        if (kind == typeof(Feather))
        {
            return GuessFeather;
        }

        if (kind == typeof(Log))
        {
            return GuessLog;
        }

        if (kind == typeof(RawRibs))
        {
            return GuessMeat;
        }

        return kind == typeof(Bottle) ? GuessGlass : GuessLeather;
    }

    private static void Once(Mobile body, Type kind)
    {
        if (_said)
        {
            return;
        }

        _said = true;

        logger.Information(
            "{Name} is the first crafter on this shard ever to ask the population for {Material}: until now the only way one could enter the world was a bot happening to kill the right animal",
            body.Name,
            kind.Name
        );
    }

    public static string Describe() =>
        Asked == 0
            ? $"no crafter has been asked about its materials ({NoTrade} answers went to bots with neither kit)"
            : $"{Asked} asked about a feather, a log, a hide or a rib: {Ordered} put the order to the population, {Standing} already have one out, "
              + $"{Stocked} have {Enough} already, {Shelved} took their own out of their bank box at a bank instead, "
              + $"{Poor} cannot afford one (the fattest purse among them held {Richest}gp); {Soon} crafters came again inside the minute (Asked counts materials, this counts bots); "
              + $"{MealCapped} asks for raw meat brought down to what the meal is worth (a cook orders {MeatBatch} at a time), "
              + $"{OutrunBots} bots held back from asking for {OutrunUnits} units ({Outrun} asks counting each minute's retry, {HeldNow()} held now) while the board already asked for more than {StockShare:P0} of what the island brings an hour (RawRibs now: {BotMarket.Wanting(typeof(RawRibs))} asked by buyers in the world against room for {StockRoom(typeof(RawRibs))}, {BotMarket.Brought(typeof(RawRibs))} brought in the hour, {BotMarket.Pace(typeof(RawRibs))} an hour at the pace of the last quarter-hour (half-life {BotMarket.SupplyHalfLifeMs / 60000} min; the room is sized to the lesser{(Thin(typeof(RawRibs)) ? ", and the pace is thin" : "")}), floor {StockFloor}{(SupplyGated ? "" : "; off")}); "
              + $"{SizedToRoom + SizedThin} raw meat lines said smaller than {MeatBatch} ({SizedToRoom} to the room left on the board, {SizedThin} to {MeatLeast} while the pace ran under {ThinShare:P0} of the hour's; {SizedUnits} units fewer); "
              + $"{Sated} raw meat wants taken back because their buyer held what they asked for from its own hunting ({SatedUnits} units; {SatedEarly} inside the want's first {BotLines.GraceMs / 60000} min, kept out, the rest counted failed{(TakeBackSated ? "" : "; off")})";

    public static void Forget()
    {
        _said = false;
        Asked = 0;
        NoTrade = 0;
        Stocked = 0;
        Shelved = 0;
        Standing = 0;
        Poor = 0;
        Soon = 0;
        Ordered = 0;
        Richest = 0;
        MealCapped = 0;
        Outrun = 0;
        OutrunBots = 0;
        OutrunUnits = 0;
        _holding.Clear();
        SizedToRoom = 0;
        SizedThin = 0;
        SizedUnits = 0;
        Sated = 0;
        SatedEarly = 0;
        SatedUnits = 0;
        _satedLook.Clear();
    }
}
