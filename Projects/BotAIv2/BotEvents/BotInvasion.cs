using System;
using System.Collections.Generic;
using Server.Logging;
using Server.Mobiles;
using Server.Regions;

namespace Server.BotAI.V2;

/// <summary>
/// The invasion (07.10.2026, Patrick's first idea: "every few hours a wave comes out of a dungeon onto the road by a town: an
/// announcement to all, a militia musters, the town pays a reward"). Every <see cref="EveryMs"/>, jittered, one town whose fighters
/// are near enough to answer is chosen (the one invaded longest ago; the next when it has no place), a place on one of its roads
/// <see cref="PastEdge"/>–<see cref="PastEdgeMost"/> tiles past its guards, and a dungeon to come out of; the reward is held out of the city's purse at once; the town crier calls it.
///
/// <para>
/// <b>The creatures come when the militia does, sized to the militia that came.</b> Nothing is put on the road at the call: a wave
/// dropped on a road nobody defends kills whoever walks it, and deaths per bot-hour must not rise. The town's fighters are offered the
/// militia (<see cref="BotMilitiaCall"/>): the patrol the shard already has (<c>BotSweep</c>) — a captain calls the free fighters
/// round it, gathers, marches, holds the ground and fights there as a company under every rule company16 wrote (the choice gate,
/// the muster at the enemy, the pull-back at two deaths in a minute). When a militia company's captain stands within
/// <see cref="ArriveReach"/> of the place the first wave walks out, priced at the company record's price of each kind
/// (<c>BotCompanyRecord.Priced</c>) to at most <see cref="Odds"/> — never above one — of the strength the company brings
/// (<c>BotSquad.Able</c>), no single creature over <see cref="SingleShare"/> of it, casters left in the dungeon. The next wave waits
/// for the last to fall and is sized to what is left of the company; <see cref="Waves"/> waves at most.
/// </para>
///
/// <para>
/// <b>Paid by the head, out of the hold.</b> Each creature that falls to a bot (or to its pet) pays its killer
/// <see cref="HeadPay"/> — the hold over the heads the raid expects — straight into its account, so the gold lands on the work the
/// bot was doing when it struck. What is not earned goes back to the purse when the raid ends: repelled, the militia gone from the
/// field for <see cref="GoneMs"/>, nobody come in <see cref="MusterMs"/>, or <see cref="LifeMs"/> after the first wave, when whatever
/// still stands is taken off the island.
/// </para>
/// </summary>
public static class BotInvasion
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotInvasion));

    public static bool Running { get; set; } = true;

    /// <summary>How often a town is invaded. Three hours, jittered by <see cref="Jitter"/> either way.</summary>
    public static int EveryMs { get; set; } = 10800000;

    public static double Jitter { get; set; } = 0.25;

    /// <summary>How long after a boot the first invasion may come.</summary>
    public static int FirstMs { get; set; } = 2700000;

    /// <summary>What the city holds out of its purse for one invasion's heads, and the least worth calling one for.</summary>
    public static int Purse { get; set; } = 1500;

    public static int LeastPurse { get; set; } = 300;

    /// <summary>The share of the militia company's strength one wave is priced at. Never above one (company16: ×1.0).</summary>
    public static double Odds { get; set; } = 0.8;

    /// <summary>No single creature priced above this share of the company's strength.</summary>
    public static double SingleShare { get; set; } = 0.4;

    /// <summary>Waves at most, and creatures in one wave at most.</summary>
    public static int Waves { get; set; } = 3;

    public static int MostHeads { get; set; } = 8;

    /// <summary>
    /// How far out of the town's square the place lies when no road gives one (the fallback ring). life18a: the road's place is measured
    /// from where the road leaves guarded ground (<see cref="PastEdge"/>–<see cref="PastEdgeMost"/>), not from the square.
    /// </summary>
    public static int Near { get; set; } = 60;

    public static int Far { get; set; } = 100;

    /// <summary>
    /// life18a, 07.10.2026: how far along the road past the town's guards the place lies. Britain's square is 300 tiles from the east
    /// edge of its region and Yew's guards reach 275 tiles out, so a band measured from the square fell inside the guards.
    /// </summary>
    public static int PastEdge { get; set; } = 20;

    public static int PastEdgeMost { get; set; } = 70;

    /// <summary>The farthest from the square a place may lie (straight), so the militia that lives round the square can march to it.</summary>
    public static int MostOut { get; set; } = 400;

    /// <summary>Tiles between the places tried along a road (the road's points are a walk's turning points, ~20 tiles apart).</summary>
    public static int RoadStep { get; set; } = 4;

    /// <summary>Lines at most a forced call (<see cref="CallNow"/>) writes about the candidates it refused.</summary>
    public static int MostProbeLines { get; set; } = 60;

    /// <summary>How far round a town's square its fighters are counted and offered the militia.</summary>
    public static int TownReach { get; set; } = 120;

    /// <summary>The fewest free fighters near a town for it to be invaded.</summary>
    public static int LeastMilitia { get; set; } = 4;

    /// <summary>How long the town waits for a militia company to reach the place before the creatures go back underground.</summary>
    public static int MusterMs { get; set; } = 1200000;

    /// <summary>How long after the first wave anything still standing is taken off the island. Forty minutes.</summary>
    public static int LifeMs { get; set; } = 2400000;

    /// <summary>How long the militia may be away from the field before the raid is called off and the field cleared.</summary>
    public static int GoneMs { get; set; } = 120000;

    /// <summary>The quiet between waves.</summary>
    public static int RestMs { get; set; } = 20000;

    /// <summary>How near the place a militia captain must stand for a wave to come out; how far beyond it the creatures appear.</summary>
    public static int ArriveReach { get; set; } = 20;

    public static int Spread { get; set; } = 10;

    /// <summary>How near the field a bot's death is counted as the invasion's.</summary>
    public static int DeathReach { get; set; } = 30;

    /// <summary>Militia companies at most for one invasion, and the wait after a captain's call failed before another is offered.</summary>
    public static int MostCompanies { get; set; } = 1;

    public static int CallRetryMs { get; set; } = 60000;

    /// <summary>
    /// Captains one invasion may call at most. A captain whose call raises fewer than a patrol's least fails its work, and a failure
    /// is a failure on the work line: three, and the town stops asking.
    /// </summary>
    public static int MostCalls { get; set; } = 3;

    /// <summary>What the militia is reckoned at per minute before the ledger has seen one: a hunt's.</summary>
    public static double MilitiaPrior { get; set; } = 60.0;

    // ---- counters ----------------------------------------------------------------------------------------------------------

    public static long Called { get; private set; }

    public static long NoTown { get; private set; }

    public static long NoPlace { get; private set; }

    /// <summary>life18a: towns passed over for want of a place while another town took the invasion (was: the call failed).</summary>
    public static long PassedOver { get; private set; }

    /// <summary>life18a: tiles tried for a place, and refused, since the boot; the refusals by reason in <see cref="_refusals"/>.</summary>
    public static long PlacesTried { get; private set; }

    public static long PlacesRefused { get; private set; }

    private static readonly Dictionary<string, int> _refusals = new(StringComparer.Ordinal);

    public static long NoPurse { get; private set; }

    public static long Answered { get; private set; }

    public static long Unanswered { get; private set; }

    public static long Repelled { get; private set; }

    public static long LeftField { get; private set; }

    public static long Expired { get; private set; }

    public static long TooWeak { get; private set; }

    public static long WavesRaised { get; private set; }

    public static long Heads { get; private set; }

    public static long Kills { get; private set; }

    public static long OthersKilled { get; private set; }

    public static long Cleared { get; private set; }

    public static long Deaths { get; private set; }

    public static long Captains { get; private set; }

    public static long CaptainsFailed { get; private set; }

    public static long Companies { get; private set; }

    public static long Offered { get; private set; }

    public static long GoldPaid { get; private set; }

    public static long GoldBack { get; private set; }

    /// <summary>The share of the company's strength the waves were actually priced at, summed, for the mean.</summary>
    private static double _pricedShare;

    /// <summary>One invasion: the town, the place, the hold, the militia and the creatures.</summary>
    public sealed class Raid
    {
        public BotTowns.Town Town;

        public Map Map;

        public Point3D Site;

        public Point3D Lair;

        public string From;

        public int Pool;

        public int Paid;

        public int HeadPay;

        public long Began;

        public long Fought;

        public long ClearedTick;

        public long MilitiaSeen;

        public long LastCall;

        public int Calls;

        public int Wave;

        public bool Fighting => Fought != 0;

        public bool Over;

        public readonly List<BaseCreature> Alive = [];

        public readonly List<BotSquad> Squads = [];

        public readonly Dictionary<Serial, int> Kills = [];

        public readonly HashSet<Serial> Dead = [];

        public int Heads;

        public int Killed;

        public string Bearing => BotSeasons.Bearing(Town, Site);
    }

    private static Raid _raid;

    private static long _nextTick;

    private static bool _seeded;

    private static readonly Dictionary<string, long> _lastAt = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The invasion standing now, or null.</summary>
    public static Raid Now => _raid;

    /// <summary>The invasion a town is calling its militia to now: one with no wave finished yet, not over.</summary>
    public static Raid Calling => _raid is { Over: false } r && (r.Wave < Waves || r.Alive.Count > 0) ? r : null;

    /// <summary>The creatures of the standing raid, for the store: so a restart takes them off the island.</summary>
    public static IEnumerable<BaseCreature> Standing()
    {
        if (_raid == null)
        {
            yield break;
        }

        for (var i = 0; i < _raid.Alive.Count; i++)
        {
            yield return _raid.Alive[i];
        }
    }

    /// <summary>What the standing raid still holds of the city's purse, for the store.</summary>
    public static int Holding => _raid == null ? 0 : Math.Max(0, _raid.Pool - _raid.Paid);

    public static void Beat(long now)
    {
        if (!_seeded)
        {
            _seeded = true;
            _nextTick = now + Math.Max(0, FirstMs);
        }

        if (_raid != null)
        {
            Run(_raid, now);

            return;
        }

        if (!Running || now - _nextTick < 0)
        {
            return;
        }

        Schedule(now);
        Begin(now);
    }

    private static void Schedule(long now)
    {
        var jitter = 1.0 + (Utility.RandomDouble() * 2.0 - 1.0) * Math.Clamp(Jitter, 0.0, 0.9);

        _nextTick = now + (long)(Math.Max(600000, EveryMs) * jitter);
    }

    /// <summary>Calls one now, by the clock or by hand (<c>dial BotInvasion.CallNow true</c>). Returns what happened.</summary>
    public static string Begin(long now)
    {
        if (_raid != null)
        {
            return $"an invasion stands already, {_raid.Bearing}";
        }

        var map = BotPopulation.Home;

        if (map == null || map == Map.Internal)
        {
            return "no home map";
        }

        var forced = _forced;
        _forced = false;

        var ranked = Choose(map);

        if (ranked.Count == 0)
        {
            NoTown++;

            logger.Information("No invasion this time: no town had {Least} free fighters within {Reach} tiles of its square to answer one", LeastMilitia, TownReach);

            return "no town could answer";
        }

        // life18a: every town that can answer is tried in turn, the one invaded longest ago first. Until now only the first was
        // tried, and since a town is stamped only when invaded, Britain (the strongest, never invaded) was chosen and refused at
        // every call of the night.
        BotTowns.Town town = null;
        var fighters = 0;
        var strength = 0.0;
        var site = Point3D.Zero;
        var passed = 0;
        _probeLines = 0;

        for (var i = 0; i < ranked.Count; i++)
        {
            if (Place(map, ranked[i].Town, forced, out site))
            {
                town = ranked[i].Town;
                fighters = ranked[i].Fighters;
                strength = ranked[i].Strength;

                break;
            }

            passed++;
        }

        if (town == null)
        {
            NoPlace++;

            logger.Information("No invasion this time: none of the {Towns} towns that could answer had open ground on its roads (since the boot: {Refusals})", ranked.Count, Tally(_refusals, 4));

            return "no place on any answering town's roads";
        }

        PassedOver += passed;

        var pool = BotSeasons.Hold(Purse, LeastPurse);

        if (pool <= 0)
        {
            NoPurse++;

            logger.Information("No invasion of {Town} this time: the city's purse holds {Purse}gp and keeps {Reserve}", town.Name, BotCity.Purse, BotSeasons.CityReserve);

            return "the city cannot pay for one";
        }

        var from = Dungeon();
        var dx = site.X - town.Square.X;
        var dy = site.Y - town.Square.Y;
        var length = Math.Max(1.0, Math.Sqrt(dx * dx + dy * dy));
        var lair = new Point3D(site.X + (int)(dx / length * Spread), site.Y + (int)(dy / length * Spread), site.Z);

        _raid = new Raid
        {
            Town = town, Map = map, Site = site, Lair = lair, From = from, Pool = pool, Began = now, MilitiaSeen = now
        };

        Called++;
        _lastAt[town.Name] = now;

        logger.Information(
            "An invasion out of {From} is called on {Town}'s road at ({X}, {Y}), {Bearing}: {Pool}gp held out of the city's purse for the heads; {Fighters} free fighters within {Reach} tiles bring {Strength:F0}; the creatures come out when a militia company reaches the place, each wave at most {Odds:P0} of what it brings",
            from,
            town.Name,
            site.X,
            site.Y,
            _raid.Bearing,
            pool,
            fighters,
            TownReach,
            strength,
            Math.Min(1.0, Odds)
        );

        BotSeasons.Announce(
            "invasion",
            map,
            town.Square,
            $"To arms, {town.Name}! Creatures out of {from} are on the road {_raid.Bearing}. The town calls its militia and pays {pool}gp for their heads."
        );

        return $"an invasion of {town.Name} out of {from}, {_raid.Bearing}, {pool}gp held";
    }

    /// <summary>Set by hand to call one at the next beat. A forced call writes every candidate place it refuses, and why.</summary>
    public static bool CallNow
    {
        get => false;
        set
        {
            if (value)
            {
                _nextTick = Core.TickCount;
                _forced = true;
            }
        }
    }

    private static bool _forced;

    private static int _probeLines;

    private readonly struct Candidate
    {
        public readonly BotTowns.Town Town;
        public readonly int Fighters;
        public readonly double Strength;
        public readonly long Last;

        public Candidate(BotTowns.Town town, int fighters, double strength, long last)
        {
            Town = town;
            Fighters = fighters;
            Strength = strength;
            Last = last;
        }
    }

    private static readonly List<Candidate> _ranked = [];

    /// <summary>
    /// The towns with <see cref="LeastMilitia"/> free fighters near, the one invaded longest ago first, the stronger on a tie
    /// (life18a: a list, so a town with no place passes the call to the next).
    /// </summary>
    private static List<Candidate> Choose(Map map)
    {
        _ranked.Clear();

        var towns = BotTowns.All;
        var counts = new int[towns.Count];
        var powers = new double[towns.Count];
        var bots = BotPopulation.Bots;

        for (var b = 0; b < bots.Count; b++)
        {
            var bot = bots[b];

            if (!BotSeasons.Fighter(bot) || bot.Map != map || bot is IBotSquadMember { Squad: not null })
            {
                continue;
            }

            for (var t = 0; t < towns.Count; t++)
            {
                if (towns[t].FromHome && bot.InRange(towns[t].Square, TownReach))
                {
                    counts[t]++;
                    powers[t] += BotThreat.Power(bot);

                    break;
                }
            }
        }

        for (var t = 0; t < towns.Count; t++)
        {
            if (counts[t] < LeastMilitia)
            {
                continue;
            }

            // A town never invaded since the boot is the oldest of all.
            var last = _lastAt.TryGetValue(towns[t].Name, out var at) ? at : long.MinValue;

            _ranked.Add(new Candidate(towns[t], counts[t], powers[t], last));
        }

        _ranked.Sort(static (a, b) => a.Last != b.Last ? a.Last.CompareTo(b.Last) : b.Strength.CompareTo(a.Strength));

        return _ranked;
    }

    private static readonly List<Point3D> _spots = [];

    /// <summary>
    /// Open ground on one of the town's roads, <see cref="PastEdge"/>–<see cref="PastEdgeMost"/> tiles walked past where the road
    /// leaves guarded ground and at most <see cref="MostOut"/> from the square; or, when no road gives one, anywhere
    /// <see cref="Near"/>–<see cref="Far"/> out of the square that the square can reach. A forced call writes each refusal.
    /// </summary>
    private static bool Place(Map map, BotTowns.Town town, bool forced, out Point3D site)
    {
        site = Point3D.Zero;
        _spots.Clear();
        _townRefusals.Clear();

        var roads = BotRoadbook.All;
        var step = Math.Max(1, RoadStep);
        var roadsOut = 0;

        for (var r = 0; r < roads.Count; r++)
        {
            var road = roads[r];
            var fromHere = string.Equals(road.From, town.Name, StringComparison.OrdinalIgnoreCase);

            if (!road.Drawn || road.Points.Count < 2
                || !fromHere && !string.Equals(road.To, town.Name, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            roadsOut++;

            // Walk the road out of this town: from its first point when it starts here, from its last when it ends here.
            var count = road.Points.Count;
            var walked = 0.0;
            var left = -1.0;

            for (var i = 0; i + 1 < count; i++)
            {
                var a = road.Points[fromHere ? i : count - 1 - i];
                var b = road.Points[fromHere ? i + 1 : count - 2 - i];
                var dx = b.X - a.X;
                var dy = b.Y - a.Y;
                var length = Math.Sqrt(dx * (double)dx + dy * (double)dy);
                var pieces = Math.Max(1, (int)(length / step));
                var beyond = false;

                for (var k = 0; k < pieces; k++)
                {
                    var x = a.X + (int)(dx * (double)k / pieces);
                    var y = a.Y + (int)(dy * (double)k / pieces);
                    var along = walked + length * k / pieces;

                    if (Region.Find(new Point3D(x, y, a.Z), map)?.IsPartOf<GuardedRegion>() == true)
                    {
                        // Inside a town's (or a field's) guards: the road has not left them yet.
                        left = -1.0;

                        continue;
                    }

                    if (left < 0.0)
                    {
                        left = along;
                    }

                    var past = along - left;

                    if (past < PastEdge)
                    {
                        continue;
                    }

                    if (past > PastEdgeMost)
                    {
                        beyond = true;

                        break;
                    }

                    PlacesTried++;

                    var ox = x - town.Square.X;
                    var oy = y - town.Square.Y;

                    if (ox * (double)ox + oy * (double)oy > MostOut * (double)MostOut)
                    {
                        Refuse(town, road, x, y, (int)past, $"over {MostOut} tiles from the square", forced);

                        continue;
                    }

                    var why = BotSeasons.Why(map, x, y, out var at);

                    if (why != null)
                    {
                        Refuse(town, road, x, y, (int)past, why, forced);

                        continue;
                    }

                    _spots.Add(at);
                }

                if (beyond)
                {
                    break;
                }

                walked += length;
            }
        }

        if (_spots.Count > 0)
        {
            site = _spots[Utility.Random(_spots.Count)];

            if (forced)
            {
                logger.Information(
                    "Invasion place for {Town}: {Places} open tiles on its {Roads} drawn roads {Least}–{Most} tiles past the guards; chose ({X}, {Y}); refused {Refused} ({Why})",
                    town.Name,
                    _spots.Count,
                    roadsOut,
                    PastEdge,
                    PastEdgeMost,
                    site.X,
                    site.Y,
                    SumOf(_townRefusals),
                    Tally(_townRefusals, 4)
                );
            }

            _spots.Clear();

            return true;
        }

        for (var tries = 0; tries < 40; tries++)
        {
            var angle = Utility.RandomDouble() * Math.PI * 2.0;
            var far = Near + Utility.RandomDouble() * Math.Max(0, Far - Near);
            var x = town.Square.X + (int)(Math.Cos(angle) * far);
            var y = town.Square.Y + (int)(Math.Sin(angle) * far);

            PlacesTried++;

            var why = BotSeasons.Why(map, x, y, out var at);

            if (why == null && BotReach.Ask(map, town.Square, at, BotArrival.Within(4), false) == BotReachVerdict.Sealed)
            {
                why = "sealed off from the square";
            }

            if (why != null)
            {
                Refuse(town, null, x, y, (int)far, why, forced);

                continue;
            }

            site = at;

            logger.Information(
                "Invasion place for {Town}: none on its {Roads} drawn roads ({Why}); took ({X}, {Y}), {Far:F0} tiles out of the square",
                town.Name,
                roadsOut,
                Tally(_townRefusals, 4),
                site.X,
                site.Y,
                far
            );

            return true;
        }

        logger.Information(
            "No invasion of {Town}: no open ground on its {Roads} drawn roads {Least}–{Most} tiles past its guards nor {Near}–{Far} out of its square; refused {Refused} ({Why})",
            town.Name,
            roadsOut,
            PastEdge,
            PastEdgeMost,
            Near,
            Far,
            SumOf(_townRefusals),
            Tally(_townRefusals, 4)
        );

        return false;
    }

    private static readonly Dictionary<string, int> _townRefusals = new(StringComparer.Ordinal);

    private static int SumOf(Dictionary<string, int> tally)
    {
        var sum = 0;

        foreach (var (_, n) in tally)
        {
            sum += n;
        }

        return sum;
    }

    /// <summary>A refused candidate: counted by its reason and, on a forced call, written out, one line each.</summary>
    private static void Refuse(BotTowns.Town town, BotRoadbook.Road road, int x, int y, int distance, string why, bool forced)
    {
        PlacesRefused++;

        _townRefusals[why] = _townRefusals.TryGetValue(why, out var n) ? n + 1 : 1;
        _refusals[why] = _refusals.TryGetValue(why, out var m) ? m + 1 : 1;

        if (!forced || _probeLines >= MostProbeLines)
        {
            return;
        }

        _probeLines++;

        if (road != null)
        {
            logger.Information(
                "Invasion place for {Town}: ({X}, {Y}) on the {From}–{To} road, {Past} tiles past the guards, refused: {Why}",
                town.Name,
                x,
                y,
                road.From,
                road.To,
                distance,
                why
            );
        }
        else
        {
            logger.Information(
                "Invasion place for {Town}: ({X}, {Y}), {Far} tiles out of the square off the roads, refused: {Why}",
                town.Name,
                x,
                y,
                distance,
                why
            );
        }
    }

    private static readonly List<KeyValuePair<string, int>> _tallySort = [];

    /// <summary>"12 guarded ground, 3 no ground to stand on": the commonest refusals first.</summary>
    private static string Tally(Dictionary<string, int> tally, int most)
    {
        if (tally.Count == 0)
        {
            return "none refused";
        }

        _tallySort.Clear();

        foreach (var pair in tally)
        {
            _tallySort.Add(pair);
        }

        _tallySort.Sort(static (a, b) => b.Value.CompareTo(a.Value));

        var text = "";

        for (var i = 0; i < _tallySort.Count && i < most; i++)
        {
            text += (i > 0 ? ", " : "") + $"{_tallySort[i].Value} {_tallySort[i].Key}";
        }

        _tallySort.Clear();

        return text;
    }

    /// <summary>A dungeon to come out of: one the survey found creatures in, or the hills.</summary>
    private static string Dungeon()
    {
        var deeps = BotDungeon.All;
        List<string> named = [];

        for (var i = 0; i < deeps.Count; i++)
        {
            if (deeps[i].Kinds.Count > 0)
            {
                named.Add(deeps[i].Name);
            }
        }

        return named.Count > 0 ? named[Utility.Random(named.Count)] : "the hills";
    }

    // ---- the militia ---------------------------------------------------------------------------------------------------------

    /// <summary>A captain took the town's call. Counted; another is not offered until this one's call has come to something.</summary>
    internal static void Took(Raid raid, Mobile captain)
    {
        Captains++;
        raid.Calls++;
        raid.LastCall = Core.TickCount;

        logger.Information(
            "{Name} answers {Town}'s call and raises the militia against the creatures out of {From}, {Bearing}",
            captain?.Name,
            raid.Town?.Name,
            raid.From,
            raid.Bearing
        );

        if (captain != null)
        {
            BotSeasons.Say(captain, "world", $"To me, {raid.Town?.Name}! We march on the creatures {raid.Bearing}.");
        }
    }

    /// <summary>The captain's company exists: watched for arrival and for leaving the field.</summary>
    internal static void Mustered(Raid raid, BotSquad squad)
    {
        if (squad == null || squad.Disbanded || raid.Squads.Contains(squad))
        {
            return;
        }

        raid.Squads.Add(squad);
        Companies++;
    }

    /// <summary>A captain's militia deed ended. A failed call before the company marched frees the town to offer another.</summary>
    internal static void Ended(Raid raid, Mobile captain, BotDoing how)
    {
        if (how.Kind == BotDoingKind.Failed)
        {
            CaptainsFailed++;
        }

        raid.LastCall = Core.TickCount;

        logger.Information("{Name}'s militia for {Town} ended: {How} — {Note}", captain?.Name, raid.Town?.Name, how.Kind, how.Note);
    }

    /// <summary>Whether the town may offer its call to another captain now.</summary>
    internal static bool Offers(Raid raid, long now)
    {
        Prune(raid);

        if (raid.Over || raid.Squads.Count >= Math.Max(1, MostCompanies) || raid.Calls >= Math.Max(1, MostCalls))
        {
            return false;
        }

        // A call taken is waited on: the company is being raised, or the captain's failure is a minute old.
        return raid.LastCall == 0 || now - raid.LastCall >= CallRetryMs;
    }

    internal static void Offer() => Offered++;

    private static void Prune(Raid raid)
    {
        for (var i = raid.Squads.Count - 1; i >= 0; i--)
        {
            var squad = raid.Squads[i];

            if (squad == null || squad.Disbanded || squad.Leader?.Self is not { Deleted: false, Alive: true })
            {
                raid.Squads.RemoveAt(i);
            }
        }
    }

    // ---- the field ---------------------------------------------------------------------------------------------------------------

    private static void Run(Raid raid, long now)
    {
        Prune(raid);
        Fallen(raid);

        BotSquad near = null;
        var nearest = int.MaxValue;
        var present = false;

        for (var i = 0; i < raid.Squads.Count; i++)
        {
            var lead = raid.Squads[i].Leader?.Self;

            if (lead == null || lead.Map != raid.Map)
            {
                continue;
            }

            var off = (int)lead.GetDistanceToSqrt(raid.Site);

            if (off <= ArriveReach * 2)
            {
                present = true;
            }

            if (off <= ArriveReach && off < nearest)
            {
                nearest = off;
                near = raid.Squads[i];
            }
        }

        if (present)
        {
            raid.MilitiaSeen = now;
        }

        if (!raid.Fighting)
        {
            if (near != null)
            {
                Answered++;
                raid.Fought = now;

                if (!Raise(raid, near, now))
                {
                    TooWeak++;
                    End(raid, now, "the militia came, but brought too little for the least of them to come out against it");
                }

                return;
            }

            if (now - raid.Began >= MusterMs)
            {
                Unanswered++;
                End(raid, now, $"no militia company reached the place in {MusterMs / 60000} minutes, and the creatures went back underground");
            }

            return;
        }

        CountDead(raid);

        if (now - raid.Fought >= LifeMs)
        {
            Expired++;
            End(raid, now, $"{LifeMs / 60000} minutes after the first wave the creatures still standing were taken off the island");

            return;
        }

        if (now - raid.MilitiaSeen >= GoneMs)
        {
            LeftField++;
            End(raid, now, $"the militia had been off the field for {GoneMs / 1000}s");

            return;
        }

        if (raid.Alive.Count > 0)
        {
            return;
        }

        if (raid.Wave >= Math.Max(1, Waves))
        {
            Repelled++;
            End(raid, now, "the militia threw back every wave");

            return;
        }

        if (near != null && now - raid.ClearedTick >= RestMs && !Raise(raid, near, now))
        {
            Repelled++;
            End(raid, now, "what was left of the militia was too weak for another wave, and it was not sent");
        }
    }

    private static readonly List<(Type Kind, double Priced)> _kinds = [];

    /// <summary>One wave, sized to the company that stands at the place. False when not even one creature fits its strength.</summary>
    private static bool Raise(Raid raid, BotSquad company, long now)
    {
        var able = company.Able();
        var budget = Math.Min(1.0, Math.Max(0.0, Odds)) * able;
        var single = Math.Max(0.0, SingleShare) * able;

        Kinds(raid, single);

        if (_kinds.Count == 0 || budget <= 0.0)
        {
            return false;
        }

        var priced = 0.0;
        var made = 0;
        var names = new List<string>();

        for (var tries = 0; tries < 40 && made < Math.Max(1, MostHeads); tries++)
        {
            var (kind, price) = _kinds[Utility.Random(_kinds.Count)];

            if (priced + price > budget)
            {
                continue;
            }

            if (Put(raid, kind) is not { } creature)
            {
                continue;
            }

            priced += price;
            made++;
            names.Add(creature.Name);
        }

        _kinds.Clear();

        if (made == 0)
        {
            return false;
        }

        raid.Wave++;
        raid.Heads += made;
        WavesRaised++;
        Heads += made;
        _pricedShare += able > 0.0 ? priced / able : 0.0;

        // The head's price set at the first wave: the hold over the heads the raid may yet put up.
        if (raid.HeadPay == 0)
        {
            raid.HeadPay = Math.Max(10, raid.Pool / Math.Max(1, made * Math.Max(1, Waves)));
        }

        logger.Information(
            "Wave {Wave} of the invasion of {Town} comes out of {From} at ({X}, {Y}): {Count} creatures ({Names}) priced {Priced:F0} against the {Able:F0} the company of {Leader} ({Members}) brings, {Share:P0}; {HeadPay}gp a head",
            raid.Wave,
            raid.Town.Name,
            raid.From,
            raid.Lair.X,
            raid.Lair.Y,
            made,
            string.Join(", ", names),
            priced,
            able,
            company.Leader?.Self?.Name,
            company.Count,
            able > 0 ? priced / able : 0,
            raid.HeadPay
        );

        BotSeasons.Announce(
            "invasion",
            raid.Map,
            raid.Lair,
            raid.Wave == 1
                ? $"They come! {made} out of {raid.From} on {raid.Town.Name}'s road: {raid.HeadPay}gp a head."
                : $"Wave {raid.Wave} on {raid.Town.Name}'s road: {made} more out of {raid.From}."
        );

        return true;
    }

    /// <summary>The kinds that may come out, each priced: the dungeon's own when the survey knows them, else the waves' list.</summary>
    private static void Kinds(Raid raid, double single)
    {
        _kinds.Clear();

        var deep = BotDungeon.Find(raid.From);

        if (deep != null)
        {
            for (var i = 0; i < deep.Kinds.Count; i++)
            {
                Try(AssemblyHandler.FindTypeByName(deep.Kinds[i]?.Trim()), single);
            }
        }

        if (_kinds.Count > 0)
        {
            return;
        }

        for (var i = 0; i < _fallback.Length; i++)
        {
            Try(_fallback[i], single);
        }
    }

    private static readonly Type[] _fallback =
    [
        typeof(Orc), typeof(Ratman), typeof(Skeleton), typeof(Zombie), typeof(HeadlessOne), typeof(OrcCaptain), typeof(Troll),
        typeof(Ettin), typeof(Ogre), typeof(Lizardman), typeof(Gnaw), typeof(Ghoul)
    ];

    /// <summary>A kind is asked once per wave on a probe that never enters the world: what it is, and its price.</summary>
    private static void Try(Type kind, double single)
    {
        if (kind == null || !typeof(BaseCreature).IsAssignableFrom(kind) || kind.IsAbstract)
        {
            return;
        }

        for (var i = 0; i < _kinds.Count; i++)
        {
            if (_kinds[i].Kind == kind)
            {
                return;
            }
        }

        BaseCreature probe = null;

        try
        {
            probe = Activator.CreateInstance(kind) as BaseCreature;
        }
        catch
        {
            // A kind with no plain constructor is not one the island can put on a road.
        }

        if (probe == null)
        {
            return;
        }

        var fits = probe.AI != AIType.AI_Mage && !probe.CantWalk && BotHaunts.AttacksFirst(probe.FightMode) && probe.Karma < 0
                   && !probe.IsParagon;
        var price = BotCompanyRecord.Priced(probe);

        probe.Delete();

        if (fits && price > 0.0 && price <= single)
        {
            _kinds.Add((kind, price));
        }
    }

    private static BaseCreature Put(Raid raid, Type kind)
    {
        Point3D at = Point3D.Zero;
        var found = false;

        // Round the lair first, beyond the company; round the place itself when the lair's ground will not hold them.
        for (var tries = 0; tries < 24 && !found; tries++)
        {
            var from = tries < 12 ? raid.Lair : raid.Site;
            var x = from.X + Utility.RandomMinMax(-4, 4);
            var y = from.Y + Utility.RandomMinMax(-4, 4);

            found = BotSeasons.Open(raid.Map, x, y, out at);
        }

        if (!found)
        {
            return null;
        }

        BaseCreature creature;

        try
        {
            creature = Activator.CreateInstance(kind) as BaseCreature;
        }
        catch
        {
            return null;
        }

        if (creature == null)
        {
            return null;
        }

        // On a short lead round the lair: a wave that wanders into Britain is the guards' fight, and one that wanders off along
        // the road is somebody else's death.
        creature.Home = raid.Lair;
        creature.RangeHome = Spread;
        creature.MoveToWorld(at, raid.Map);
        raid.Alive.Add(creature);

        return creature;
    }

    /// <summary>Whatever fell since the last beat: its killer paid by the head if it was one of ours, or its pet's master.</summary>
    private static void Fallen(Raid raid)
    {
        for (var i = raid.Alive.Count - 1; i >= 0; i--)
        {
            var creature = raid.Alive[i];

            if (creature is { Deleted: false, Alive: true })
            {
                continue;
            }

            raid.Alive.RemoveAt(i);
            raid.Killed++;

            if (raid.Alive.Count == 0)
            {
                raid.ClearedTick = Core.TickCount;
            }

            var killer = creature.LastKiller;

            if (killer is BaseCreature { Controlled: true, ControlMaster: { } master })
            {
                killer = master;
            }
            else if (killer is BaseCreature { Summoned: true, SummonMaster: { } summoner })
            {
                killer = summoner;
            }

            if (killer is not BotMobile bot)
            {
                OthersKilled++;

                continue;
            }

            Kills++;
            raid.Kills[bot.Serial] = raid.Kills.TryGetValue(bot.Serial, out var k) ? k + 1 : 1;

            var pay = Math.Min(raid.HeadPay, raid.Pool - raid.Paid);

            if (pay > 0 && BotSeasons.Disburse(bot, pay))
            {
                raid.Paid += pay;
                GoldPaid += pay;

                logger.Information(
                    "{Name} killed {Creature} of the invasion of {Town} and was paid {Pay}gp by the town ({Kills} heads of its own; {Left}gp of the hold left)",
                    bot.Name,
                    creature.Name,
                    raid.Town.Name,
                    pay,
                    raid.Kills[bot.Serial],
                    raid.Pool - raid.Paid
                );
            }
        }
    }

    private static void CountDead(Raid raid)
    {
        foreach (var m in raid.Map.GetMobilesInRange<BotMobile>(raid.Site, DeathReach + Spread))
        {
            if (!m.Alive && raid.Dead.Add(m.Serial))
            {
                Deaths++;

                logger.Information("{Name} fell on the field of the invasion of {Town}", m.Name, raid.Town.Name);
            }
        }
    }

    private static void End(Raid raid, long now, string why)
    {
        raid.Over = true;

        var gone = 0;

        for (var i = 0; i < raid.Alive.Count; i++)
        {
            if (raid.Alive[i] is { Deleted: false } creature)
            {
                creature.Delete();
                gone++;
            }
        }

        raid.Alive.Clear();
        Cleared += gone;

        var back = Math.Max(0, raid.Pool - raid.Paid);

        BotSeasons.Return(back);
        GoldBack += back;

        var best = "nobody";
        var most = 0;

        foreach (var (serial, kills) in raid.Kills)
        {
            if (kills > most && World.FindMobile(serial) is { } who)
            {
                most = kills;
                best = who.Name;
            }
        }

        logger.Information(
            "The invasion of {Town} is over after {Minutes:F0} minutes: {Why}; {Waves} waves, {Heads} creatures, {Killed} fell ({Ours} to ours), {Gone} taken away; {Dead} of ours fell on the field; {Paid}gp paid of {Pool}, {Back}gp back to the purse; most heads: {Best} ({Most})",
            raid.Town.Name,
            (now - raid.Began) / 60000.0,
            why,
            raid.Wave,
            raid.Heads,
            raid.Killed,
            Sum(raid.Kills),
            gone,
            raid.Dead.Count,
            raid.Paid,
            raid.Pool,
            back,
            best,
            most
        );

        BotSeasons.Announce(
            "invasion",
            raid.Map,
            raid.Town.Square,
            raid.Wave == 0
                ? $"The creatures out of {raid.From} have gone back underground; {raid.Town.Name}'s road is quiet."
                : $"{raid.Town.Name}'s militia held the road: {Sum(raid.Kills)} of {raid.Heads} slain, {raid.Paid}gp paid{(most > 0 ? $", {best} took {most}" : "")}."
        );

        _raid = null;
    }

    private static int Sum(Dictionary<Serial, int> kills)
    {
        var sum = 0;

        foreach (var (_, n) in kills)
        {
            sum += n;
        }

        return sum;
    }

    /// <summary>Called off by a reload or a stop: the creatures taken away and the hold handed back.</summary>
    public static void Stand()
    {
        if (_raid != null)
        {
            End(_raid, Core.TickCount, "the shard stopped it");
        }
    }

    public static string Describe() =>
        !Running
            ? "off"
            : $"{Called} called ({NoTown} times no town had {LeastMilitia} free fighters within {TownReach} tiles, {NoPlace} no place on any answering town's roads, {PassedOver} towns passed over for want of one, {NoPurse} the city could not hold {LeastPurse}gp; {PlacesTried} tiles tried for a place, {PlacesRefused} refused: {Tally(_refusals, 3)}), "
              + $"{Answered} answered by a militia and {Unanswered} not in {MusterMs / 60000} minutes; {Offered} calls offered, {Captains} captains took one, {CaptainsFailed} could not raise or hold a company, {Companies} companies marched; "
              + $"{WavesRaised} waves, {Heads} creatures, priced at {(WavesRaised > 0 ? _pricedShare / WavesRaised : 0):P0} of the company on average (cap {Math.Min(1.0, Odds):P0}); {Kills} killed by ours, {OthersKilled} by others, {Cleared} taken away; "
              + $"ended: {Repelled} repelled, {LeftField} the militia left the field, {Expired} on the {LifeMs / 60000}-minute clock, {TooWeak} the militia too weak for one; {Deaths} of ours fell on the fields; {GoldPaid}gp paid by the head, {GoldBack}gp back to the purse"
              + (_raid == null ? $"; next in {Math.Max(0, _nextTick - Core.TickCount) / 60000} minutes" : $"; now: {_raid.Town.Name}, {_raid.Bearing}, {(_raid.Fighting ? $"wave {_raid.Wave}, {_raid.Alive.Count} standing" : "the militia mustering")}, {_raid.Squads.Count} companies");

    public static void Forget()
    {
        _raid = null;
        _seeded = false;
        _lastAt.Clear();
        PassedOver = PlacesTried = PlacesRefused = 0;
        _refusals.Clear();
        Called = NoTown = NoPlace = NoPurse = Answered = Unanswered = Repelled = LeftField = Expired = TooWeak = 0;
        WavesRaised = Heads = Kills = OthersKilled = Cleared = Deaths = Captains = CaptainsFailed = Companies = Offered = 0;
        GoldPaid = GoldBack = 0;
        _pricedShare = 0;
    }
}
