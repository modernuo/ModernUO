using System;
using Server.Items;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// Offers any bot with a mana pool the chance to lay in a few attack scrolls.
///
/// <para>
/// <b>The market had a supplier and no customers, and this is why.</b> Scribes write scrolls all day and put
/// them on the stalls; the only bot that ever asked for one was a bot filling a spellbook, because
/// <see cref="BotSeeker"/> refuses outright to anybody without a book. So the whole of demand was "mages
/// completing their libraries" — a want each, once, for ever — the goods piled up unsold, and the dashboard's
/// board of what the population is short of stayed empty for days while the log cheerfully reported scrolls
/// being written. A market with one buyer per spell is not a market.
/// </para>
///
/// <para>
/// <b>And a scroll is not a mage's tool.</b> The engine settles that: <c>Spell.ConsumeReagents</c> waves the
/// herbs away the moment a scroll is attached, no book is consulted anywhere, and the scroll is spent on the
/// cast. A warrior can throw two arrows at something on the way in and then draw a blade; a crafter caught in
/// the open can throw the thing that is chasing it instead of dying with a hammer in its hands. That is what
/// these are for, and it makes every fight on the shard into recurring demand for somebody's work — which is
/// the one shape of trade this economy has been missing.
/// </para>
///
/// <para>
/// It is offered, priced and refused like any other work. A bot that would rather dig, digs.
/// </para>
/// </summary>
public sealed class BotArmoury : IBotProposer
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotArmoury));

    /// <summary>
    /// How many attack scrolls a bot keeps by it.
    ///
    /// Few. These are an opening, not a career: enough to soften something on the way in or to buy the
    /// seconds it takes to get away, and not so many that a warrior spends its purse arming itself with
    /// somebody else's trade.
    /// </summary>
    public static int Stock { get; set; } = 3;

    /// <summary>
    /// Coin a bot will not spend on somebody else's trade.
    ///
    /// <para>
    /// Scrolls are bought one at a time and each one is cheap, which is exactly how a purse disappears
    /// without anybody noticing: a warrior that keeps topping up to its limit spends its whole takings a few
    /// coins at a time and then cannot afford a weapon, a bandage or a resurrection. Below this line the
    /// shopping stops, whatever the stock.
    /// </para>
    /// </summary>
    public static int Reserve { get; set; } = 150;

    /// <summary>
    /// The least mana that makes any of this worth buying.
    ///
    /// <para>
    /// A pool, not a skill. Casting from a scroll asks nothing of Magery beyond the engine's own roll, but it
    /// does spend mana out of whatever the bot has — so a bot with a pool of four can throw one arrow and
    /// then nothing, and buying five scrolls for it is buying four ornaments.
    /// </para>
    /// </summary>
    public static int LeastPool { get; set; } = 12;

    /// <summary>What one of these is reckoned to be worth per minute, before the ledger corrects it.</summary>
    public static double Prior { get; set; } = 14.0;

    private static bool _said;

    /// <summary>Bots asked, and how each answer went. No bucket called "other".</summary>
    public static long Asked { get; private set; }

    public static long NoPool { get; private set; }

    public static long Stocked { get; private set; }

    /// <summary>Wanted one and would not spend the last of its purse on it.</summary>
    public static long Broke { get; private set; }

    /// <summary>Casters not sent to buy a scroll they have on their own stall. See Propose.</summary>
    public static long Vending { get; private set; }

    public static long Offered { get; private set; }

    public string Name => "Armoury";

    public BotStanding Rung => BotStanding.Free;

    /// <summary>
    /// The attack scroll this bot keeps <see cref="Stock"/> of, or null. Asked by the shopping below and by the
    /// seller's keep list alike.
    ///
    /// <para>
    /// <b>One question for the buyer and the seller, because two answers to it were a round trip.</b> This proposer
    /// topped a pack up to three harm scrolls and <c>BotUnload.Keeps</c> had no entry for a scroll at all, so the
    /// next trip to a counter put all three on a stall as surplus and the bot went shopping again. Across four
    /// sessions on 14.09.2026 that was 471 harm scrolls stocked, between 59 and 80 a session bought over a
    /// shopkeeper's counter at 22gp — coin that leaves the world — and a dozen bots with no pen, Neriah, Orin
    /// Ashdown and Lysa Ashdown the busiest, selling the scrolls they had just bought. Nessa Ashdown's pack read
    /// "HarmScroll x3 (keeps 0, 3 surplus)".
    /// </para>
    ///
    /// <para>
    /// <b>The strongest attack this bot can actually use, not the cheapest one that exists.</b> This was a
    /// constant — the magic arrow, for everybody, for ever — and the reasoning beside it was sound as far
    /// as it went: a ladder that ignored what a bot can pay would have warriors shopping for energy bolts.
    /// But the answer to that is to ask about the bot, which BotStrike.Stock does, weighing the pool
    /// against the circle's mana and the bot's Magery against what the engine will roll when the scroll
    /// goes off. Frozen at the bottom rung, a mage that had spent all day getting better at magic bought
    /// exactly what it bought as a novice, and there was no rung above it to grow into.
    /// </para>
    /// </summary>
    public static Type Kept(Mobile body, out int spell)
    {
        spell = -1;

        if (body == null || BotGrimoire.Known == 0 || body.ManaMax < LeastPool)
        {
            return null;
        }

        spell = BotStrike.Stock(body);

        return spell < 0 ? null : BotGrimoire.ScrollFor(spell);
    }

    public BotDeed Propose(IBotWilful bot)
    {
        var body = bot?.Self;
        var map = body?.Map;

        if (map == null || map == Map.Internal || !body.Alive || BotGrimoire.Known == 0)
        {
            return null;
        }

        Asked++;

        var kind = Kept(body, out var spell);

        if (kind == null)
        {
            // Too small a pool, or none that covers a circle twice. A spell with no scroll is neither.
            if (spell < 0)
            {
                NoPool++;
            }

            return null;
        }

        var held = body.Backpack?.GetAmount(kind) ?? 0;

        if (held >= Stock)
        {
            Stocked++;

            return null;
        }

        // Not with the last of the purse. See Reserve: one scroll is cheap and a habit of topping up is not.
        if ((body.Backpack?.TotalGold ?? 0) <= Reserve)
        {
            Broke++;

            return null;
        }

        // Somebody of ours may already be making these. Asking the market first is what turns a shopkeeper's
        // price into a ceiling rather than the only option — see BotSeeker for the same order of preference.
        // <b>Its own stock first.</b> A scribe with harm scrolls of its own was sent to buy harm scrolls, the deed took hers back
        // off her stall and the peddler put them out again, three times a second (Brannoc, 06:13 on 14.09.2026). Its own scrolls lie
        // in its bank box now (01.10.2026): standing at a bank, it takes them out instead of buying.
        if (BotMarket.FromBank(bot, kind, Stock) > 0)
        {
            Vending++;

            return null;
        }

        var want = BotMarket.Wanted(bot, kind);

        BotShops.Survey(map, body.Location);

        var shop = BotShops.Nearest(bot, kind);
        var counter = shop == null ? 0 : BotShops.Price(shop, kind);
        var front = BotShopkeep.Best(bot, kind, 1, out var ask, out _, out var landed);

        Offered++;
        Once(body);

        // <b>Whichever is cheaper, and one of ours wins a tie.</b> A shopkeeper's price stays the ceiling —
        // no bot may charge more than the shelf for something the shelf has — but the two are not equivalent
        // at the same number. Coin paid to a bot moves inside the population and comes round again; coin paid
        // across a counter leaves the world for good, and a monster's purse is the only place new coin comes
        // from. So an equal price is a reason to buy from the scribe who wrote it rather than from the shelf.
        if (front != null && (counter <= 0 || landed <= counter))
        {
            return BotAcquire.AtShop(kind, spell, front, ask, toCast: true);
        }

        if (counter > 0)
        {
            return BotAcquire.Counter(kind, spell, shop, counter, toCast: true);
        }

        if (want != null)
        {
            return null;
        }

        // A scribe that writes these for less than asking would cost writes its own (03.10.2026). See BotQuill.OwnPen.
        if (BotQuill.OwnPen(bot, kind))
        {
            BotScribes.LeftToPen(body, kind);

            return null;
        }

        // Nobody sells them and no bot's shop has one. Say so aloud: that is the signal a scribe reads to decide what to write
        // next, and a carrier reads to walk one over.
        var offer = BotMarket.Worth(kind, BotGrimoire.ShopPrice(BotGrimoire.Circle(spell)));

        return BotAcquire.Board(kind, spell, map, body.Location, offer, toCast: true);
    }

    private static void Once(Mobile body)
    {
        if (_said)
        {
            return;
        }

        _said = true;

        logger.Information(
            "{Name} is the first bot with no spellbook to go shopping for scrolls; anything with {Pool} mana may now fight with them",
            body.Name,
            LeastPool
        );
    }

    public static string Describe() =>
        Asked == 0
            ? "nobody has been offered scrolls yet"
            : $"{Asked} asked: {Offered} sent shopping, {Stocked} already carrying {Stock}, {Broke} too poor to spare it, {Vending} took their own out of their bank box at a bank, {NoPool} with too small a pool";

    public static void Forget()
    {
        _said = false;
        Asked = 0;
        NoPool = 0;
        Stocked = 0;
        Broke = 0;
        Offered = 0;
    }
}
