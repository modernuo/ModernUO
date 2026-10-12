using System;
﻿using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// Offers the Baron the ground that has killed the most people, and five bots to take there.
///
/// <para>
/// <b>It reads a different column of the same table the captain reads, and the difference is the whole
/// point.</b> <see cref="BotPatrol"/> asks <c>BotPeril.Worst</c> — a decaying frequency, which answers
/// "where is blood being spilt lately" and is exactly right for deciding where a company should be standing
/// before anything happens. This asks <c>BotPeril.Deadliest</c>, a count that does not fade, which answers
/// "where has it already gone wrong". A square can be top of one list and absent from the other, and both
/// lists are correct.
/// </para>
///
/// <para>
/// <b>Every refusal is named, and there is no bucket called "other".</b> A harrowing needs a Baron, a
/// Baron who is not already leading one, one who is fit to walk into it, ground with the dead on it and a
/// way through to that ground. When none is happening, which of those five was missing is the only
/// question worth being able to answer — an unnamed nought is the failure this shard has paid for more
/// than any other.
/// </para>
///
/// <para>
/// <b>It does not count volunteers, and it used to.</b> The offer was refused outright unless five free
/// bots were standing within forty tiles at the instant it was weighed, which is arithmetic about one
/// second of a working population and produced a Baron who never left town. Raising a company is the
/// errand's own business now — he goes to the square and calls for five minutes; see
/// <see cref="BotHarrow.MusterMs"/>. A muster that comes to nothing is counted there, by name, where it
/// actually happened.
/// </para>
/// </summary>
public sealed class BotHarrower : IBotProposer
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotHarrower));

    /// <summary>
    /// How far a Baron will march a company from where he is standing.
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

    public string Name => "Baron";

    public BotStanding Rung => BotStanding.Free;

    public static long Asked { get; private set; }

    /// <summary>Asked of somebody who is not a Baron. Not a refusal — nearly every answer is this.</summary>
    public static long NotABaron { get; private set; }

    public static long Held { get; private set; }

    public static long Unfit { get; private set; }

    /// <summary>Nowhere within reach has taken <c>BotPeril.Deadly</c> people.</summary>
    public static long Quiet { get; private set; }

    /// <summary>The deadliest ground is behind something the company cannot walk through.</summary>
    public static long Sealed { get; private set; }

    /// <summary>Ground the road map says has no road from home. The first harrow of build 72 marched 264 tiles short of such ground.</summary>
    public static long Roadless { get; private set; }

    /// <summary>Ground the road map says is more than <see cref="MostDetour"/> tiles round by road than straight.</summary>
    public static long Roundabout { get; private set; }

    /// <summary>
    /// How much further round by road than straight a square may be before the Baron passes it over.
    ///
    /// <para>
    /// Two hundred. On 16.09.2026 at 06:36 the levy marched on (825, 1605) — 880 by road against 615 straight — and the
    /// walk "stopped closing 264 tiles short after 600 beats": a company's march is one walk with one plan, and a plan
    /// does not carry a road that long round. Until the walk can follow the road map, ground that far round is ground
    /// the company cannot reach, and the map already knows it.
    /// </para>
    /// </summary>
    public static int MostDetour { get; set; } = 200;

    /// <summary>
    /// Dire ground with nowhere in it a body could stand — open water, and very little else.
    ///
    /// A named nought of its own, because "the island is quiet" and "the worst of it is a coordinate in the
    /// sea" are different facts about an evening and <see cref="Quiet"/> would report both as the first.
    /// </summary>
    public static long Unfooted { get; private set; }

    /// <summary>Asks where at least one damned square was passed over for want of grandmasters.</summary>
    public static long Unready { get; private set; }

    /// <summary>Asks passed over because a muster of this Baron's had just come to nothing.</summary>
    public static long Resting { get; private set; }

    public static long Offered { get; private set; }

    /// <summary>Times the Baron was offered a patrol against a murderer instead of ground.</summary>
    public static long Hunting { get; private set; }

    /// <summary>Offers made on ground the city put a bounty on.</summary>
    public static long Bountied { get; private set; }

    /// <summary>Dire squares passed over as a guild's own, that guild having a leader to raise its own company. See <see cref="BotReeve"/>.</summary>
    public static long Guilded { get; private set; }

    /// <summary>Dire squares passed over because their fight was several times the company a levy would raise. See BotPeril.Overwhelm.</summary>
    public static long Overwhelmed { get; private set; }

    /// <summary>Squares passed over because fewer bots stood within call than the least company. See <see cref="BotHarrow.Supply"/>.</summary>
    public static long Unmanned { get; private set; }

    private static bool _said;

    public BotDeed Propose(IBotWilful bot)
    {
        var body = bot?.Self;
        var map = body?.Map;

        if (map == null || map == Map.Internal || !body.Alive)
        {
            return null;
        }

        // Counted before the class check, so that "nobody is a Baron" and "the Baron is never offered
        // anything" stay different numbers instead of the same silence.
        if (body is not BotMobile { Class: BotBaron })
        {
            NotABaron++;

            return null;
        }

        Asked++;

        // <b>A muster that came to nothing is left alone for a while before it is posted again.</b> Patrick's
        // order of 05.09.2026: the levy takes bots off their work whether they volunteered or not, so a Baron
        // who re-posts a failed call on the next beat is a standing tax on the population. See
        // BotHarrow.RestMs; the rest itself is kept there, beside the muster it belongs to.
        if (BotHarrow.Resting(body))
        {
            Resting++;

            return null;
        }

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

        // Asked with a refusal in hand, so one unreachable square is not the whole map. The reach ledger
        // answers from pockets already proved by searches that failed, so this stays a dictionary lookup: a
        // real path search per candidate per beat is the price the hunt's proposer paid once and wrote down.
        // Over the ground the company will actually walk, not over one bookkeeping cell of it. See
        // BotPeril.Deadliest: cells are twenty-four tiles across and the box is seventy-five, and counting
        // the dead per cell is what made this offer nearly unsatisfiable.
        // <b>Chosen off the quadrant map rather than off the peril map, by order, and the two rank ground
        // by genuinely different things.</b> Peril counts the dead and forgets on a clock — right for a
        // captain deciding where a company should stand this minute, and wrong for this: a square that
        // killed nobody but has ground people down for an hour never rises on it, and a square that had one
        // bad night an hour ago quietly falls off it. BotQuad is a standing reputation that does not decay,
        // so "dire" means the island itself has been found wanting, which is what a great hunt answers.
        // <b>Damned ground the island cannot yet walk on is filtered out of the choice, not refused after
        // it has been chosen.</b> The first version of this check returned null on the direst square when
        // the population had no grandmasters — which is correct about that square and wrong about the
        // evening: one unwalkable cell then stopped every harrowing on the island, because Direst answers
        // with the worst and this threw the answer away instead of asking for the next one. The reach
        // ledger is already filtered here for exactly this reason; this is the same shape of fact about the
        // same candidate. Counted once per ask rather than per candidate: it walks the population.
        // <b>A murderer at large comes before any ground.</b> Patrick's order of 15.09.2026: the Baron may raise a
        // patrol against a red instead of hunting. See BotOutlaw and BotManhunt.
        // <b>A hideout a caught thief has given away comes first of all, because it is the one thing here that goes
        // stale.</b> Patrick's order of 17.09.2026. The knowledge lasts BotUnderworld.RaidTimeoutMs and no longer,
        // while a murderer at large will still be at large in five minutes; this stood second until the night of
        // 18.09.2026, when Hale gave the hideout away at 00:18:23 and the Baron went on raising patrols instead,
        // because the crime window always had a red somewhere in his reach. A rarer errand that expires must not
        // queue behind a common one that does not.
        if (BotUnderworld.RaidDue(out var den))
        {
            return new BotRaid(map, den, bot.Bond?.Weapon?.Skill ?? SkillName.Wrestling, true);
        }

        // <b>A murderer at large comes before any ground.</b> Patrick's order of 15.09.2026.
        var red = BotOutlaw.AtLarge(map, body.Location, Range);

        if (red != null)
        {
            Hunting++;

            return new BotManhunt(red, bot.Bond?.Weapon?.Skill ?? SkillName.Wrestling);
        }

        var unready = 0;
        var takeable = Takeable(map, body.Location, () => unready++);

        // <b>A bounty the city has put on ground comes before the map's own choice.</b> The debuggers' way of pointing
        // the Baron at a place: see BotCity.Bounty. Filtered like any other square — the road map, the reach ledger,
        // damned ground — so a bounty on ground nobody can reach is a bounty that lapses.
        var bountied = BotCity.Bountied(map, body.Location, Range);
        var quad = bountied != Point3D.Zero && takeable(bountied) ? BotQuad.At(map, bountied) : null;

        if (quad != null)
        {
            Bountied++;
        }

        // <b>A guild's own dire ground is the guild's to clear, while it has a leader to clear it.</b> See BotReeve:
        // Patrick's fourth Quad idea of 16.09.2026. The bounty above is the city's call and stays the Baron's.
        quad ??= BotQuad.Direst(
            map,
            body.Location,
            Range,
            at =>
            {
                if (!takeable(at))
                {
                    return false;
                }

                if (BotReeve.Keeps(map, at))
                {
                    Guilded++;

                    return false;
                }

                return true;
            }
        );

        if (unready > 0)
        {
            Unready++;
        }

        if (quad == null)
        {
            Quiet++;

            return null;
        }

        var square = BotQuad.Stand(quad);

        if (square == Point3D.Zero)
        {
            Unfooted++;

            return null;
        }

        // Not a square somebody is already harrowing. See BotHarrow.Taken.
        if (BotHarrow.Taken(map, square))
        {
            BotHarrow.Declined();

            return null;
        }

        // Not a muster nobody can answer: the least company must stand within call of the Baron now, or the five minutes
        // are spent learning so. See BotHarrow.Supply (29.09.2026).
        if (BotHarrow.Supply(map, body.Location, body) < BotHarrow.Least)
        {
            Unmanned++;

            return null;
        }

        Offered++;

        Once(body, square, quad.Deaths);

        return new BotHarrow(map, square, quad.Deaths);
    }

    /// <summary>Whether the ground between the Baron and the square is not already known to be closed.</summary>
    /// <summary>
    /// The test a square has to pass before a Baron would march on it: not damned beyond what the island can
    /// raise, and a road to it.
    ///
    /// <para>
    /// <b>Handed out rather than kept private, because the rounds ask the same question and were asking a
    /// different one.</b> <c>BotWarden</c> stands the scouting aside whenever dire ground exists — right in
    /// principle, since a Baron leading a company is refused a harrowing and the two offices must not
    /// compete on price. But it asked <c>BotQuad.Direst</c> with no filter at all, so it stood aside for
    /// ground this proposer then refused as damned, and the Baron was left with neither office. Measured on
    /// 08.09.2026: twenty asks, twenty "stood aside for ground dire enough to harrow" and twenty "passed
    /// over damned ground the island cannot yet raise a company for", printed in the same paragraph, and a
    /// Baron who did nothing but stroll round the town for an afternoon. Two gates in a ring, each holding
    /// the door for the other — the same shape as the three that once ringed the smith.
    /// </para>
    /// </summary>
    /// <param name="refused">Called once per square turned away for being damned. May be null.</param>
    public static Func<Point3D, bool> Takeable(Map map, Point3D from, Action refused = null)
    {
        var ready = BotHarrow.Musterable();

        return at =>
        {
            if (BotQuad.Damning(map, at) && ready < BotHarrow.Grandmasters)
            {
                refused?.Invoke();

                return false;
            }

            // <b>Nor ground whose fight is several times the company the levy would raise.</b> The auction refuses such a
            // harrow (BotPeril.Overwhelms, 26.09.2026), and a proposer that did not ask the same question would offer it at
            // every review. Reckoned at an ordinary bot a head, since the levy takes whoever it finds.
            if (BotPeril.Overwhelms(map, at, from, BotHarrow.Expected(map, at, BotKept.LoneBot), out _, count: false))
            {
                Overwhelmed++;

                return false;
            }

            return Reachable(map, from, at);
        };
    }

    private static bool Reachable(Map map, Point3D from, Point3D square)
    {
        // The road map first, where it has an answer: a square it says has no road, or is far round by road, is a
        // march that will stop short — the reach ledger only learns that after the levy has failed once.
        if (BotRoads.Covers(map, square.X, square.Y))
        {
            if (BotRoads.FromHome(map, square.X, square.Y) < 0)
            {
                Roadless++;

                return false;
            }

            // <b>Judged from home, never from where the Baron happens to stand.</b> The levy musters at the town and
            // marches from there, so the road that matters is the road from home; and BotRoads.Detour from anywhere else
            // is "at least", which from far off reads as nothing — at 17:36 on 16.09.2026 the Baron, standing at the
            // south-eastern cluster, was offered (825, 1605) (880 by road, 615 straight from home) because 880 minus his
            // own nine hundred minus the straight line between them came out below nought, and the levy stopped 264
            // tiles short for the third time that day.
            var road = BotRoads.FromHome(map, square.X, square.Y);
            var home = BotRoads.Home;
            var detour = road < 0 ? -1 : road - Math.Max(Math.Abs(square.X - home.X), Math.Abs(square.Y - home.Y));

            if (detour > MostDetour)
            {
                Roundabout++;

                return false;
            }
        }

        if (BotReach.Ask(map, from, square, BotArrival.Within(BotHarrow.Side / 3)) != BotReachVerdict.Sealed)
        {
            return true;
        }

        Sealed++;

        return false;
    }

    /// <summary>
    /// Said the first time only. An offer is not a march — most offers are weighed and thrown away — so a
    /// line per offer would be a log full of a company that never set out. What actually marched says so
    /// itself, in <see cref="BotHarrow"/>.
    /// </summary>
    private static void Once(Mobile body, Point3D square, int dead)
    {
        if (_said)
        {
            return;
        }

        _said = true;

        logger.Information(
            "{Name} has been offered the first harrowing on this shard: ({X}, {Y}), where {Dead} have died",
            body.Name,
            square.X,
            square.Y,
            dead
        );
    }

    public static string Describe() =>
        Asked == 0
            ? $"no Baron has ever been offered a harrowing ({NotABaron} answers went to bots that are not Barons)"
            : $"{Asked} times a Baron was asked: {Offered} were offered ground, {Hunting} a patrol against a murderer, {Guilded} dire squares left to the guild that holds them, {Overwhelmed} passed over for a fight {BotPeril.Overwhelm:F1} times the company a levy would raise, {Bountied} ground the city put a bounty on, {Held} were already leading a company, {Unfit} were too hurt, {Quiet} found nowhere reading at or below {BotQuad.Dire:F2}, {Sealed} found the worst of it behind something, {Roadless} on ground the road map says has no road from home, {Roundabout} more than {MostDetour} tiles round by road, {Unfooted} found nowhere in it to stand, {Unmanned} found fewer than {BotHarrow.Least} within call, {Resting} came too soon after a muster that failed, {Unready} passed over damned ground the island cannot yet raise a company for ({BotHarrow.Musterable()} of the {BotHarrow.Grandmasters} needed are fit for it today); {BotHarrow.Describe()}";

    public static void Forget()
    {
        _said = false;
        Asked = 0;
        NotABaron = 0;
        Held = 0;
        Unfit = 0;
        Quiet = 0;
        Sealed = 0;
        Roadless = 0;
        Roundabout = 0;
        Bountied = 0;
        Guilded = 0;
        Overwhelmed = 0;
        Unfooted = 0;
        Offered = 0;
        Resting = 0;
        Unready = 0;

        BotHarrow.Forget();
    }
}
