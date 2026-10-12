using System;
using Server.Items;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// Offers a bot with an axe a trip to the woods, and only when somebody wants the wood.
///
/// <para>
/// <b>Gated on demand rather than on an empty pack, and that is the difference between a supply line and a
/// woodpile.</b> Nothing on this island eats logs by itself: they exist to become shafts, and shafts exist
/// to become arrows. So the question this proposer asks is not "am I short of wood" but "is anybody asking
/// for wood, or for something made of it" — which is the board, and the board is where the archer's own
/// want lands when the provisioner's twenty arrows are not enough.
/// </para>
///
/// <para>
/// That makes the whole of Patrick's chain one loop of reading and answering, with nobody told anything: an
/// archer runs low and posts arrows; a fletcher reads arrows and finds it is short of wood; this reads the
/// same board and sends somebody to a tree; the hunters read the feather want the fletcher raises and go
/// looking for birds. Four trades, one board, no messages.
/// </para>
/// </summary>
public sealed class BotWoodsman : IBotProposer
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotWoodsman));

    private static bool _said;

    /// <summary>Every gate apart, with the denominator. There is no bucket called "other".</summary>
    public static long Asked { get; private set; }

    public static long NoAxe { get; private set; }

    /// <summary>
    /// Answers that went to bots whose only axe was too heavy for them to hold. Apart from <see cref="NoAxe"/>,
    /// because "has no axe" and "has one it cannot lift" are different facts. See <c>BotTimber.Tool</c>.
    /// </summary>
    public static long TooHeavy { get; private set; }

    public static long NoCall { get; private set; }

    /// <summary>
    /// Fewest logs on show at the shops by the bank (the stalls, until 01.10.2026) before a fletcher's going without wood calls the
    /// woodcutters. See <see cref="ForStalls"/>.
    /// </summary>
    public static int LeastOnMarket { get; set; } = 40;

    /// <summary>
    /// Trips sent because a fletcher lately found no wood anywhere and the stalls held fewer than <see cref="LeastOnMarket"/> logs.
    ///
    /// <para>
    /// <b>The call a poor fletcher could not pay to make (30.09.2026).</b> This trade cuts only for money on the board, a fletcher
    /// of its own with feathers, or arrows asked for; and a fletcher's order for wood needs five logs over the hundred gold it
    /// keeps back. On build 334 at 23:03, 105 of 110 fletcher asks "could not find wood" while 3738 of 3904 woodcutter asks on 336
    /// found "nobody asking for wood or arrows" — the two ends of one chain, each waiting for the other. On 336 the wood reached the
    /// stalls only in the second hour, when gatherers carried their own surplus logs to a counter (the fletchers' "could not find
    /// wood" froze at 118 from 00:33). A fletcher's want of wood is now read as a call while it is fresh and the stalls are short,
    /// and what is cut goes to a stall above the twenty a fletching cutter keeps (<c>BotTimber.Store</c>).
    /// </para>
    /// </summary>
    public static long ForStalls { get; private set; }

    public static long Stocked { get; private set; }

    public static long NoTree { get; private set; }

    public static long Sent { get; private set; }

    public string Name => "Woodsman";

    public BotStanding Rung => BotStanding.Free;

    public BotDeed Propose(IBotWilful bot)
    {
        var body = bot?.Self;
        var map = body?.Map;

        if (map == null || map == Map.Internal || !body.Alive)
        {
            return null;
        }

        if (BotTimber.Tool(body, out var heavy) == null || BotTimber.System == null)
        {
            if (heavy)
            {
                TooHeavy++;
            }
            else
            {
                NoAxe++;
            }

            return null;
        }

        Asked++;

        // Somebody has to want it. Logs directly, or arrows — which are logs one step further on, and which
        // is what an archer actually asks for — or this bot's own bench.
        //
        // <b>Its own trade counts, and leaving it out kept the arrow chain shut after every other link of it
        // had been opened.</b> Nothing on Felucca sells a log: the shard says so itself, once, at
        // error level — "No shopkeeper within reach of the bots on Felucca sells wood, so no arrows can be
        // made". So a fletcher's only two routes to wood are the board and its own axe. The board wants
        // money it does not have — 8562 refusals for want of a purse at 13:01 on 04.09.2026, the richest of
        // them holding 124 gold against a reserve of 150 — which leaves the axe, and the axe was reserved
        // for bots filling somebody else's order. A fletcher standing on twenty feathers with a hatchet in
        // its pack, told that nobody is asking for wood, is the whole trade stopped on a technicality.
        var called = Fletching(body) || Wanted(typeof(Log)) || Wanted(typeof(Arrow));
        var forStalls = !called && BotFletcher.Woodless && BotShopkeep.Showing(typeof(Log)) < LeastOnMarket;

        if (!called && !forStalls)
        {
            NoCall++;

            return null;
        }

        if (BotTimber.Logs(body) >= BotTimber.Worthwhile)
        {
            Stocked++;

            return null;
        }

        // Asked before the errand rather than discovered at the first beat: a trip to a wood that is not
        // there is the shape of failure this shard has paid for five times over. A skipped candidate is
        // free; an errand that fails on its opening beat is offered again on the next.
        var tree = BotTimber.Find(body);

        if (tree == null)
        {
            NoTree++;

            return null;
        }

        Sent++;

        if (forStalls)
        {
            ForStalls++;
        }

        Once(body);

        return new BotChop(map, new Point3D(tree.X, tree.Y, tree.Z), BotTimber.Worthwhile - BotTimber.Logs(body));
    }

    /// <summary>
    /// Whether this bot is a fletcher holding feathers and short of the wood to feather them.
    ///
    /// <para>
    /// Feathers rather than merely a kit, because wood is only worth cutting to somebody who can use it: the
    /// feather is the binding half of an arrow and a fletcher without one has no more use for a log than a
    /// warrior has. The same pair of numbers the fletcher's own proposer reads, so the two cannot drift.
    /// </para>
    /// </summary>
    private static bool Fletching(Mobile body) =>
        BotFletching.Kit(body) != null
        && BotFletching.Feathers(body) > 0
        && BotFletching.Logs(body) + BotFletching.Shafts(body) < BotFletching.LeastArrows;

    /// <summary>Whether anybody has money down on the board for this, right now.</summary>
    private static bool Wanted(Type kind)
    {
        var wants = BotMarket.Wants;

        for (var i = 0; i < wants.Count; i++)
        {
            if (wants[i].IsOpen && wants[i].Kind == kind)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Said once. The first tree ever cut on this shard is worth a line; the thousandth is not.</summary>
    private static void Once(Mobile body)
    {
        if (_said)
        {
            return;
        }

        _said = true;

        logger.Information(
            "{Name} is the first bot on this shard ever to cut wood; until now Lumberjacking was a skill nobody had an errand for",
            body.Name
        );
    }

    public static string Describe() =>
        Asked == 0
            ? $"nobody has been offered wood ({NoAxe} answers went to bots with no axe, {TooHeavy} to bots whose only axe was too heavy to hold)"
            : $"{Asked} asked to cut wood ({TooHeavy} not asked for carrying only an axe too heavy to hold):{Sent} sent to a tree ({ForStalls} of them because a fletcher went without wood and the shops by the bank showed under {LeastOnMarket} logs), {NoCall} found nobody asking for wood or arrows, {Stocked} were carrying enough already, {NoTree} had no tree within {BotTimber.Reach} tiles ({BotTimber.Townbound} passed over for standing inside a town and {BotTimber.Fenced} on ground that has refused the population, {BotTimber.Unwalkable} for a walk refused at the trunk lately); {BotChop.Bent} trees swapped for the next when the walk to one was refused; {BotChop.Spoken} trees given up because the engine said they were cut out against {BotChop.Silent} given up by the clock alone, {BotChop.Unreached} for the engine calling every swing out of range; "
              + $"{BotTimber.Ordered} logs kept for somebody's want, to be carried to it, and {BotTimber.Listed} kept to sell, above the {BotTimber.Keeps} a cutter that can fletch keeps back";

    public static void Forget()
    {
        Asked = 0;
        NoAxe = 0;
        TooHeavy = 0;
        NoCall = 0;
        Stocked = 0;
        NoTree = 0;
        ForStalls = 0;
        BotTimber.ForgetTrade();
        Sent = 0;
    }
}
