using System;
using System.Collections.Generic;
using Server.Logging;
using Server.Mobiles;
using Server.Regions;

namespace Server.BotAI.V2;

/// <summary>
/// Offers any fighter the square where bots lately died, and a company to clear it.
///
/// <para>
/// <b>Patrick's order of 02.10.2026: "I don't see bots gathering in companies and going to the squares where others died,
/// to clear them and protect other bots on the road."</b> Measured on build 388's first nine hours (00:14–09:15): 721
/// deaths, 500 of them of bots alone, in the same cells all night — (1300, 1400) 32, (900, 700) 25, (1700, 2500) 23,
/// (1800, 3000) 22 — and what was raised against them: 59 patrols (<see cref="BotPatrol"/>, offered to captains and guild
/// heads only, 3349 of 5078 asks refused for "too few free bots near" within forty tiles, aimed at the blow count rather
/// than at the dead), 15 of the Baron's musters of which 1 marched (<see cref="BotHarrower"/>, one bot on the island may
/// call one), 43 guild harrows offered for a guild's own squares (<see cref="BotReeve"/>, which needs a guild with ground).
/// 94 boxes were closed to single bots that died twice on a piece of ground (<see cref="BotBreaker"/>) and 90 killing
/// fields quarantined (<see cref="BotBarred"/>), and nothing on the shard lifted either except the clock. The three
/// companies that clear ground were each behind a gate — a rank, a guild, a Baron — and the ground was behind all three.
/// </para>
///
/// <para>
/// <b>So any fighter may call one.</b> Not a producer, not a neutral trader, not a novice, not a healer (who is levied as
/// the company's medic), not one already in a company, hurt, or standing off a muster that failed. The square: one of
/// the quadrant map's where <see cref="LeastDeaths"/> or more died, the last inside <see cref="RecentMs"/>, within
/// <see cref="Reach"/> of the fighter, reachable, not already being harrowed, not cleared within the last hour, and not
/// damned beyond what the island can raise (<see cref="BotHarrower.Takeable"/>, the Baron's own test: damned ground is
/// the Baron's). The ones on a road between towns come first (<see cref="BotRoadbook"/>: within <see cref="RoadWidth"/>
/// of a drawn road's waypoints), then the most dead, then the latest. The company is the harrow's
/// (<see cref="BotHarrow"/>, as a clearing): levied within two hundred tiles of the muster, sized by strength to what the
/// square asks — the record of what has killed there (<see cref="BotQuad.Muscle"/>) or what stands there now
/// (<see cref="BotPeril.Hostility"/>), whichever is more, at <see cref="BotHarrow.ClearFit"/> — and not offered at all
/// when the bots within call do not bring that much (<see cref="BotHarrow.SupplyStrength"/>), because a muster that
/// cannot be filled is five minutes of a fighter standing still.
/// </para>
///
/// <para>
/// <b>Every refusal is named.</b> When no clearing happens, which of the gates was shut is the only question worth being
/// able to answer; an unnamed nought is the failure this shard has paid for most.
/// </para>
///
/// <para>
/// <b>The quarantined death squares, first, by the priced figure (03.10.2026, build next; Patrick's order of 02.10.2026, item 4:
/// "companies must gather and go to the squares where others died, clear them, and protect bots on the road").</b> On builds 395
/// and 396 this cleared nothing — 0 marched, 0 quarantines lifted, up to 72 standing at once — for four reasons, measured: of the
/// 31563 asks on 396 that found a square, 62 % found fewer than three within call and 35 % found the bots within call short of
/// the <i>first</i> square chosen and never tried the next (19 offered in 3.7 hours); the offer told the auction it brought the
/// leader's strength six times over, not the company it would march with (<see cref="BotHarrow.Brings"/>); the company was sized
/// in heads from the leader's strength while the march waited on strength, so every muster that was taken came up short at the
/// bell with every member standing in it (11 of 11 with 16221 of the 16875 asked, 6 of 6 with 8970, 9 of 9 with 11509 — all for
/// (1695, 1365), inside a Reapers' killing field); and the ground was judged by the Baron's test (a levy of lone bots, roads
/// from Britain). Sweeps were kept out of closed ground the same night (march10), so nobody went in.
/// </para>
///
/// <para>
/// Now every quarantine standing within <see cref="Reach"/> is a candidate by the square holding its middle, whatever that
/// square's own record, and comes first. What a square asks (<see cref="Asks"/>) is the record, the fight standing there now,
/// the strongest thing alive in the box at its kind's price in our lives (<see cref="Strongest"/>), and for a quarantine the
/// strongest thing recorded killing in it, priced the same (<see cref="BotBarred.Closing"/>) — the figure a company withdraws by
/// (<see cref="BotSquad.Reckon"/>), so a clearing that marches does not withdraw from what it was sized for. A quarantine the
/// plague or a killer of <see cref="BotBarred.QuarantinePower"/> declared, and one asking past what the island's strongest company
/// of <see cref="BotSquad.MaxSize"/> brings (<see cref="Island"/>), stays closed and is said so in the log once; a square asking
/// past what the strongest company of the bots within call makes is passed over for the next. The clearing walks the whole
/// quarantine box and lifts it only when nothing in it is left past a lone bot (<see cref="BotBarred.Lift"/>).
/// </para>
/// </summary>
public sealed class BotClearer : IBotProposer
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotClearer));

    public string Name => "Clearer";

    public BotStanding Rung => BotStanding.Free;

    /// <summary>A switch.</summary>
    public static bool Running { get; set; } = true;

    /// <summary>How far from where it stands a fighter looks for a square to clear. Six hundred: a town and its surroundings.</summary>
    public static int Reach { get; set; } = 600;

    /// <summary>How many deaths a square needs to be offered. Two.</summary>
    public static int LeastDeaths { get; set; } = 2;

    /// <summary>How lately the last death on it must have fallen. Two hours.</summary>
    public static int RecentMs { get; set; } = 7200000;

    /// <summary>The main skill a fighter needs to call a company. Fifty: not a novice (<see cref="BotLadder.NoviceSkill"/>).</summary>
    public static double LeastSkill { get; set; } = 50.0;

    /// <summary>Veterans asked past the class and novice bars (07.10.2026, traits17).</summary>
    public static long VeteransAsked { get; private set; }

    /// <summary>How near a drawn road between towns a square must lie to count as on it. Thirty: a quadrant.</summary>
    public static int RoadWidth { get; set; } = 30;

    /// <summary>How long a square's road answer is kept. Ten minutes; roads are drawn once and walked after.</summary>
    public static int RoadMemoryMs { get; set; } = 600000;

    /// <summary>How long after a clearing finished on a square it is not offered again. The hour the deaths-after count runs for.</summary>
    public static int ClearedRestMs { get; set; } = 3600000;

    /// <summary>
    /// How many clearings may stand on the island at once. Three: each is a levy of up to a company off their work, and a
    /// population of fifty with four clearings called is a population at a muster.
    /// </summary>
    public static int MostAtOnce { get; set; } = 3;

    /// <summary>Asks refused because <see cref="MostAtOnce"/> clearings already stood.</summary>
    public static long Enough { get; private set; }

    // ---- Every gate, counted apart -----------------------------------------------------------------------------------------

    public static long Asked { get; private set; }

    /// <summary>Asked of a bot that is not a fighter: a producer, a neutral, a healer. Not a refusal.</summary>
    public static long NotAFighter { get; private set; }

    public static long Novices { get; private set; }

    public static long Held { get; private set; }

    public static long Unfit { get; private set; }

    public static long Resting { get; private set; }

    /// <summary>Asks that found no square with the deaths within reach.</summary>
    public static long Quiet { get; private set; }

    /// <summary>Squares passed over for being harrowed already, cleared lately, or beyond the Baron's own test.</summary>
    public static long TakenOver { get; private set; }

    public static long ClearedLately { get; private set; }

    public static long Untakeable { get; private set; }

    /// <summary>Asks refused because everybody within call brought less than the square asks.</summary>
    public static long TooWeak { get; private set; }

    public static long Unmanned { get; private set; }

    public static long Offered { get; private set; }

    /// <summary>Of the offers, squares on a road between towns.</summary>
    public static long OnRoads { get; private set; }

    /// <summary>Of the offers, squares inside a standing quarantine (03.10.2026).</summary>
    public static long OfferedClosed { get; private set; }

    /// <summary>Squares passed over for lying in one of Patrick's standing bars, which nothing lifts.</summary>
    public static long Barred { get; private set; }

    /// <summary>Squares passed over for lying in a quarantine the plague or a killer of <see cref="BotBarred.QuarantinePower"/> declared.</summary>
    public static long Doomed { get; private set; }

    /// <summary>Squares passed over for asking, at <see cref="BotHarrow.ClearFit"/>, more than the island's strongest company brings. See <see cref="Island"/>.</summary>
    public static long Beyond { get; private set; }

    /// <summary>Squares passed over for asking more than the strongest company the bots within call make; the next is tried.</summary>
    public static long Outmatched { get; private set; }

    /// <summary>Asks that found squares within reach and passed every one over for something other than strength.</summary>
    public static long Untaken { get; private set; }

    /// <summary>Quarantines said in the log to stay closed to the clearings, once each.</summary>
    public static long SaidClosed { get; private set; }

    /// <summary>The boxes already said to stay closed, so each is said once. See <see cref="Say"/>.</summary>
    private static readonly HashSet<(int, int, int, int)> _saidClosed = [];

    /// <summary>The quarantine at each candidate's middle, for the order and the gates. Reused.</summary>
    private static readonly Dictionary<BotQuad.Quad, BotBarred.Closure> _closed = [];

    /// <summary>The places already weighed in one ask. Reused.</summary>
    private static readonly HashSet<Point3D> _tried = [];

    /// <summary>Squares passed over as resting after a clearing's muster on them came up short. See <see cref="RestSquare"/>.</summary>
    public static long SquareResting { get; private set; }

    /// <summary>Asks refused because the company would have mustered inside closed ground (review of clear12, F1).</summary>
    public static long ClosedMuster { get; private set; }

    private static readonly Dictionary<(int Map, int X, int Y), long> _restingSquares = [];

    /// <summary>
    /// <b>A square whose clearing came up short at the bell rests, not only its leader (review of clear12, F2).</b> On 396 Petra,
    /// Kaelthas and Isembard took (1695, 1365) one after another inside twenty minutes, each short at the bell: the leader's rest
    /// (<see cref="BotHarrow.Rest"/>) let the next fighter take the same square at its next review. Rested
    /// <see cref="BotHarrow.RestMs"/>, by the square's quadrant.
    /// </summary>
    public static void RestSquare(Map map, Point3D square)
    {
        if (map == null)
        {
            return;
        }

        if (_restingSquares.Count >= 1024)
        {
            _restingSquares.Clear();
        }

        _restingSquares[BotQuad.Key(map, square)] = Core.TickCount + BotHarrow.RestMs;
    }

    private static bool SquareRests(Map map, Point3D square) =>
        _restingSquares.TryGetValue(BotQuad.Key(map, square), out var until) && Core.TickCount - until < 0;

    private static bool _said;

    /// <summary>Whether each square lies on a road, remembered a while. See <see cref="RoadWidth"/>.</summary>
    private static readonly Dictionary<(int Map, int X, int Y), (bool OnRoad, long Tick)> _roads = [];

    private static readonly List<BotQuad.Quad> _found = [];

    public BotDeed Propose(IBotWilful bot)
    {
        var body = bot?.Self;
        var map = body?.Map;

        if (!Running || map == null || map == Map.Internal || !body.Alive || !BotSquads.Running)
        {
            return null;
        }

        // A veteran leads a clearing whatever its class and its skill, a trader excepted (07.10.2026, traits17). See BotTraits.Veteran.
        var veteran = BotTraits.Veteran(body);

        if (body is not BotMobile { Class: { } klass } fighter || !veteran && klass.Role is BotRole.Producer or BotRole.Medic || BotTrading.Refuses(body))
        {
            NotAFighter++;

            return null;
        }

        Asked++;

        if (!veteran && (klass.MainSkill is not { } main || fighter.Skills[main].Value < LeastSkill))
        {
            Novices++;

            return null;
        }

        if (veteran)
        {
            VeteransAsked++;
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

        if (BotHarrow.Resting(body))
        {
            Resting++;

            return null;
        }

        if (BotHarrow.ClearingsStanding() >= Math.Max(1, MostAtOnce))
        {
            Enough++;

            return null;
        }

        var quad = Choose(map, body, out var within, out var square, out var asked, out var onRoad, out var closed, out var dead);

        if (quad == null)
        {
            return null;
        }

        // Where the company would muster: the town's gate towards the square when the fighter stands in one, else here.
        var muster = BotPopulation.Gate(map, body.Location, square, counted: false);

        if (muster == Point3D.Zero)
        {
            muster = body.Location;
        }

        // <b>Never a muster inside closed ground (review of clear12, F1).</b> Outside a town the muster is where the fighter stands,
        // and nothing makes a bot leave a standing quarantine: the levy would walk in one at a time and the first three would
        // fight what twenty were sized for.
        if (BotBarred.Holds(map, muster))
        {
            ClosedMuster++;

            return null;
        }

        if (BotHarrow.Supply(map, muster, body) + 1 < BotHarrow.Least)
        {
            Unmanned++;

            return null;
        }

        var supply = muster == body.Location ? within : BotHarrow.SupplyStrength(map, muster, body);

        if (supply < asked * BotHarrow.ClearFit)
        {
            TooWeak++;

            return null;
        }

        Offered++;

        if (onRoad)
        {
            OnRoads++;
        }

        if (closed)
        {
            OfferedClosed++;
        }

        Once(body, square, dead, asked, supply, onRoad);

        // A quarantine's clearing walks the whole box the quarantine closed, not the harrow's own: it is lifted only when all of it is
        // empty (BotBarred.Lift).
        return new BotHarrow(map, square, dead, asked, clearing: true, closed ? BotBarred.QuarantineHalf : 0);
    }

    /// <summary>
    /// The square to clear: inside a standing quarantine first, then on a road, then the most dead, then the latest death. Null,
    /// counted, when none. What it asks of a company is <see cref="Asks"/>; a square asking at <see cref="BotHarrow.ClearFit"/> more
    /// than <paramref name="within"/> (the strongest company the bots within call of the fighter make) is passed over for the
    /// next one, rather than the ask thrown away on the first (03.10.2026). The muster's own figure is asked again where it stands.
    /// </summary>
    private static BotQuad.Quad Choose(
        Map map, Mobile body, out double within, out Point3D square, out double asked, out bool onRoad, out bool closed, out int dead
    )
    {
        within = 0.0;
        square = Point3D.Zero;
        asked = 0.0;
        onRoad = false;
        closed = false;
        dead = 0;

        var now = Core.TickCount;
        var from = body.Location;

        _found.Clear();
        _closed.Clear();

        foreach (var quad in BotQuad.All)
        {
            if (quad.Map != map || quad.Deep || quad.Townbound || quad.Deaths < Math.Max(1, LeastDeaths) || quad.DiedTick == 0)
            {
                continue;
            }

            if (now - quad.DiedTick > RecentMs || quad.Safety > BotQuad.Wanted)
            {
                continue;
            }

            var middle = quad.Middle;

            if (Math.Max(Math.Abs(middle.X - from.X), Math.Abs(middle.Y - from.Y)) > Reach)
            {
                continue;
            }

            _found.Add(quad);
        }

        // <b>Every quarantine standing within reach, by the square holding its middle (03.10.2026).</b> A killing field is three
        // deaths within forty-five tiles, which can fall in three squares of one death each, none of them on the list above; the
        // quarantine is the evidence, and it is the ground nobody else may enter.
        foreach (var closure in BotBarred.Closures())
        {
            var middle = closure.Middle;

            if (Math.Max(Math.Abs(middle.X - from.X), Math.Abs(middle.Y - from.Y)) > Reach)
            {
                continue;
            }

            var quad = BotQuad.Known(map, middle);

            if (quad == null || quad.Deep || quad.Townbound || _found.Contains(quad))
            {
                continue;
            }

            _found.Add(quad);
        }

        if (_found.Count == 0)
        {
            Quiet++;

            return null;
        }

        for (var i = 0; i < _found.Count; i++)
        {
            var middle = _found[i].Middle;

            if (BotBarred.Closing(map, new Point3D(middle.X, middle.Y, 0), out var closure))
            {
                _closed[_found[i]] = closure;
            }
        }

        // Closed ground first, then roads, then the dead, then the latest. Road answers are remembered; the rest is integers.
        _found.Sort(
            (a, b) =>
            {
                var ca = _closed.ContainsKey(a);
                var cb = _closed.ContainsKey(b);

                if (ca != cb)
                {
                    return ca ? -1 : 1;
                }

                var ra = OnRoad(a);
                var rb = OnRoad(b);

                if (ra != rb)
                {
                    return ra ? -1 : 1;
                }

                if (a.Deaths != b.Deaths)
                {
                    return b.Deaths.CompareTo(a.Deaths);
                }

                return b.DiedTick.CompareTo(a.DiedTick);
            }
        );

        var island = Island(map);
        var outmatched = false;
        var grandmasters = -1;

        within = BotHarrow.SupplyStrength(map, from, body);
        _tried.Clear();

        for (var i = 0; i < _found.Count; i++)
        {
            var quad = _found[i];
            var inClosure = _closed.TryGetValue(quad, out var closure);

            // A quarantine is walked from its own middle, where the death that closed it fell; a square of its own otherwise.
            var stand = inClosure ? Settled(map, closure.Middle) : Point3D.Zero;

            if (stand == Point3D.Zero)
            {
                stand = BotQuad.Stand(quad);
            }

            // Several squares of one quarantine stand on its middle: weighed once.
            if (stand == Point3D.Zero || !_tried.Add(stand))
            {
                continue;
            }

            // Cleared within the hour: this square, or the square the clearing would stand on (a quarantine's middle), which is the
            // one a clearing that ended marks (BotQuad.Cleared) — a quarantine left standing for something still in it is not
            // offered again at once.
            var standQuad = BotQuad.Known(map, stand);

            if (quad.HarrowedTick != 0 && now - quad.HarrowedTick < ClearedRestMs
                || standQuad is { HarrowedTick: not 0 } && now - standQuad.HarrowedTick < ClearedRestMs)
            {
                ClearedLately++;

                continue;
            }

            if (BotHarrow.Taken(map, stand))
            {
                TakenOver++;

                continue;
            }

            if (SquareRests(map, stand))
            {
                SquareResting++;

                continue;
            }

            if (BotBarred.BarredOutright(map, stand))
            {
                Barred++;

                continue;
            }

            if (inClosure && closure.Dire)
            {
                Doomed++;
                Say(closure, $"it was closed for \"{closure.Why}\", and no company is sent where the plague or a killer of {BotBarred.QuarantinePower:F0} has killed");

                continue;
            }

            if (BotQuad.Damning(map, stand))
            {
                if (grandmasters < 0)
                {
                    grandmasters = BotHarrow.Musterable();
                }

                if (grandmasters < BotHarrow.Grandmasters)
                {
                    Untakeable++;

                    continue;
                }
            }

            if (!Reachable(map, from, stand))
            {
                Untakeable++;

                continue;
            }

            var ask = Asks(map, stand, inClosure ? closure : null);

            if (ask * BotHarrow.ClearFit > island)
            {
                Beyond++;

                if (inClosure)
                {
                    Say(
                        closure,
                        $"it asks {ask:F0} of a company — the record, what stands in it now and what killed in it, at the kind's price in our lives"
                        + (closure.WorstKind != null ? $"; its worst killer on record a {closure.WorstKind} of {closure.WorstRaw:F0}, priced {closure.Worst:F0}" : "")
                        + $", and ×{BotHarrow.ClearFit:F2} of it is past the {island:F0} the island's strongest company of {BotSquad.MaxSize} brings"
                    );
                }

                continue;
            }

            if (ask * BotHarrow.ClearFit > within)
            {
                Outmatched++;
                outmatched = true;

                continue;
            }

            square = stand;
            asked = ask;
            onRoad = OnRoad(quad);
            closed = inClosure;
            dead = inClosure ? Math.Max(quad.Deaths, closure.Deaths) : quad.Deaths;

            return quad;
        }

        // Every square passed over: for the strength within call, counted with the asks found short at the muster; for anything
        // else, named.
        if (outmatched)
        {
            TooWeak++;
        }
        else
        {
            Untaken++;
        }

        return null;
    }

    /// <summary>
    /// The way to a square is not known to be shut: a gate joins it to where the fighter stands, the river is not between them
    /// (<see cref="BotHarrow.Reaches"/>, the levy's own test), and the reach ledger has not proved it sealed. The clearing's own
    /// test, from where the fighter stands (03.10.2026): the Baron's (<see cref="BotHarrower.Takeable"/>) judges the road from
    /// Britain, where his levy musters, and weighs the ground against a levy of lone bots, where a clearing's company is sized
    /// to the square (<see cref="Asks"/>).
    /// </summary>
    private static bool Reachable(Map map, Point3D from, Point3D stand) =>
        BotGates.Joined(map, from, stand)
        && BotHarrow.Reaches(map, from, stand, count: false)
        && BotReach.Ask(map, from, stand, BotArrival.Within(BotHarrow.Side / 3)) != BotReachVerdict.Sealed;

    /// <summary>A place with the ground settled under it, or zero.</summary>
    private static Point3D Settled(Map map, Point3D at) =>
        BotStep.Settle(map, at.X, at.Y, out var z) ? new Point3D(at.X, at.Y, z) : Point3D.Zero;

    /// <summary>
    /// What a square asks of a clearing's company, in <c>BotThreat.Power</c>'s units: the record of what has killed there
    /// (<c>BotQuad.Muscle</c>), the fight standing there now (<c>BotPeril.Hostility</c>), the strongest thing alive in the ground
    /// the clearing walks at its kind's price in our lives (<see cref="Strongest"/>), and — for a quarantine — the strongest thing
    /// recorded killing in it, priced the same (<see cref="BotBarred.Closure.Worst"/>); whichever is most. The price is the
    /// figure a company withdraws by (<c>BotSquad.Reckon</c>), so a company of <see cref="BotHarrow.ClearFit"/> times this does
    /// not withdraw from anything it was counted against (03.10.2026).
    /// </summary>
    public static double Asks(Map map, Point3D stand, BotBarred.Closure? closure)
    {
        var asked = Math.Max(BotQuad.Muscle(map, stand), BotPeril.Hostility(map, stand));

        asked = Math.Max(asked, Strongest(map, stand, closure != null ? BotBarred.QuarantineHalf : BotHarrow.Side / 2, out _, company: true));

        if (closure is { } c)
        {
            asked = Math.Max(asked, c.Worst);
        }

        return asked;
    }

    /// <summary>How long one look at a box's strongest creature stands. Ten seconds, as <c>BotPeril.HostileMs</c>.</summary>
    public static int StrongestMs { get; set; } = 10000;

    private static readonly Dictionary<(int Map, int X, int Y, int Half, bool Company), (double Priced, BaseCreature Worst, long Tick)> _strongest = [];

    /// <summary>
    /// The strongest wild creature alive within <paramref name="half"/> tiles each way of <paramref name="centre"/> that sets upon
    /// whoever comes near (as <c>BotPeril.Hostility</c> counts them: not the ones that fight only when struck), off guarded
    /// ground, at its kind's price in our lives (<c>BotQuarry.Priced</c>), with the creature. Nought on empty ground. Kept per
    /// cell of eight tiles for <see cref="StrongestMs"/>. Asked by the clearing's choice and by <see cref="BotBarred.Lift"/>.
    /// <paramref name="company"/>: at the kind's company price (<c>BotCompanyRecord.Price</c>, 03.10.2026), as the clearing's ask
    /// weighs it for the company it raises; the lift and the harrow's win ask whether the ground is safe for a lone bot, at the
    /// island's price.
    /// </summary>
    public static double Strongest(Map map, Point3D centre, int half, out BaseCreature worst, bool fresh = false, bool company = false)
    {
        worst = null;

        if (map == null || map == Map.Internal || half <= 0)
        {
            return 0.0;
        }

        var key = (map.MapID, centre.X >> 3, centre.Y >> 3, half, company);
        var now = Core.TickCount;

        // A lift and a clearing's win read the ground now, not a look up to ten seconds old (review of clear12, F5).
        if (!fresh && _strongest.TryGetValue(key, out var held) && now - held.Tick < StrongestMs)
        {
            worst = held.Worst;

            return held.Priced;
        }

        var most = 0.0;

        foreach (var creature in map.GetMobilesInRange<BaseCreature>(centre, half))
        {
            if (creature is not { Deleted: false, Alive: true, Controlled: false, Summoned: false, IsDeadBondedPet: false } || creature is BaseVendor
                || creature.FightMode is FightMode.None or FightMode.Aggressor or FightMode.Evil
                || Math.Abs(creature.X - centre.X) > half || Math.Abs(creature.Y - centre.Y) > half
                || creature.Region?.IsPartOf<GuardedRegion>() == true)
            {
                continue;
            }

            var priced = company ? BotCompanyRecord.Priced(creature) : BotQuarry.Priced(creature);

            if (priced > most)
            {
                most = priced;
                worst = creature;
            }
        }

        if (_strongest.Count >= 4096)
        {
            _strongest.Clear();
        }

        _strongest[key] = (most, worst, now);

        return most;
    }

    /// <summary>How long the island's strongest company is kept between reckonings. A minute: bots grow by the hour.</summary>
    public static int IslandMs { get; set; } = 60000;

    private static double _island;

    private static long _islandTick;

    private static int _islandMap = -1;

    private static readonly List<double> _powers = [];

    /// <summary>
    /// What the island's strongest company could bring: the <see cref="BotSquad.MaxSize"/> strongest bots alive on this map that
    /// may be levied (not producers), busy or not. A square asking at <see cref="BotHarrow.ClearFit"/> more than this is beyond
    /// anybody, and its quarantine stays closed (03.10.2026).
    /// </summary>
    public static double Island(Map map)
    {
        var now = Core.TickCount;

        if (map != null && map.MapID == _islandMap && now - _islandTick < IslandMs)
        {
            return _island;
        }

        _powers.Clear();

        var bots = BotPopulation.Bots;

        for (var i = 0; i < bots.Count; i++)
        {
            var bot = bots[i];

            if (bot is { Deleted: false, Alive: true } && bot.Map == map && bot.Class is { } klass && klass.Role != BotRole.Producer
                && bot is IBotAlly { AbleToFight: true })
            {
                _powers.Add(BotThreat.Power(bot));
            }
        }

        _powers.Sort();

        var sum = 0.0;

        for (int i = _powers.Count - 1, taken = 0; i >= 0 && taken < BotSquad.MaxSize; i--, taken++)
        {
            sum += _powers[i];
        }

        _powers.Clear();

        _island = sum;
        _islandTick = now;
        _islandMap = map?.MapID ?? -1;

        return sum;
    }

    /// <summary>
    /// A quarantine nobody is sent to, said in the log once (03.10.2026: squares nobody can win stay closed and are said
    /// plainly).
    /// </summary>
    private static void Say(BotBarred.Closure closure, string why)
    {
        if (_saidClosed.Count >= 1024)
        {
            _saidClosed.Clear();
        }

        if (!_saidClosed.Add((closure.X1, closure.Y1, closure.X2, closure.Y2)))
        {
            return;
        }

        SaidClosed++;

        logger.Information(
            "Quarantine ({X1},{Y1})-({X2},{Y2}) stays closed to the clearings: {Why}; {Deaths} have died in it",
            closure.X1,
            closure.Y1,
            closure.X2,
            closure.Y2,
            why,
            closure.Deaths
        );
    }

    /// <summary>Whether a square lies within <see cref="RoadWidth"/> of a drawn road between towns. Remembered <see cref="RoadMemoryMs"/>.</summary>
    private static bool OnRoad(BotQuad.Quad quad)
    {
        var key = (quad.Map?.MapID ?? -1, quad.X, quad.Y);
        var now = Core.TickCount;

        if (_roads.TryGetValue(key, out var held) && now - held.Tick < RoadMemoryMs)
        {
            return held.OnRoad;
        }

        var middle = quad.Middle;
        var width = Math.Max(1, RoadWidth);
        var onRoad = false;
        var roads = BotRoadbook.All;

        for (var r = 0; r < roads.Count && !onRoad; r++)
        {
            var points = roads[r].Points;

            for (var p = 0; p < points.Count; p++)
            {
                var point = points[p];

                if (Math.Abs(point.X - middle.X) <= width && Math.Abs(point.Y - middle.Y) <= width)
                {
                    onRoad = true;

                    break;
                }
            }
        }

        if (_roads.Count >= 4096)
        {
            _roads.Clear();
        }

        _roads[key] = (onRoad, now);

        return onRoad;
    }

    /// <summary>Said once: the first clearing offered on this shard. The marches say themselves, in <see cref="BotHarrow"/>.</summary>
    private static void Once(Mobile body, Point3D square, int dead, double asked, double supply, bool onRoad)
    {
        if (_said)
        {
            return;
        }

        _said = true;

        logger.Information(
            "{Name} the {Class} has been offered the first clearing on this shard: ({X}, {Y}), where {Dead} have died{Road}; the ground asks {Asked:F0} of strength and {Supply:F0} stands within call",
            body.Name,
            (body as BotMobile)?.Class?.Name,
            square.X,
            square.Y,
            dead,
            onRoad ? ", on a road between towns" : "",
            asked * BotHarrow.ClearFit,
            supply
        );
    }

    public static string Describe() =>
        !Running
            ? "clearings are off"
            : Asked == 0
                ? $"no fighter has been asked about a clearing yet ({NotAFighter} answers went to bots that are not fighters)"
                : $"{Asked} fighters asked ({NotAFighter} answers went to bots that are not fighters): {Offered} offered a square where {LeastDeaths} or more died in the last {RecentMs / 60000} minutes or a quarantine stands ({OfferedClosed} of them inside a standing quarantine, {OnRoads} on a road between towns), {Novices} were novices under {LeastSkill:F0}, {Held} already in a company, {Unfit} too hurt, {Resting} standing off a muster that failed, {Enough} asked with {MostAtOnce} clearings already standing, {Quiet} found no such square within {Reach} tiles, {ClearedLately} squares passed over as cleared within the hour, {TakenOver} as being harrowed already, {Barred} as lying in a standing bar, {Doomed} as lying in a quarantine the plague or a killer of {BotBarred.QuarantinePower:F0} declared, {Untakeable} as unreachable or damned past the grandmasters the island has, {Beyond} as asking at ×{BotHarrow.ClearFit:F2} past what the island's strongest company of {BotSquad.MaxSize} brings, {Outmatched} as asking more than the strongest company the bots within call make (the next one tried), {Untaken} asks passed every square within reach over for something other than strength, {SquareResting} squares passed over as resting after a muster on them came up short, {TooWeak} asks found the bots within call bringing less than ×{BotHarrow.ClearFit:F2} what the square chosen, or every square they could reach, asks, {Unmanned} fewer than {BotHarrow.Least} within call, {ClosedMuster} would have mustered inside closed ground; {BotHarrow.ClearMarches} marched ({BotHarrow.ClearClosed} of them into a standing quarantine; {BotHarrow.ClearResized} musters called past the leader's first reckoning for the strength asked), {BotHarrow.ClearEmptied} emptied their ground, {BotHarrow.ClearTimedout} ran out of time on it, {BotHarrow.ClearWon} left it with nothing past {BotBarred.EmptyBelow:F0} at its kind's price, {BotHarrow.ClearUndermanned} came up short at the bell, {BotHarrow.ClearWithdrew} withdrew from what they could not beat, {BotHarrow.ClearReasked} asked more at the bell than at the offer, {BotHarrow.ClearDoomed} given up at the bell for the plague, {BotHarrow.ClearEnough} levies stopped with the strength asked in the company, {BotHarrow.ClearKilled} things killed, {BotQuad.DiedAfterClearing} deaths on cleared ground within {BotQuad.AfterMs / 60000} minutes; {BotHarrow.QuarantinesLifted} quarantines ({BotHarrow.ClearLifted} of them by clearings) and {BotHarrow.BoxesLifted} closed boxes lifted with the ground; {SaidClosed} quarantines said to stay closed; {Census()}";

    /// <summary>
    /// The quarantines standing, by who could clear them: asking no more than the island's strongest company brings at
    /// <see cref="BotHarrow.ClearFit"/>, asking more, declared for the plague or a killer of <see cref="BotBarred.QuarantinePower"/>,
    /// or underground (the delves' ground, not the clearings'). Reckoned every summary, as <see cref="Asks"/> reckons a square.
    /// </summary>
    private static string Census()
    {
        var map = Map.Felucca;

        if (map == null)
        {
            return "no quarantine reckoned";
        }

        var island = Island(map);
        int standing = 0, winnable = 0, beyond = 0, dire = 0, under = 0;

        foreach (var closure in BotBarred.Closures())
        {
            standing++;

            var middle = closure.Middle;

            if (BotDungeon.Under(middle))
            {
                under++;

                continue;
            }

            if (closure.Dire)
            {
                dire++;

                continue;
            }

            var stand = Settled(map, middle);

            if (Asks(map, stand == Point3D.Zero ? middle : stand, closure) * BotHarrow.ClearFit > island)
            {
                beyond++;
            }
            else
            {
                winnable++;
            }
        }

        return standing == 0
            ? "no quarantine stands"
            : $"of the {standing} quarantines standing, {winnable} ask no more than the island's strongest company of {BotSquad.MaxSize} brings at ×{BotHarrow.ClearFit:F2} ({island:F0}), {beyond} ask more and stay closed, {dire} were declared for the plague or a killer of {BotBarred.QuarantinePower:F0} and stay closed, {under} lie underground";
    }

    public static void Forget()
    {
        _said = false;
        _roads.Clear();
        Asked = 0;
        NotAFighter = 0;
        Novices = 0;
        Held = 0;
        Unfit = 0;
        Resting = 0;
        Enough = 0;
        Quiet = 0;
        TakenOver = 0;
        ClearedLately = 0;
        Untakeable = 0;
        TooWeak = 0;
        Unmanned = 0;
        Offered = 0;
        OnRoads = 0;
        OfferedClosed = 0;
        Barred = 0;
        Doomed = 0;
        Beyond = 0;
        Outmatched = 0;
        Untaken = 0;
        SaidClosed = 0;
        _saidClosed.Clear();
        _closed.Clear();
        _strongest.Clear();
        _islandTick = 0;
        _islandMap = -1;
        SquareResting = 0;
        ClosedMuster = 0;
        _restingSquares.Clear();
    }
}
