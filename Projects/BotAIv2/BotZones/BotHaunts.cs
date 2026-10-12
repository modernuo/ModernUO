using System;
using System.Collections.Generic;
using System.Diagnostics;
using Server.Engines.Spawners;
using Server.Items;
using Server.Logging;
using Server.Mobiles;
using Server.Regions;

namespace Server.BotAI.V2;

/// <summary>
/// One kind of creature as the zones know it: whether it attacks on sight, casts, how far it sees, whether it can leave the
/// water, and how strong one is.
///
/// <para>
/// <b>Read off a living one when there is one, and built once when there is not.</b> A living creature carries its own fight
/// mode, mind and sight, and that is the truth about it; a kind nobody has seen alive yet — every creature of its spawners
/// dead — is asked of <see cref="BotDungeon.Traits"/>, which builds one specimen, reads it and destroys it in the same breath,
/// the same specimen <see cref="BotDungeon.Might"/> already builds for the delve, so a kind is never built twice.
/// </para>
/// </summary>
public sealed class BotZoneKind
{
    public BotZoneKind(Type type, string name = null)
    {
        Type = type;
        Name = name ?? type.Name;
    }

    public Type Type { get; }

    public string Name { get; }

    /// <summary>A name the spawners use for it, for building a specimen when none is alive.</summary>
    public string Spawned { get; set; }

    /// <summary>Whether its traits have been read, from the living or from a specimen.</summary>
    public bool Known { get; set; }

    public bool FromLife { get; set; }

    /// <summary>The engine would not build it: a constructor wanting arguments, or not a creature after all.</summary>
    public bool Unbuildable { get; set; }

    /// <summary>Attacks on sight: <c>Closest</c>, <c>Strongest</c> or <c>Weakest</c>. <c>None</c>, <c>Aggressor</c> and <c>Evil</c> do not come for a bot first.</summary>
    public bool Aggressive { get; set; }

    public bool Caster { get; set; }

    public int Perception { get; set; } = 16;

    /// <summary>
    /// How far its blow lands from, in tiles: a caster's spell, an archer's bow; nought for a creature that has to close
    /// (08.10.2026). Its aggro reach is the larger of this and its sight: it picks a target only within its sight, so a spell
    /// that lands farther than it sees (a short-sighted caster) is what widens it. See <see cref="BotHaunts.StrikeOf"/>.
    /// </summary>
    public int Strike { get; set; }

    /// <summary>Cannot walk and can swim: a sea creature, which never sets foot on the land a bot walks.</summary>
    public bool Water { get; set; }

    /// <summary>
    /// Can swim, walking or not. A swimmer that can also walk — a water elemental — is the sea's when its spawner stands in the
    /// water and the land's when it stands ashore (07.10.2026): the same test the sweep makes of a living one by where it stands.
    /// Until then a sea camp of krakens and water elementals was painted as a deadly land zone whose patrol never held anybody.
    /// </summary>
    public bool Swims { get; set; }

    /// <summary>Cannot walk and cannot swim: it stays where it grew, and reaches only as far as it strikes.</summary>
    public bool Rooted { get; set; }

    /// <summary>A shopkeeper, an escort, a healer or anything the engine makes invulnerable: not a creature of any zone.</summary>
    public bool Townsfolk { get; set; }

    /// <summary>The mean strength of the ones measured (<see cref="BotThreat.Power"/>), at most <see cref="MostPowerSamples"/> of them.</summary>
    public double Power => _powerCount == 0 ? 0.0 : _powerSum / _powerCount;

    public const int MostPowerSamples = 32;

    private double _powerSum;

    private int _powerCount;

    public void AddPower(double power)
    {
        if (power <= 0.0 || _powerCount >= MostPowerSamples)
        {
            return;
        }

        _powerSum += power;
        _powerCount++;
    }

    /// <summary>Takes the traits off a creature of the kind.</summary>
    public void ReadFrom(BaseCreature c)
    {
        Aggressive = BotHaunts.AttacksFirst(c.FightMode);
        Caster = c.AI == AIType.AI_Mage;
        Perception = c.RangePerception;
        Strike = BotHaunts.StrikeOf(c);
        Water = c.CantWalk && c.CanSwim;
        Swims = c.CanSwim;
        Rooted = c.CantWalk && !c.CanSwim;
        Townsfolk = BotHaunts.IsTownsfolk(c);
        Known = true;
        FromLife = true;
    }
}

/// <summary>One entry of a spawner: a kind and how many of it the spawner keeps.</summary>
public readonly record struct BotHauntEntry(BotZoneKind Kind, int Entry, int Max, int Probability);

/// <summary>
/// One spawner as a haunt: where its creatures live by declaration, what they are, and what the shard has seen of them awake.
///
/// <para>
/// <b>The declaration is a promise about a disc; the observation is what the creatures actually did.</b> The spawner says its
/// creatures may wander <see cref="Radius"/> tiles from it. Where the ground is awake, the scan records where they really
/// were (<see cref="BotZoneRaster"/>) and how far from home (<see cref="ObservedRange"/>); once <see cref="Confidence"/> is
/// high, the build shrinks the declared disc to the observed reach and the observed shape wins. A creature on sleeping ground stands where it
/// was spawned and proves nothing about its patrol, so only awake sightings count here.
/// </para>
/// </summary>
public sealed class BotHaunt
{
    public BotHaunt(BaseSpawner spawner, long now)
    {
        Spawner = spawner;
        _sampledTick = now;
    }

    public BaseSpawner Spawner { get; }

    public Point3D At { get; set; }

    /// <summary>The declared walking range, in tiles: the larger of the spawner's home and walking ranges.</summary>
    public int Radius { get; set; }

    /// <summary>How many creatures the spawner keeps when full.</summary>
    public int Count { get; set; }

    public bool Running { get; set; }

    /// <summary>Inside a town's region: passive creatures here are the town's cats and dogs, not a hunting ground.</summary>
    public bool InTown { get; set; }

    public BotHauntEntry[] Entries { get; set; } = [];

    /// <summary>Marked by the survey pass that last found the spawner; a haunt no pass found is dropped at the end of it.</summary>
    public bool Found { get; set; }

    /// <summary>Its index in the list handed to the last build, for the creatures that belong to it.</summary>
    public int Index { get; set; } = -1;

    /// <summary>Whether the spawner's own tile is water, and the tile that was read for it (read once; a spawner seldom moves).</summary>
    public bool Wet { get; set; }

    public Point3D WetAt { get; set; } = new(-1, -1, 0);

    /// <summary>Seconds its ground was seen awake, continuously sampled (see <see cref="Sample"/>).</summary>
    public double AwakeSeconds { get; set; }

    /// <summary>Awake sightings of its creatures.</summary>
    public long Samples { get; set; }

    /// <summary>The farthest from home one of its creatures has been seen awake, in tiles, never past the declaration.</summary>
    public int ObservedRange { get; set; }

    /// <summary>
    /// <b>How far its creatures really go (08.10.2026).</b> Every calm awake sighting — no combatant, out of war mode — counted
    /// by its distance from home in tiles up to <see cref="Radius"/> + <see cref="CountedBeyond"/> (a sighting farther off
    /// counts at the cap), each weighing one and halved every <see cref="BotZones.ReachHalfLifeMinutes"/>. The analysis of 08.10
    /// (deaths30) found 258 of 420 observed land zones at their declared radius: <see cref="ObservedRange"/> is clipped there.
    /// <see cref="ReachAt"/> reads a high percentile of it, and the build lets a camp claim the cells it was seen patrolling out
    /// to there. Calm only (both reviews of 08.10): a chase is where the bot led it, and counting chases made the map a slow
    /// pursuit overlay that painted the road a bot fled down and then agreed with the next death on it.
    /// </summary>
    private float[] _reach;

    private long _reachTick;

    /// <summary>The faded weight of the sightings counted for <see cref="ReachAt"/>, as of the last sighting or reading.</summary>
    public double ReachWeight { get; private set; }

    public DateTime LastAwake { get; set; } = DateTime.MinValue;

    /// <summary>How many times the tour has stood here, and when it last did.</summary>
    public int Toured { get; set; }

    public DateTime TouredAt { get; set; } = DateTime.MinValue;

    private long _sampledTick;

    private bool _wasAwake;

    /// <summary>
    /// How far the shape is learned: half from the seconds its ground has been seen awake against
    /// <see cref="BotZones.ConfidentSeconds"/>, half from the sightings against <see cref="BotZones.ConfidentSamples"/>. Both
    /// halves, because either alone lies: a thousand sightings of one creature that never moved in a minute of waking say
    /// little of a patrol, and ten minutes awake with a single sighting say little of a pack.
    /// </summary>
    public double Confidence =>
        0.5 * Math.Min(1.0, AwakeSeconds / Math.Max(1, BotZones.ConfidentSeconds))
        + 0.5 * Math.Min(1.0, Samples / (double)Math.Max(1, BotZones.ConfidentSamples));

    /// <summary>
    /// What a creature of it was doing when the scan passed it: awake or asleep, how far from home, and whether calm (no
    /// combatant, out of war mode), which alone counts for the reach.
    /// </summary>
    public void Sample(long now, bool awake, int away, bool calm)
    {
        if (awake)
        {
            // Awake time is the gap since the last sample only when that sample was awake too, and only while the gap is short
            // enough to be one stretch of waking: two awake sightings an hour apart are not an hour of patrol.
            var gap = now - _sampledTick;

            if (_wasAwake && gap > 0 && gap <= BotZones.AwakeGapMs)
            {
                AwakeSeconds += gap / 1000.0;
            }

            Samples++;
            LastAwake = DateTime.UtcNow;

            // Never past the declaration: the observed range can only shrink the disc, and a creature seen 748 tiles from home
            // (the memory file of 02.10.2026, a chase across the island) says nothing of its spawner's patrol. See BotZoneBuild.
            if (away > ObservedRange)
            {
                ObservedRange = Math.Min(away, Radius);
            }

            // How far past it they go on their own is counted apart, capped and fading; a chase is not counted at all.
            if (calm)
            {
                AddReach(now, away, 1f);
            }
        }

        _wasAwake = awake;
        _sampledTick = now;
    }

    public string Key => $"{At.X},{At.Y},{At.Z}";

    /// <summary>
    /// The distance from home within which <see cref="BotZones.ReachPercentile"/> of the faded sightings fall, in tiles, at most
    /// <see cref="Radius"/> + <see cref="CountedBeyond"/> (the build caps it at <see cref="BotZones.ReachBeyond"/>); -1 while they
    /// weigh less than <see cref="BotZones.ReachLeast"/>, and for a spawner too wide to paint.
    /// </summary>
    public int ReachAt(long now)
    {
        Fade(now);

        var bins = _reach;

        if (bins == null || ReachWeight < Math.Max(1, BotZones.ReachLeast))
        {
            return -1;
        }

        var want = ReachWeight * Math.Clamp(BotZones.ReachPercentile, 0.0, 1.0);
        var sum = 0.0;

        for (var d = 0; d < bins.Length; d++)
        {
            sum += bins[d];

            if (sum >= want)
            {
                return d;
            }
        }

        return bins.Length - 1;
    }

    /// <summary>
    /// What a restart brought back, as the counted sightings themselves, each bin faded by the time the shard was down. Only the
    /// bins: a percentile point put back as all of its weight would hold the reach out where it was for the many half-lives it
    /// takes new sightings to outweigh it (the reviews of 08.10), and each restart would ratchet it outward.
    /// </summary>
    public void Restore(float[] bins, double fade, long now)
    {
        if (bins == null || fade <= 0.0)
        {
            return;
        }

        for (var d = 0; d < bins.Length; d++)
        {
            if (bins[d] > 0f)
            {
                AddReach(now, d, (float)(bins[d] * fade));
            }
        }
    }

    /// <summary>A file holding only a reach point and its weight: put back as at most <see cref="BotZones.ReachLeast"/>, a hint and no more.</summary>
    public void Seed(int reach, double weight, long now)
    {
        if (reach >= 0 && weight > 0.0)
        {
            AddReach(now, reach, (float)Math.Min(weight, Math.Max(1, BotZones.ReachLeast)));
        }
    }

    /// <summary>A copy of the counted sightings faded to now, for the memory file; null when nothing is counted.</summary>
    public float[] ReachBins(long now)
    {
        Fade(now);

        return _reach == null || ReachWeight <= 0.0 ? null : (float[])_reach.Clone();
    }

    /// <summary>Tiles past the declared range the sightings are counted out to, whatever <see cref="BotZones.ReachBeyond"/> paints.</summary>
    public const int CountedBeyond = 32;

    private void AddReach(long now, int away, float weight)
    {
        var length = Radius + Math.Max(CountedBeyond, BotZones.ReachBeyond) + 1;

        if (Radius > BotZones.DeclaredMost)
        {
            return;
        }

        if (_reach == null || _reach.Length != length)
        {
            // New, or the spawner's range changed under it: what was counted against the old range is dropped.
            _reach = new float[length];
            _reachTick = now;
            ReachWeight = 0.0;
        }

        Fade(now);

        _reach[Math.Clamp(away, 0, length - 1)] += weight;
        ReachWeight += weight;
    }

    /// <summary>Halves the sightings every <see cref="BotZones.ReachHalfLifeMinutes"/>, brought up to date at most once a minute.</summary>
    private void Fade(long now)
    {
        var bins = _reach;
        var elapsed = now - _reachTick;

        if (bins == null || elapsed < 60000)
        {
            return;
        }

        _reachTick = now;

        var factor = (float)Math.Pow(0.5, elapsed / 60000.0 / Math.Max(1, BotZones.ReachHalfLifeMinutes));
        var weight = 0.0;

        for (var d = 0; d < bins.Length; d++)
        {
            bins[d] *= factor;
            weight += bins[d];
        }

        ReachWeight = weight;
    }
}

/// <summary>What the scan last knew about one living creature.</summary>
public sealed class BotZoneSeen
{
    public BotZoneKind Kind;

    public BotHaunt Haunt;

    public bool Awake;

    public long Tick;

    /// <summary>Its own strength, read when first seen and again every <see cref="BotHaunts.RepowerEvery"/> sightings.</summary>
    public float Power;

    public int Sightings;

    /// <summary>
    /// <b>Where a wanderer has been (08.10.2026).</b> For an attacker with no spawner or out past its spawner's range: its last
    /// places, a ring of <see cref="BotHaunts.TrailMost"/>, a new point only once it has moved a cell, each with the tick it was
    /// last seen there. The build paints a wanderer over where it walked in the last <see cref="BotZones.WanderMinutes"/>, not
    /// on the one tile it stood on at the last build: the analysis of 08.10 found a wandering spider's victim four tiles off its
    /// paint. Null for a creature that never wandered.
    /// </summary>
    public short[] TrailX;

    public short[] TrailY;

    public long[] TrailTick;

    /// <summary>Points in the ring, and the slot of the newest.</summary>
    public int TrailCount;

    public int TrailHead;

    /// <summary>Its place now: a new point, unless it is within a cell of the newest one, which is only freshened.</summary>
    public void Walked(int x, int y, long now)
    {
        if (TrailX == null)
        {
            var most = Math.Max(1, BotHaunts.TrailMost);

            TrailX = new short[most];
            TrailY = new short[most];
            TrailTick = new long[most];
            TrailHead = -1;
        }

        if (TrailCount > 0 && Math.Max(Math.Abs(TrailX[TrailHead] - x), Math.Abs(TrailY[TrailHead] - y)) < BotZoneState.Cell)
        {
            TrailTick[TrailHead] = now;

            return;
        }

        TrailHead = (TrailHead + 1) % TrailX.Length;
        TrailX[TrailHead] = (short)x;
        TrailY[TrailHead] = (short)y;
        TrailTick[TrailHead] = now;
        TrailCount = Math.Min(TrailCount + 1, TrailX.Length);
    }
}

/// <summary>
/// The spawners of the bots' home map read as haunts, the kinds of creature they keep, and the register of living creatures
/// the scan has passed.
///
/// <para>
/// <b>Read in strips, a strip a beat.</b> <c>map.GetItemsInBounds&lt;BaseSpawner&gt;</c> over one column of the map is a
/// fraction of a millisecond; the whole map at once is the nine milliseconds <see cref="BotLairs"/> spends on the island
/// alone, and the dungeons and the lost lands double it. So the survey walks the map a strip per beat, and walks it again
/// every <see cref="ResurveyMs"/> to find spawners added or removed, which on this shard is nearly never.
/// </para>
/// </summary>
public static class BotHaunts
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotHaunts));

    /// <summary>Tiles in one strip of the survey.</summary>
    public static int StripTiles { get; set; } = 256;

    /// <summary>How often the spawners are read again, for any added or removed.</summary>
    public static int ResurveyMs { get; set; } = 1800000;

    /// <summary>Unknown kinds built and measured at most in one beat. Each is one construction and one delete.</summary>
    public static int MeasurePerBeat { get; set; } = 2;

    /// <summary>A creature's own strength is read again after this many sightings, for a creature whose health has grown.</summary>
    public static int RepowerEvery { get; set; } = 16;

    /// <summary>Places at most in a wanderer's trail (<see cref="BotZoneSeen.Walked"/>).</summary>
    public static int TrailMost { get; set; } = 32;

    /// <summary>A creature the scan has not passed for this long is dropped from the register: dead, gone or moved away.</summary>
    public static int StaleMs { get; set; } = 180000;

    public static Map Map { get; private set; }

    /// <summary>Whether a whole pass over the map has finished since the start.</summary>
    public static bool Surveyed { get; private set; }

    public static int Surveys { get; private set; }

    /// <summary>Loop milliseconds the last whole pass cost, strips summed.</summary>
    public static double SurveyMs { get; private set; }

    /// <summary>Spawners read in the last pass that keep a creature, that keep none (reagents, scenery), and that are stopped.</summary>
    public static int Spawners => _all.Count;

    public static int NoCreature { get; private set; }

    public static int Stopped { get; private set; }

    /// <summary>Names on spawners the engine could not resolve to a creature.</summary>
    public static int UnknownNames { get; private set; }

    /// <summary>Kinds measured by building a specimen, and kinds that would not build.</summary>
    public static long Measured { get; private set; }

    public static long Unbuildable { get; private set; }

    /// <summary>Kinds a spawner names that are neither seen alive nor measured yet, as of the last beat.</summary>
    public static int Pending { get; private set; }

    public static IReadOnlyList<BotHaunt> All => _all;

    public static IReadOnlyCollection<BotZoneKind> Kinds => _kinds.Values;

    /// <summary>Living creatures in the register.</summary>
    public static int Registered => _seen.Count;

    private static readonly List<BotHaunt> _all = [];

    private static readonly Dictionary<BaseSpawner, BotHaunt> _bySpawner = [];

    private static readonly Dictionary<Type, BotZoneKind> _kinds = [];

    private static readonly Dictionary<string, Type> _named = new(StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<BaseCreature, BotZoneSeen> _seen = [];

    private static readonly List<BaseCreature> _gone = [];

    private static readonly List<BotHauntEntry> _entries = [];

    private static int _strip;

    private static bool _passing;

    private static long _passedTick;

    private static double _passMs;

    private static int _passNoCreature;

    private static int _passStopped;

    public static void Open(Map map, long now)
    {
        Forget();
        Map = map;

        // Due at once: the first beat starts the first pass, by the same path every later pass takes.
        _passedTick = now - ResurveyMs;
    }

    /// <summary>Whether a mode attacks a bot on sight.</summary>
    public static bool AttacksFirst(FightMode mode) => mode is FightMode.Closest or FightMode.Strongest or FightMode.Weakest;

    /// <summary>
    /// How far a creature's blow lands from (08.10.2026): a caster's spell at the range the engine targets spells at
    /// (<c>IRangedSpell.TargetRange</c>, <see cref="BotZones.SpellReach"/>), a bow or crossbow in its hands at the weapon's own
    /// range; nought otherwise, and nought for all with <see cref="BotZones.SpellReach"/> at nought. A breath reaches as far as
    /// the creature sees, which its sight already counts.
    /// </summary>
    public static int StrikeOf(BaseCreature c)
    {
        if (BotZones.SpellReach <= 0)
        {
            return 0;
        }

        if (c.AI == AIType.AI_Mage)
        {
            return BotZones.SpellReach;
        }

        return c.Weapon is BaseRanged ranged ? Math.Max(0, ranged.MaxRange) : 0;
    }

    /// <summary>Whether a creature belongs to the towns' life rather than to any zone.</summary>
    public static bool IsTownsfolk(BaseCreature c) => c is BaseVendor or BaseEscortable || c.IsInvulnerable;

    /// <summary>One strip of the survey, when a pass is under way or due. True when it did any work.</summary>
    public static bool Step(long now)
    {
        var map = Map;

        if (map == null)
        {
            return false;
        }

        if (!_passing)
        {
            if (now - _passedTick < ResurveyMs)
            {
                return false;
            }

            _passing = true;
            _strip = 0;
            _passMs = 0.0;
            _passNoCreature = 0;
            _passStopped = 0;

            for (var i = 0; i < _all.Count; i++)
            {
                _all[i].Found = false;
            }
        }

        var began = Stopwatch.GetTimestamp();
        var side = Math.Max(16, StripTiles);
        var x = _strip * side;

        if (x < map.Width)
        {
            Strip(map, new Rectangle2D(x, 0, Math.Min(side, map.Width - x), map.Height), now);
            _strip++;
        }

        _passMs += Stopwatch.GetElapsedTime(began).TotalMilliseconds;

        if (_strip * side < map.Width)
        {
            return true;
        }

        // The pass is done: what it did not find is gone.
        for (var i = _all.Count - 1; i >= 0; i--)
        {
            var haunt = _all[i];

            if (haunt.Found && haunt.Spawner is { Deleted: false })
            {
                continue;
            }

            haunt.Index = -1;
            _bySpawner.Remove(haunt.Spawner);
            _all.RemoveAt(i);
        }

        _passing = false;
        _passedTick = now;
        NoCreature = _passNoCreature;
        Stopped = _passStopped;
        SurveyMs = _passMs;
        Surveys++;

        if (!Surveyed)
        {
            Surveyed = true;

            logger.Information(
                "Zones: the spawners of {Map} were read in {Ms:F1}ms of the loop over {Strips} strips: {Haunts} keep creatures of {Kinds} kinds, {None} keep none, {Stopped} are stopped, {Unknown} names are no creature",
                map,
                SurveyMs,
                _strip,
                _all.Count,
                _kinds.Count,
                NoCreature,
                Stopped,
                UnknownNames
            );
        }

        return true;
    }

    private static void Strip(Map map, Rectangle2D bounds, long now)
    {
        foreach (var spawner in map.GetItemsInBounds<BaseSpawner>(bounds))
        {
            if (spawner is not { Deleted: false } || spawner.Map != map)
            {
                continue;
            }

            _entries.Clear();

            var list = spawner.Entries;

            if (list != null)
            {
                for (var i = 0; i < list.Count; i++)
                {
                    var entry = list[i];
                    var kind = KindNamed(entry?.SpawnedName);

                    if (kind == null || (kind.Townsfolk && kind.Known))
                    {
                        continue;
                    }

                    _entries.Add(new BotHauntEntry(kind, i, Math.Max(1, entry.SpawnedMaxCount), Math.Max(1, entry.SpawnedProbability)));
                }
            }

            if (_entries.Count == 0)
            {
                _passNoCreature++;

                continue;
            }

            if (!_bySpawner.TryGetValue(spawner, out var haunt))
            {
                haunt = new BotHaunt(spawner, now);
                _bySpawner[spawner] = haunt;
                _all.Add(haunt);
            }

            haunt.Found = true;
            haunt.At = spawner.Location;
            haunt.Radius = Math.Max(0, Math.Max(spawner.HomeRange, spawner.WalkingRange));
            haunt.Count = Math.Max(1, spawner.Count);
            haunt.Running = spawner.Running;
            haunt.Entries = _entries.ToArray();
            haunt.InTown = Region.Find(spawner.Location, map)?.IsPartOf<TownRegion>() == true;

            if (!haunt.Running)
            {
                _passStopped++;
            }
        }
    }

    /// <summary>The kind a spawner's name stands for, or null when it names no creature.</summary>
    public static BotZoneKind KindNamed(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        name = name.Trim();

        if (!_named.TryGetValue(name, out var type))
        {
            type = AssemblyHandler.FindTypeByName(name);

            if (type != null && !typeof(BaseCreature).IsAssignableFrom(type))
            {
                type = null;
            }

            if (type == null)
            {
                UnknownNames++;
            }

            _named[name] = type;
        }

        if (type == null)
        {
            return null;
        }

        var kind = KindOf(type);
        kind.Spawned ??= name;

        return kind;
    }

    public static BotZoneKind KindOf(Type type)
    {
        if (!_kinds.TryGetValue(type, out var kind))
        {
            kind = new BotZoneKind(type);
            _kinds[type] = kind;
        }

        return kind;
    }

    /// <summary>
    /// Builds and measures kinds nobody has seen alive, at most <see cref="MeasurePerBeat"/> a beat. True when it did any work.
    /// </summary>
    public static bool Measure()
    {
        var done = 0;
        var pending = 0;

        foreach (var kind in _kinds.Values)
        {
            if (kind.Known || kind.Unbuildable || kind.Spawned == null)
            {
                continue;
            }

            if (done >= MeasurePerBeat)
            {
                pending++;

                continue;
            }

            done++;

            var might = BotDungeon.Might(kind.Spawned);

            if (!BotDungeon.Traits(kind.Spawned, out var traits))
            {
                kind.Unbuildable = true;
                Unbuildable++;

                continue;
            }

            kind.AddPower(might);
            kind.Aggressive = AttacksFirst(traits.Mode);
            kind.Caster = traits.AI == AIType.AI_Mage;
            kind.Perception = traits.Perception;

            // A specimen's weapon is not read here, a caster's spell is: an archer learns its bow when first seen alive.
            kind.Strike = traits.AI == AIType.AI_Mage ? Math.Max(0, BotZones.SpellReach) : 0;
            kind.Water = traits.CantWalk && traits.CanSwim;
            kind.Swims = traits.CanSwim;
            kind.Rooted = traits.CantWalk && !traits.CanSwim;
            kind.Townsfolk = traits.Townsfolk;
            kind.Known = true;
            Measured++;
        }

        Pending = pending;

        return done > 0;
    }

    public static BotHaunt Of(BaseSpawner spawner) =>
        spawner != null && _bySpawner.TryGetValue(spawner, out var haunt) ? haunt : null;

    /// <summary>
    /// The scan passed a creature: its kind learned from it, its entry in the register brought up to date, and its haunt told
    /// whether it was awake and how far from home. Returns its kind, or null for a creature no zone counts.
    /// </summary>
    public static BotZoneKind Note(BaseCreature c, bool awake, long now)
    {
        var kind = KindOf(c.GetType());

        if (!kind.FromLife)
        {
            kind.ReadFrom(c);
        }

        if (kind.Townsfolk || IsTownsfolk(c))
        {
            return null;
        }

        if (!_seen.TryGetValue(c, out var seen))
        {
            seen = new BotZoneSeen { Kind = kind };
            _seen[c] = seen;
        }

        if (seen.Sightings++ % Math.Max(1, RepowerEvery) == 0)
        {
            var power = BotThreat.Power(c);

            seen.Power = (float)power;
            kind.AddPower(power);
        }

        seen.Awake = awake;
        seen.Tick = now;

        var haunt = c.Spawner is BaseSpawner spawner ? Of(spawner) : null;
        var away = 0;

        seen.Haunt = haunt;

        if (haunt != null)
        {
            var home = c.Home == Point3D.Zero ? haunt.At : c.Home;

            away = Math.Max(Math.Abs(c.X - home.X), Math.Abs(c.Y - home.Y));
            haunt.Sample(now, awake, away, c.Combatant == null && !c.Warmode);
        }

        // A wanderer's trail (08.10.2026): an attacker of the land with no spawner, or out past its spawner's declared range. The
        // build decides which are wanderers by its own ranges; this keeps the places of every one that might be.
        if ((haunt == null || away > haunt.Radius) && AttacksFirst(c.FightMode) && !(c.CantWalk && c.CanSwim))
        {
            seen.Walked(c.X, c.Y, now);
        }

        return kind;
    }

    /// <summary>
    /// The register, pruned of the dead, the deleted, the tamed and the long unseen, handed to <paramref name="each"/> one living
    /// creature at a time.
    /// </summary>
    public static void Living(Map map, long now, Action<BaseCreature, BotZoneSeen> each)
    {
        _gone.Clear();

        foreach (var (c, seen) in _seen)
        {
            if (c is not { Deleted: false, Alive: true } || c.Map != map || c.Controlled || c.Summoned || now - seen.Tick > StaleMs)
            {
                _gone.Add(c);

                continue;
            }

            each(c, seen);
        }

        for (var i = 0; i < _gone.Count; i++)
        {
            _seen.Remove(_gone[i]);
        }

        _gone.Clear();
    }

    public static string Describe() =>
        Map == null
            ? "no map"
            : $"{_all.Count} spawners keep creatures ({NoCreature} keep none, {Stopped} stopped), {_kinds.Count} kinds ({Measured} built to be measured, {Unbuildable} would not build, {UnknownNames} names no creature), {_seen.Count} living creatures registered; the survey {(Surveyed ? $"took {SurveyMs:F1}ms over {Surveys} passes" : "is under way")}";

    public static void Forget()
    {
        Map = null;
        Surveyed = false;
        Surveys = 0;
        SurveyMs = 0.0;
        NoCreature = 0;
        Stopped = 0;
        UnknownNames = 0;
        Measured = 0;
        Unbuildable = 0;
        _all.Clear();
        _bySpawner.Clear();
        _kinds.Clear();
        _named.Clear();
        _seen.Clear();
        _gone.Clear();
        _passing = false;
        _strip = 0;
    }
}
