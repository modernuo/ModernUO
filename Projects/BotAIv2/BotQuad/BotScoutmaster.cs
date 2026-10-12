using System;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// Offers a captain the nearest ground nobody has ever stood in.
///
/// <para>
/// <b>Nearest rather than worst, which is the opposite of how every other errand on this shard picks its
/// destination.</b> A patrol goes where it is most dangerous and a great hunt goes where it is most dire,
/// because both are answers to "where is the trouble". This is an answer to "what do we not know", and
/// unknown ground is all equally unknown — there is nothing to rank it by. So the tiebreak is the walk:
/// the map fills outwards from where the population actually lives, which is also the order in which the
/// knowledge is worth having.
/// </para>
///
/// <para>
/// <b>And the candidates are ground the population has been <em>near</em>, never a search of the island.</b>
/// The quadrant table only holds squares something has happened in or beside, so the unknown squares this
/// can offer are the fringe of what is known — the ring just past the frontier. That keeps the question
/// cheap, keeps parties from being sent across the map, and means the frontier advances a ring at a time
/// under its own steam.
/// </para>
/// </summary>
public sealed class BotScoutmaster : IBotProposer
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotScoutmaster));

    public string Name => "Scoutmaster";

    public BotStanding Rung => BotStanding.Free;

    /// <summary>Asked of a bot that is not a captain. Not a refusal — most answers are this.</summary>
    public static long NotACaptain { get; private set; }

    public static long Asked { get; private set; }

    public static long Held { get; private set; }

    public static long Unfit { get; private set; }

    /// <summary>Captains too poor to pay a party and still stand on their feet.</summary>
    public static long Poor { get; private set; }

    /// <summary>The fattest purse among those. See <c>BotStable.Richest</c> for why this is kept.</summary>
    public static long Richest { get; private set; }

    /// <summary>Nothing unknown within reach. The island around the population is read.</summary>
    public static long Charted { get; private set; }

    /// <summary>Scouts sent back to ground nobody had counted in a while, the frontier being gone.</summary>
    public static long Resurveyed { get; private set; }

    /// <summary>Unknown ground with no way through to it that anybody has found.</summary>
    public static long Sealed { get; private set; }

    /// <summary>Too few free bodies about to be worth calling.</summary>
    public static long TooFewNear { get; private set; }

    public static long Offered { get; private set; }

    /// <summary>
    /// What one look before committing a party may cost.
    ///
    /// Five times an ordinary search, because the journey being vetted is five hundred tiles long and an
    /// ordinary ceiling would refuse every honest one of them. Asked about twice a minute across the whole
    /// population, so a third of a second is a rounding error against the walk it is deciding.
    /// </summary>
    public static double VetMs { get; set; } = 300.0;

    /// <summary>Looks that were handed less clock than they asked for, and so concluded nothing.</summary>
    public static long Cramped { get; private set; }

    public BotDeed Propose(IBotWilful bot)
    {
        var body = bot?.Self;
        var map = body?.Map;

        if (map == null || map == Map.Internal || !body.Alive)
        {
            return null;
        }

        // Counted before the class check so "nobody is a captain" and "the captain never gets an offer" are
        // different numbers rather than the same silence.
        if (body is not BotMobile { Class.Leads: true })
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

        // Asked before the ground is looked for rather than after: a captain who cannot pay is not going,
        // and walking the whole frontier to find that out would be work done for a refusal.
        var wealth = BotYield.Spendable(body);

        if (wealth - BotScout.Wage < BotScout.Solvent)
        {
            Poor++;

            if (wealth > Richest)
            {
                Richest = wealth;
            }

            return null;
        }

        var where = Unknown(map, body.Location, BotScout.Range);
        var again = false;

        // The frontier closed on 16.09.2026 — 4932 of 4936 squares stood in — and a scout that only ever looks for
        // ground nobody has stood in has nothing to do for ever after. Ground nobody has counted in six hours is the
        // next best question. See BotQuad.Stalest.
        if (where == Point3D.Zero)
        {
            where = BotQuad.Stalest(map, body.Location, BotScout.Range, at => Reachable(map, body.Location, at));
            again = where != Point3D.Zero;
        }

        if (where == Point3D.Zero)
        {
            Charted++;

            return null;
        }

        if (Free(body, BotScout.Reach) < BotScout.Least - 1)
        {
            TooFewNear++;

            return null;
        }

        // <b>One real search before six bots are committed to a five-minute walk, and it is the only place on
        // this shard where that trade is obviously worth making.</b>
        //
        // Reachable above is a dictionary lookup against pockets somebody has already proved closed, and it
        // cannot see the thing that actually stops a scouting party: water. The frontier offers the nearest
        // ground nobody has stood in, the population has by now walked out to its own coast, so the nearest
        // unknown ground is across it. Aldric took a party to (885, 2205), to (1005, 1185), to (975, 1185);
        // Baldric to (1845, 975) and (1065, 1095); every one of them ended with the party standing a hundred
        // and fifty tiles short and the errand taken off them by the stall watch. Six bots, five minutes,
        // each time, and the members are Bound throughout so none of them has work of its own.
        //
        // The cost is one search of a few hundred milliseconds against thirty bot-minutes, asked about twice
        // a minute across the whole population. See BotBolt.Retreat for the same reasoning in the same words:
        // it is for nobody to offer running to somebody who cannot run.
        //
        // Snapshotted so a starved search cannot be mistaken for an answer. If the population's second was
        // already spent the look got FloorMs, which proves nothing about the island, and striking a square off
        // the frontier is permanent.
        var cramped = BotPath.Starved;

        if (!BotPath.CanReach(map, body.Location, where, BotArrival.Within(BotQuad.Side / 3), VetMs))
        {
            if (BotPath.Starved != cramped)
            {
                Cramped++;

                return null;
            }

            // Marked read, exactly as BotScout.Bend marks a square it steps past, and for the same reason: it
            // genuinely is known now — known to be out of reach of where the population lives — and leaving it
            // unread offers it again on the next beat, which is the loop this whole file exists to end.
            BotQuad.Seen(map, where);
            Sealed++;

            return null;
        }

        Offered++;

        if (again)
        {
            Resurveyed++;
        }

        return new BotScout(map, where);
    }

    /// <summary>The nearest unknown ground this captain could actually get to, and walk home from. See BotScout.Roadworthy.</summary>
    private static Point3D Unknown(Map map, Point3D from, int within) =>
        BotQuad.Frontier(map, from, within, at => BotScout.Roadworthy(map, at) && Reachable(map, from, at));

    /// <summary>Whether the ground between here and there is not already known to be closed.</summary>
    private static bool Reachable(Map map, Point3D from, Point3D at)
    {
        // A dictionary lookup against pockets already proved closed by searches that failed — never a fresh
        // search. See BotHunter.Hunting for what a real search per candidate per beat costs this shard.
        if (BotReach.Ask(map, from, at, BotArrival.Within(BotQuad.Side / 3)) != BotReachVerdict.Sealed)
        {
            return true;
        }

        Sealed++;

        return false;
    }

    /// <summary>Bots near enough to be called on, who can fight and are not already in a company.</summary>
    private static int Free(Mobile body, int range)
    {
        var map = body.Map;
        var free = 0;

        foreach (var mobile in map.GetMobilesInRange<Mobile>(body.Location, range))
        {
            if (mobile == body || mobile is not IBotSquadMember { Squad: null })
            {
                continue;
            }

            if (mobile is IBotAlly { AbleToFight: true })
            {
                free++;
            }
        }

        return free;
    }

    public static string Describe() =>
        Asked == 0
            ? $"no captain has ever been offered a scouting party ({NotACaptain} answers went to bots that are not captains)"
            : $"{Asked} times a captain was asked to scout: {Offered} were offered unknown ground, {Held} were already in a company, "
              + $"{Unfit} were too hurt, {Poor} could not pay {BotScout.Wage}gp and keep {BotScout.Solvent} (the fattest purse among them held {Richest}gp), "
              + $"{Charted} found everything within {BotScout.Range} tiles already walked and counted lately, {Resurveyed} were sent back to ground uncounted for {BotQuad.StaleMs / 3600000} hours, {Sealed} found no way through to it and were struck off the frontier, {Cramped} could not be looked at for want of clock, "
              + $"{TooFewNear} had too few free bots near; {BotScout.Describe()}";

    public static void Forget()
    {
        Asked = 0;
        NotACaptain = 0;
        Held = 0;
        Unfit = 0;
        Poor = 0;
        Richest = 0;
        Charted = 0;
        Resurveyed = 0;
        Sealed = 0;
        Cramped = 0;
        TooFewNear = 0;
        Offered = 0;

        BotScout.Forget();
    }
}
