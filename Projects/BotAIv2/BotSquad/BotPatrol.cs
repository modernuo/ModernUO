using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// Offers a captain the worst square on the island and a company to take there.
///
/// <para>
/// <b>It answers one bot, and that is the whole of what "authority" means on this shard.</b> There is no
/// rank, no chain of command and nothing anybody is obliged to do. A captain gets an offer nobody else gets;
/// the bots it calls on are free, come because they were free, and go back to their own business the moment
/// the company ends. This project has refused to model obedience since the first week — a bot that does what
/// it is told is a bot whose motivation has been replaced by somebody else's — and a proposer that only ever
/// answers one class is the entire mechanism by which one bot can nevertheless lead.
/// </para>
///
/// <para>
/// <b>It refuses far more often than it offers, and every refusal is named.</b> A patrol needs a captain, a
/// quiet captain, volunteers standing near it and somewhere genuinely dangerous to go — and when there is no
/// patrol happening, "which of those four was missing" is the only question worth being able to answer. An
/// unnamed nought is the failure mode this shard has paid for more than any other.
/// </para>
/// </summary>
public sealed class BotPatrol : IBotProposer
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotPatrol));

    /// <summary>
    /// How far a captain will march a company from where it is standing.
    ///
    /// <para>
    /// <b>It follows how far the population may go, and pinning it to a number of its own is what stopped
    /// this working.</b> Bots die where they roam; a rescuer that cannot reach as far as the population
    /// wanders is a rescuer with a map of somewhere else. On 27.08.2026 <c>Roam</c> was raised to five
    /// hundred to spread the hunting out, and within the hour every death on the island was at x≈1865 —
    /// four hundred and twenty tiles from home — while this said three hundred. The Baron was asked a
    /// hundred times in a row and answered "nowhere has taken anybody" a hundred times, with three bodies on
    /// the board and two of them in one square. Two thresholds on one shelf, and the second one nobody moved.
    /// </para>
    ///
    /// <para>
    /// Nought means "as far as the population goes", which is the default and the only value that cannot go
    /// stale. Configuration may still pin a real number when a shorter leash is actually wanted.
    /// </para>
    /// </summary>
    public static int Range
    {
        get => _range > 0 ? _range : BotPopulation.Roam;
        set => _range = value;
    }

    private static int _range;

    public string Name => "Patrol";

    public BotStanding Rung => BotStanding.Free;

    public static long Asked { get; private set; }

    /// <summary>Asked of a bot that is not a captain. Not a refusal — fifteen of sixteen answers are this.</summary>
    public static long NotACaptain { get; private set; }

    public static long Held { get; private set; }

    public static long Unfit { get; private set; }

    public static long Peaceful { get; private set; }

    public static long TooFewNear { get; private set; }

    /// <summary>
    /// Dangerous squares turned down because the ground between here and there is known to be closed.
    ///
    /// A named nought of its own, because "the island is quiet" and "the worst of it is across water" are
    /// different facts about an evening and <see cref="Peaceful"/> would have reported both as the first.
    /// </summary>
    public static long Sealed { get; private set; }

    /// <summary>
    /// Offers made. <b>Not marches</b>, and it used to be printed as though it were: most offers lose the
    /// auction and are thrown away. What actually set out is <see cref="BotSweep.Marches"/>.
    /// </summary>
    public static long Offered { get; private set; }

    public BotDeed Propose(IBotWilful bot)
    {
        var body = bot?.Self;
        var map = body?.Map;

        if (map == null || map == Map.Internal || !body.Alive)
        {
            return null;
        }

        // Counted before the class check so that "nobody is a captain" and "the captain never gets an offer"
        // are different numbers rather than the same silence.
        // <b>A guild's head may call a company as a captain does (29.09.2026, Patrick's fifth answer).</b> "Deaths are not
        // bad, they are a sign that the bots should gather there and give battle" — and the population had one captain and
        // one baron to call the sweeps, three in two hours while the same six spawns killed two hundred. Every guild has a
        // head; the head calls.
        if (body is not BotMobile { Class.Leads: true } && !(body is BotMobile head && BotGuilds.Leads(head)))
        {
            NotACaptain++;

            return null;
        }

        Asked++;

        if (!BotSquads.Running)
        {
            return null;
        }

        if (bot is not IBotSquadMember { Squad: null })
        {
            Held++;

            return null;
        }

        if (body.HitsMax <= 0 || body.Hits < body.HitsMax * BotHunter.FitAt)
        {
            Unfit++;

            return null;
        }

        // Who could be called, and what they would bring, asked once (03.10.2026). See Takes.
        var free = Free(body, BotSweep.Reach, out var supply);

        // <b>The town's clearing before the island's worst (30.09.2026, build 341).</b> A town sets a guild a clearing — "make the
        // dangerous ground at (1305, 3735) read at least -0.90 … your members standing in it 10 minutes between them" — and
        // nothing on the shard offered it as work: on 339 The Anchor's stood at "0 of 10 minutes stood in it, 17 min left", The
        // Lantern's at 0 with 78 left, The Wolf's at 2 (`BotTownTask`; a failure raises the town's prices by half a percent and
        // three put the guild out). A company is what a clearing is: the town sets the worst ground near it, and a lone
        // watchman sent there is a death (see BotTownWatch). The head is offered its town's square first, where the gates reach it
        // and the company can be raised.
        // A point is a struct: "is { }" is true of the zero Clearing answers with when there is nothing to clear, and build 341
        // marched Brokk's and Liriel's companies on "the square at (0, 0)". Asked against zero.
        if (body is BotMobile { Guild: Server.Guilds.Guild guild } && Clearing(guild.Name, map, body.Location) is var clearing
            && clearing != Point3D.Zero && free >= BotSweep.Least - 1 && Takes(map, clearing, supply))
        {
            Offered++;
            ForTown++;

            return new BotSweep(map, clearing, BotPeril.Reading(map, clearing));
        }

        // <b>Asked with a refusal in hand, so that one unreachable square is not the whole map.</b> The worst
        // square is an opinion about where blood is being spilt and knows nothing about whether a company can
        // get there; without this the captain is shown that one square, is shown it again the instant the
        // patrol fails, and the next four places on the list are never offered at all.
        //
        // A dictionary lookup and it must stay one — the reach ledger answers from pockets already proved by
        // searches that failed, and a real search per candidate per captain per beat is the price the hunt's
        // proposer paid once and wrote down. See BotHunter.Hunting.
        // A captain with too few near is refused below as before, and its candidates are not weighed for strength, so the
        // refusals are counted once each.
        var refusedBefore = Closed + Outmatched;

        var square = BotPeril.Worst(
            map,
            body.Location,
            Range,
            out var reading,
            at => Reachable(map, body.Location, at) && (free < BotSweep.Least - 1 || Takes(map, at, supply))
        );

        if (square == Point3D.Zero)
        {
            // Every dangerous square within reach refused for closed ground or for strength is not a quiet island (review of
            // march10, F8): its own bucket.
            if (Closed + Outmatched > refusedBefore)
            {
                Unwinnable++;
            }
            else
            {
                Peaceful++;
            }

            return null;
        }

        if (free < BotSweep.Least - 1)
        {
            TooFewNear++;

            return null;
        }

        Offered++;

        return new BotSweep(map, square, reading);
    }

    /// <summary>Squares offered for a town's clearing rather than for the island's worst. See <see cref="Propose"/>.</summary>
    public static long ForTown { get; private set; }

    /// <summary>The square of a clearing this guild's town has set it and not yet had done, within <see cref="Range"/>; zero when none.</summary>
    private static Point3D Clearing(string guild, Map map, Point3D from)
    {
        foreach (var standing in BotBurgh.All)
        {
            if (standing.Guild != guild || standing.Task is not { Kind: BotTownTask.Kinds.Clear } task || task.Map != map
                || task.Presence >= BotTownTask.ClearPresence && BotQuad.Safety(map, task.Target) >= task.Goal)
            {
                continue;
            }

            if (!Utility.InRange(from, task.Target, Range) || !BotGates.Joined(map, from, task.Target) || !Reachable(map, from, task.Target))
            {
                continue;
            }

            return task.Target;
        }

        return Point3D.Zero;
    }

    /// <summary>Whether the ground between the captain and a square is not already known to be closed.</summary>
    private static bool Reachable(Map map, Point3D from, Point3D square)
    {
        if (BotReach.Ask(map, from, square, BotArrival.Within(BotPeril.Side / 3)) != BotReachVerdict.Sealed)
        {
            return true;
        }

        Sealed++;

        return false;
    }

    /// <summary>
    /// Bots near enough to be called on, who are able to fight and are not already in a company; and in
    /// <paramref name="supply"/> what the captain and the strongest of them, a full company's worth (<c>BotSquad.MaxSize</c>),
    /// would bring — the most the sweep's call (<c>BotSweep.Calling</c>, strongest first) can raise (03.10.2026).
    /// </summary>
    private static int Free(Mobile body, int range, out double supply)
    {
        var map = body.Map;
        var free = 0;

        _powers.Clear();

        foreach (var mobile in map.GetMobilesInRange<Mobile>(body.Location, range))
        {
            if (Callable(body, mobile))
            {
                free++;
                _powers.Add(BotThreat.Power(mobile));
            }
        }

        _powers.Sort();

        supply = BotThreat.Power(body);

        for (int i = _powers.Count - 1, taken = 0; i >= 0 && taken < BotSquad.MaxSize - 1; i--, taken++)
        {
            supply += _powers[i];
        }

        _powers.Clear();

        return free;
    }

    private static readonly System.Collections.Generic.List<double> _powers = [];

    /// <summary>Whether a captain's sweep may call this bot. Asked by the offer here and by the call itself (<c>BotSweep.Calling</c>).</summary>
    internal static bool Callable(Mobile captain, Mobile mobile) =>
        mobile != captain && mobile is IBotSquadMember { Squad: null }

        // Only a bot the levy's door would let in (01.10.2026, build 388): not a neutral class (BotTrading.Refuses) and not
        // one of a guild hostile to the captain's (BotSquads.Join). On 386 the sweep failed on its first beat "only 2 were
        // free to march" nineteen times — Edith six times in eleven seconds — with this count satisfied and the door shut.
        && mobile is IBotAlly { AbleToFight: true } && !BotTrading.Neutral(mobile) && !BotRegard.Hostile(captain, mobile);

    /// <summary>
    /// What a square asks of a sweep: the record of what has killed there (<c>BotQuad.Muscle</c>, the figure a prowl's company is
    /// raised to) or the fight standing there now (<c>BotPeril.Hostility</c>, what wanders as well as what the spawners keep),
    /// whichever is more — the clearing's own measure (<c>BotClearer</c>). See <see cref="Takes"/>.
    /// </summary>
    public static double Asks(Map map, Point3D at) => System.Math.Max(BotQuad.Muscle(map, at), BotPeril.Hostility(map, at));

    /// <summary>
    /// <b>A sweep is offered only ground it is not closed out of and can win (03.10.2026, build next).</b> The worst square was
    /// offered on its reading alone, and the march was never weighed against anything: on build 395 the ten sweeps of 03:20–04:19
    /// lost 23 bots and brought back one coin. Four of them — Rhistel's ten, Marcus's four, Haldir's four and Isembard's eight,
    /// 03:21–03:26 — marched on (1764, 1068), inside the quarantine the plague beast had closed at 03:19:00 and asking 13500,
    /// and 18 of their 26 died there by 03:37; 20 of the 60 deaths that hour inside a standing quarantine were sweeps'. Ground a bar or a
    /// quarantine closes (<c>BotBarred.Holds</c>) is the Baron's and the clearings' (<c>BotHarrow</c>, sized to the square and
    /// lifting the quarantine when they empty it), never a sweep's; and a square asking more (<see cref="Asks"/>) than the captain
    /// and the strongest it could call bring is passed over, as a prowl's company is raised only to what its ground asks.
    /// Counted: <see cref="Closed"/>, <see cref="Outmatched"/>.
    /// </summary>
    private static bool Takes(Map map, Point3D at, double supply)
    {
        if (BotBarred.Holds(map, at))
        {
            Closed++;

            return false;
        }

        if (Asks(map, at) > supply)
        {
            Outmatched++;

            return false;
        }

        return true;
    }

    /// <summary>Candidate squares passed over for lying in closed ground, and for asking more than the bots within call bring. See <see cref="Takes"/>.</summary>
    public static long Closed { get; private set; }

    /// <summary>Asks that found dangerous ground and every square of it closed or beyond what the captain could call (not "peaceful").</summary>
    public static long Unwinnable { get; private set; }

    public static long Outmatched { get; private set; }

    public static string Describe() =>
        Asked == 0
            ? $"no captain has ever been offered a patrol ({NotACaptain} answers went to bots that are not captains)"
            : $"{Asked} times a captain was asked: {Offered} were offered a square ({ForTown} of them their town's clearing), {Held} were already in a company, {Unfit} were too hurt, {Peaceful} found nowhere dangerous enough, {Sealed} found the worst of it behind something, {TooFewNear} had too few free bots near; {Unwinnable} found every dangerous square within reach closed or beyond what they could call ({Closed} candidate squares passed over for lying in closed ground and {Outmatched} for asking more than the captain and the strongest it could call bring); {BotSweep.Describe()}; {BotPeril.Describe()}";

    public static void Forget()
    {
        Asked = 0;
        NotACaptain = 0;
        Held = 0;
        Unfit = 0;
        Peaceful = 0;
        Sealed = 0;
        TooFewNear = 0;
        Offered = 0;
        Closed = 0;
        Outmatched = 0;
        Unwinnable = 0;
    }
}
