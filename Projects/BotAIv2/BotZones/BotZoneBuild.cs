using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;

namespace Server.BotAI.V2;

/// <summary>
/// Turns what is known about the haunts, the living creatures, the observed patrols and the deaths into zones: which cells are
/// patrol, which are aggro fringe, what each zone holds, how strong it is against a bot, and its outline.
///
/// <para>
/// <b>Pure: it reads only its input and writes only its own scratch and the one back buffer it was handed.</b> That is what lets
/// <see cref="BotZones"/> run it off the loop when the machine has the cores for it (CLAUDE.md rules 3 and 10): nothing in here
/// touches a map, a mobile, a timer or a static the loop writes. The scratch arrays are static because one build runs at a
/// time — <see cref="BotZones"/> never starts a second before the first has published.
/// </para>
///
/// <para>
/// <b>Three layers, never merged across.</b> Land patrol of creatures that attack on sight (declared discs until observation
/// wins, the living where they stand, the cells seen patrolled awake); the sea's (creatures that cannot leave the water, only
/// where they are); and the passive animals' (hunting grounds), painted only where no attacker's patrol is. A sea serpent's
/// water never joins an orc camp on the beach into one zone, and a sea spawner that declares a walking range of 175 tiles
/// paints nothing: only its living creatures do.
/// </para>
///
/// <para>
/// <b>A zone is a camp of spawners, not a connected patch of paint (02.10.2026, build 389).</b> Until 388 a zone was every
/// cell of one layer joined eight ways, and the paint joined: 1,651 spawners' discs of up to 64 tiles, the cells where their
/// creatures were seen awake (with a cell's margin each way, a twelve-tile stripe along every chase a bot led across the
/// island) and the discs round the living fused into zone 25, "W of Trinsic", 1,224 × 1,920 tiles with 80 spawners, 1,198
/// creatures and Trinsic's own bank inside its outline, charged everywhere at the fight of its worst corner (22,935, ×20 a
/// bot); the roads went round the peninsula. Patrick, 02.10 00:20: "one of your zones has grown over half of Britain ... the
/// zone is information both for caution and for fighting and hunting." Now the land's attackers are grouped by their
/// spawners (<see cref="Group"/>): two spawners are one zone when their discs touch within
/// <see cref="BotZoneSettings.JoinGap"/> tiles and the zone they make stays within <see cref="BotZoneSettings.ZoneMostTiles"/>
/// across, nearest pairs first, so a fort of six spawners is one zone and a chain of camps along a road is a chain of zones.
/// Each zone paints only its own: its spawners' discs, the living of those spawners that are within range of home, and the
/// observed cells its spawners' ranges (plus <see cref="BotZoneSettings.ObservedSlack"/>) cover. A creature farther from home
/// than that (a chase, a lure) or with no spawner is a <em>wanderer</em>: a zone of its own where it stands
/// (<see cref="BotZone.Stray"/>), real danger and no patrol. An observed cell no spawner's range covers is a chase, and paints
/// nothing. Guarded ground is never painted: the guards kill what attacks there.
/// </para>
///
/// <para>
/// <b>Where the deaths said the map was wrong (08.10.2026, deaths30).</b> Of 21 deaths on clear ground, 14 began inside a camp's
/// painted aggro and ended 15–250 tiles out, the pursuer still on the bot; three were by wanderers whose paint lagged behind
/// them, one by an invasion wave, three by casters past a fringe of their sight. So a camp's patrol may grow past its declared
/// range to how far its creatures are really seen from home calm (<see cref="BotZoneHauntIn.Reach"/>, a high percentile, at most
/// <see cref="BotZoneSettings.ReachBeyond"/> past the declaration) — in the direction they were seen, not as a wider disc: past
/// the declaration a camp claims the observed cells out to its reach, and the cells are where its creatures walked. The grouping
/// into camps still goes by the declaration, so the zones and their numbers stand. A caster's or an archer's fringe is the larger
/// of its sight and its blow's reach (it picks a target only within its sight); a wanderer is painted
/// along where it walked in the last minutes (<see cref="BotZoneInput.Trail"/>, at most
/// <see cref="BotZoneSettings.WanderMostCells"/> cells); and an event's creatures are a zone of their own while they live
/// (<see cref="BotZoneInput.Events"/>). Guarded ground stays unpainted: town guards are the answer there, not the map.
/// </para>
///
/// <para>
/// <b>Strength is what can come at once, not the worst alone — and at a place, not over a peninsula.</b> The analysts' reading of
/// 3474 deaths on 29–30.09 found 78 companies of three to six strong bots killed by a single weak kind — snakes, zombies,
/// skeletons — in ground reckoned clear: the worst creature nearby was weak, and there were many of it. So a zone's threat is
/// taken round every living attacker in it: that one and all others within <see cref="BotZoneSettings.ConvergeTiles"/> of it,
/// the strongest in full and the rest at <see cref="BotThreat.Secondary"/>, and the zone's threat is the worst such place in
/// it — the hunter's question of the whole zone. The walker's question of one node is answered by the fight raster
/// (<see cref="BotZoneState.Fight"/>): the same convergence taken round every <em>cell</em>, over all the living wherever
/// their zone is, floored by what the cell's own spawners promise at one place when they are full.
/// </para>
/// </summary>
public static class BotZoneBuild
{
    private const byte Land = 1;

    private const byte Sea = 2;

    private const byte Passive = 4;

    /// <summary>The sea's grouping: a wide disc round each sea creature, so a school is one zone, never painted as danger itself.</summary>
    private const byte SeaGroup = 8;

    private static byte[] _mark;

    private static int[] _label;

    /// <summary>Per cell, the comp painting it last: the dedupe of one comp's cell list, which another comp may share.</summary>
    private static int[] _owner;

    /// <summary>The comp being painted into, whose cell list the paint appends to; null for a plane painted for labelling.</summary>
    private static Comp _painting;

    /// <summary>The guarded cells of the build under way, and how many a patrol or fringe would have covered.</summary>
    private static byte[] _guarded;

    private static int _guardedKept;

    /// <summary>Cells that took the land's bit for the first time in this build: the measure a wanderer's trail is capped by.</summary>
    private static int _marked;

    /// <summary>The fight raster's scratch: the sum and the worst of the living within reach of a cell, stamped per build.</summary>
    private static float[] _fightSum;

    private static float[] _fightMax;

    private static int[] _fightStamp;

    private static int _fightGen;

    private static readonly List<int> _touched = [];

    private static readonly List<int> _cells = [];

    private static readonly List<int> _queue = [];

    private static readonly Dictionary<int, int> _seaLabel = [];

    private static long _ringsTicks;

    private static double Ms(long from, long to) => (to - from) * 1000.0 / Stopwatch.Frequency;

    /// <summary>
    /// The outlines of the last build by the shape they were traced from, and those of this one. A zone whose cells did not
    /// change between two builds — most of them: declared discs and sleeping creatures — is not traced again; its rings are
    /// copied as the JSON they already were. Kept only for the shapes the last build used, so the cache never outgrows a map.
    /// </summary>
    private static Dictionary<ulong, byte[]> _rings = [];

    private static Dictionary<ulong, byte[]> _ringsNext = [];

    private static int _ringsTraced;

    private static int _ringsReused;

    /// <summary>A mask's rings written as a JSON array, traced only when this exact shape was not traced last time.</summary>
    private static void RingsJson(Utf8JsonWriter json, byte[] mask, int w, int h, int ox, int oy, BotZoneSettings s, int most)
    {
        var key = Fingerprint(mask, w, h, ox, oy, s, most);

        if (!_rings.TryGetValue(key, out var fragment) && !_ringsNext.TryGetValue(key, out fragment))
        {
            var began = Stopwatch.GetTimestamp();
            var rings = BotZoneShapes.Rings(mask, w, h, ox, oy, s.SimplifyTiles, most, s.MostRings, s.SmoothPasses);
            var buffer = new ArrayBufferWriter<byte>(64 + rings.Count * 256);

            using (var writer = new Utf8JsonWriter(buffer))
            {
                Rings(writer, rings);
            }

            fragment = buffer.WrittenSpan.ToArray();
            _ringsTicks += Stopwatch.GetTimestamp() - began;
            _ringsTraced++;
        }
        else
        {
            _ringsReused++;
        }

        _ringsNext[key] = fragment;
        json.WriteRawValue(fragment, true);
    }

    /// <summary>FNV-1a over the mask, its place and the numbers the tracing uses.</summary>
    private static ulong Fingerprint(byte[] mask, int w, int h, int ox, int oy, BotZoneSettings s, int most)
    {
        var hash = 14695981039346656037UL;

        void Mix(ulong value)
        {
            hash ^= value;
            hash *= 1099511628211UL;
        }

        Mix((ulong)w);
        Mix((ulong)h);
        Mix((ulong)ox);
        Mix((ulong)oy);
        Mix((ulong)most);
        Mix((ulong)s.MostRings);
        Mix((ulong)s.SmoothPasses);
        Mix((ulong)BitConverter.DoubleToInt64Bits(s.SimplifyTiles));

        for (var i = 0; i < mask.Length; i++)
        {
            hash ^= mask[i];
            hash *= 1099511628211UL;
        }

        return hash;
    }

    private sealed class Comp
    {
        public int Number;

        public byte Layer;

        public int Start;

        public int Count;

        public int MinX = int.MaxValue;

        public int MinY = int.MaxValue;

        public int MaxX = -1;

        public int MaxY = -1;

        public bool Kept;

        public readonly List<int> Haunts = [];

        public readonly List<int> Live = [];

        public double ObservedWeight;

        public int Alive;

        public int Aggressive;

        public int Casters;

        public int Converge;

        public int Max;

        public double Total;

        public double Worst;

        public double Threat;

        public double VsBot;

        /// <summary>The fight promised at one place by its spawners when full. See <see cref="BotZone.Declared"/>.</summary>
        public double Declared;

        public bool Empty;

        /// <summary>Wanderers' ground: attackers away from any spawner's range. See <see cref="BotZone.Stray"/>.</summary>
        public bool Stray;

        /// <summary>The event its creatures belong to, in words; null for a camp or a wanderer. See <see cref="BotZone.Event"/>.</summary>
        public string Event;

        /// <summary>The farthest a spawner of it paints past its declaration on what was seen; nought when none grew.</summary>
        public int Grown;

        /// <summary>
        /// Every attacker of it is rooted (a reaper, a whipping vine, a corpser): it stands where it grew, guarded ground or not —
        /// the vine at (1852, 2759) inside Trinsic killed six bots on 388 with no zone over it — so its cell and its reach are
        /// painted wherever they fall.
        /// </summary>
        public bool Rooted = true;

        public int Reach;

        public int DeclaredRange;

        public int ObservedRange;

        public double Confidence;

        public double AwakeSeconds;

        public long Samples;

        public DateTime LastSeen = DateTime.MinValue;

        public BotZoneLevel Level;

        public bool Promoted;

        public int Deaths;

        public byte[] Aggro;

        public int AX;

        public int AY;

        public int AW;

        public int AH;

        public int AggroCount;

        public int Index;

        public int Id;

        public int CenterX;

        public int CenterY;

        public Point3D Stand;

        public bool StandIsReal;

        public BotZoneKindCount[] Kinds = [];

        public int OtherKinds;

        public string Label;

        public string Why;
    }

    public static BotZoneState Build(BotZoneInput input)
    {
        var began = Stopwatch.GetTimestamp();
        var s = input.Settings;
        var w = input.Width;
        var h = input.Height;

        if (_mark == null || _mark.Length != w * h)
        {
            _mark = new byte[w * h];
            _label = new int[w * h];
            _owner = new int[w * h];
            _fightSum = new float[w * h];
            _fightMax = new float[w * h];
            _fightStamp = new int[w * h];
            _fightGen = 0;
        }

        List<Comp> comps = [];

        _width = w;
        _guarded = input.Guarded is { } guarded && guarded.Length == w * h ? guarded : null;
        _guardedKept = 0;
        _marked = 0;
        _painting = null;

        try
        {
            return Run(input, s, w, h, comps, began);
        }
        finally
        {
            for (var k = 0; k < _touched.Count; k++)
            {
                _mark[_touched[k]] = 0;
                _label[_touched[k]] = 0;
                _owner[_touched[k]] = 0;
            }

            _touched.Clear();
            _cells.Clear();
            _queue.Clear();
            _seaLabel.Clear();
            _painting = null;
            _guarded = null;
        }
    }

    /// <summary>
    /// A sea camp (07.10.2026, second reviews): a spawner standing in the water that keeps an attacker of the sea alone (cannot
    /// walk, can swim: krakens, serpents). Its swimmers that could walk are the sea's with it; without one, they are the land's.
    /// </summary>
    private static bool SeaCamp(BotZoneHauntIn haunt, BotZoneKindIn[] kinds)
    {
        if (!haunt.Wet)
        {
            return false;
        }

        for (var e = 0; e < haunt.Kinds.Length; e++)
        {
            var kind = kinds[haunt.Kinds[e]];

            if (kind.Water && kind.Aggressive)
            {
                return true;
            }
        }

        return false;
    }

    private static BotZoneState Run(BotZoneInput input, BotZoneSettings s, int w, int h, List<Comp> comps, long began)
    {
        var haunts = input.Haunts;
        var kinds = input.Kinds;
        var live = input.Live;
        var hostileLand = new bool[haunts.Length];
        var hostileSea = new bool[haunts.Length];
        var passive = new bool[haunts.Length];
        var rooted = new bool[haunts.Length];

        for (var i = 0; i < haunts.Length; i++)
        {
            var haunt = haunts[i];
            var anyAggressive = false;
            var anyLand = false;
            var allRooted = true;

            for (var e = 0; e < haunt.Kinds.Length; e++)
            {
                var kind = kinds[haunt.Kinds[e]];

                // A swimmer kept by a spawner in the water beside a creature of the sea alone is the sea's, whether or not it
                // could walk (07.10.2026): a camp of krakens and water elementals out at sea is no land zone, and water elementals
                // alone in an inland lake walk ashore and stay the land's. See BotZoneKind.Swims and SeaCamp.
                var wet = kind.WaterAt(SeaCamp(haunt, kinds));

                if (!wet)
                {
                    anyLand = true;
                }

                if (!kind.Aggressive)
                {
                    continue;
                }

                anyAggressive = true;

                if (wet)
                {
                    hostileSea[i] = true;
                }
                else
                {
                    hostileLand[i] = true;
                    allRooted &= kind.Rooted;
                }
            }

            passive[i] = !anyAggressive && anyLand && !haunt.InTown;
            rooted[i] = hostileLand[i] && allRooted;
        }

        // How far each spawner's creatures are met (08.10.2026): the declared range, grown to what was seen when a high share of
        // the sightings fall past it, never more than ReachBeyond past it. Its living count as its patrol out to here, and the
        // observed cells it claims too; the camps are still grouped by the declaration.
        var range = new int[haunts.Length];
        var grownHaunts = 0;

        for (var i = 0; i < haunts.Length; i++)
        {
            range[i] = Math.Max(haunts[i].Radius, Grown(haunts[i], rooted[i], s));

            if (range[i] > haunts[i].Radius && hostileLand[i] && haunts[i].Running && haunts[i].Radius <= s.DeclaredMost)
            {
                grownHaunts++;
            }
        }

        // The land's attackers, by camp: each group of spawners paints its own ground and nothing else (see the class note).
        var hauntComp = new int[haunts.Length];
        var liveComp = new int[live.Length];
        var landGroups = Group(haunts, hostileLand, rooted, s);
        var liveOf = new List<int>[landGroups.Count];
        var observedOf = new List<int>[landGroups.Count];
        var groupOf = new int[haunts.Length];

        Array.Fill(groupOf, -1);

        for (var g = 0; g < landGroups.Count; g++)
        {
            liveOf[g] = [];
            observedOf[g] = [];

            for (var k = 0; k < landGroups[g].Count; k++)
            {
                groupOf[landGroups[g][k]] = g;
            }
        }

        // The living of a spawner within its range (and a few steps) are its patrol; farther off they are wanderers (below).
        for (var i = 0; i < live.Length; i++)
        {
            var c = live[i];

            if (!c.Aggressive || c.Water || c.Haunt < 0 || groupOf[c.Haunt] < 0)
            {
                continue;
            }

            var haunt = haunts[c.Haunt];

            if (Math.Max(Math.Abs(c.X - haunt.X), Math.Abs(c.Y - haunt.Y)) <= range[c.Haunt] + s.StrayBeyond)
            {
                liveOf[groupOf[c.Haunt]].Add(i);
            }
        }

        // An observed cell belongs to the nearest spawner whose range covers it; one no range covers is a chase, and paints nothing.
        Attribute(input, haunts, range, hostileLand, groupOf, observedOf, s, w, h);

        for (var g = 0; g < landGroups.Count; g++)
        {
            var group = landGroups[g];
            var comp = new Comp { Number = comps.Count + 1, Layer = Land, Start = _cells.Count };

            comps.Add(comp);
            _painting = comp;

            // Declared discs until observation wins. Once it wins, the disc shrinks to the farthest its creatures were seen awake
            // from home and a margin — never past the declaration: a shape learned in ten minutes of waking may not have seen the
            // whole walk, and the envelope keeps a road from being laid through the part nobody happened to watch. Past the
            // declaration a camp grows only by the observed cells it claims out to its reach (08.10.2026, Attribute): where its
            // creatures were seen walking calm, in that direction, and not a disc the size of the reach all round.
            for (var k = 0; k < group.Count; k++)
            {
                var i = group[k];
                var haunt = haunts[i];

                comp.Haunts.Add(i);
                comp.Rooted &= rooted[i];
                hauntComp[i] = comp.Number;

                if (!haunt.Running || haunt.Radius > s.DeclaredMost)
                {
                    continue;
                }

                var radius = rooted[i] ? 0 : haunt.Radius;

                if (haunt.Confidence >= s.ObservedWins)
                {
                    radius = Math.Min(radius, haunt.ObservedRange + s.ObservedMargin * BotZoneState.Cell);
                }

                if (range[i] > haunt.Radius)
                {
                    comp.Grown = Math.Max(comp.Grown, range[i]);
                }

                Paint(haunt.X, haunt.Y, radius, Land, 0, w, h, rooted[i]);
            }

            var lives = liveOf[g];

            for (var k = 0; k < lives.Count; k++)
            {
                var c = live[lives[k]];
                var radius = c.Rooted ? 0 : haunts[c.Haunt].Radius > s.DeclaredMost ? s.LooseRadius : s.LiveRadius;

                Paint(c.X, c.Y, radius, Land, 0, w, h, c.Rooted);
                comp.Rooted &= c.Rooted;
                comp.Live.Add(lives[k]);
                liveComp[lives[k]] = comp.Number;
            }

            var observed = observedOf[g];

            for (var k = 0; k < observed.Count; k++)
            {
                var cell = input.Observed[observed[k]];

                Block(cell % w, cell / w, s.ObservedMargin, Land, w, h);
            }

            _painting = null;
        }

        // Events (08.10.2026): an invasion's waves, a relic's warden, The Shadow on a quay, each a zone of its own for as long as
        // any of its creatures lives — its field, the living where they stand and where they walked. The gather hands in only
        // events with someone alive, so the zone is gone the build after the last of them falls.
        var events = input.Events;
        var eventZones = 0;
        var trailCells = 0;

        for (var e = 0; e < events.Length; e++)
        {
            var ev = events[e];
            var comp = new Comp { Number = comps.Count + 1, Layer = Land, Start = _cells.Count, Stray = true, Event = ev.Label };

            comps.Add(comp);
            _painting = comp;
            Paint(ev.X, ev.Y, Math.Max(0, ev.Radius), Land, 0, w, h);

            for (var i = 0; i < live.Length; i++)
            {
                var c = live[i];

                if (c.Event != e || liveComp[i] != 0)
                {
                    continue;
                }

                comp.Live.Add(i);
                comp.Rooted &= c.Rooted;
                liveComp[i] = comp.Number;

                if (c.Aggressive && !c.Water)
                {
                    Paint(c.X, c.Y, c.Rooted ? 0 : s.LiveRadius, Land, 0, w, h, c.Rooted);

                    if (!c.Rooted)
                    {
                        trailCells += Trail(input, c, s.LiveRadius, s.WanderMostCells, w, h);
                    }
                }
            }

            _painting = null;
        }

        // Wanderers: attackers of no spawner, or away from theirs. Standing in a camp's paint they are that camp's; elsewhere they
        // are drawn where they stand and along where they walked in the last minutes (08.10.2026), and what that paint joins
        // into, eight ways, is a zone of its own.
        var strayFrom = comps.Count;

        for (var i = 0; i < live.Length; i++)
        {
            var c = live[i];

            if (!c.Aggressive || c.Water || liveComp[i] != 0)
            {
                continue;
            }

            var number = At(c.X, c.Y, Land, comps, w, h);

            if (number > 0)
            {
                comps[number - 1].Live.Add(i);
                comps[number - 1].Rooted &= c.Rooted;
                liveComp[i] = number;

                continue;
            }

            Paint(c.X, c.Y, c.Rooted ? 0 : s.LiveRadius, Land, 0, w, h, c.Rooted);

            if (!c.Rooted)
            {
                trailCells += Trail(input, c, s.LiveRadius, s.WanderMostCells, w, h);
            }
        }

        Label(Land, Land, comps, w, h);

        for (var k = strayFrom; k < comps.Count; k++)
        {
            comps[k].Stray = true;
        }

        for (var i = 0; i < live.Length; i++)
        {
            var c = live[i];

            if (c.Aggressive && !c.Water && liveComp[i] == 0)
            {
                var number = At(c.X, c.Y, Land, comps, w, h);

                if (number > 0)
                {
                    comps[number - 1].Live.Add(i);
                    comps[number - 1].Rooted &= c.Rooted;
                    liveComp[i] = number;
                }
            }
        }

        // The sea: only its living, only where they are. Grouped wide, so a school is one zone; its patrol is the narrow disc
        // round each creature, so a coast is not called a sea serpent's because one swims forty tiles off it.
        for (var i = 0; i < live.Length; i++)
        {
            var c = live[i];

            if (c.Aggressive && c.Water)
            {
                Paint(c.X, c.Y, s.WaterRadius, Sea, 0, w, h);
                Paint(c.X, c.Y, Math.Max(s.WaterRadius, s.WaterGroup), SeaGroup, 0, w, h);
            }
        }

        Label(SeaGroup, Sea, comps, w, h);

        // The hunting grounds, by camp as well, only where no attacker patrols.
        var passiveGroups = Group(haunts, passive, null, s);

        for (var g = 0; g < passiveGroups.Count; g++)
        {
            var group = passiveGroups[g];
            var comp = new Comp { Number = comps.Count + 1, Layer = Passive, Start = _cells.Count };

            comps.Add(comp);
            _painting = comp;

            for (var k = 0; k < group.Count; k++)
            {
                var i = group[k];
                var haunt = haunts[i];

                comp.Haunts.Add(i);
                hauntComp[i] = comp.Number;

                if (haunt.Running && haunt.Radius <= s.DeclaredMost)
                {
                    Paint(haunt.X, haunt.Y, haunt.Radius, Passive, Land, w, h);
                }
            }

            _painting = null;
        }

        for (var i = 0; i < live.Length; i++)
        {
            var c = live[i];

            if (c.Aggressive || c.Water)
            {
                continue;
            }

            if (c.Haunt >= 0 && (!passive[c.Haunt] || haunts[c.Haunt].Radius <= s.DeclaredMost))
            {
                continue;
            }

            Paint(c.X, c.Y, s.PassiveRadius, Passive, Land, w, h);
        }

        Label(Passive, Passive, comps, w, h);

        var painted = Stopwatch.GetTimestamp();

        // Who belongs where: the sea's by where they swim, the land's rest by what they stand in.
        for (var i = 0; i < live.Length; i++)
        {
            var c = live[i];

            if (liveComp[i] != 0)
            {
                continue;
            }

            var number = 0;

            if (c.Aggressive && c.Water)
            {
                number = At(c.X, c.Y, Sea, comps, w, h);
            }
            else if (!c.Aggressive && !c.Water)
            {
                // A sheep in an orc camp is the camp's; otherwise the hunting ground it stands in, or its own spawner's.
                number = At(c.X, c.Y, Land, comps, w, h);

                if (number == 0)
                {
                    number = At(c.X, c.Y, Passive, comps, w, h);
                }

                if (number == 0 && c.Haunt >= 0 && passive[c.Haunt] && hauntComp[c.Haunt] > 0 && comps[hauntComp[c.Haunt] - 1].Count > 0)
                {
                    number = hauntComp[c.Haunt];
                }
            }

            if (number == 0)
            {
                continue;
            }

            comps[number - 1].Live.Add(i);
            liveComp[i] = number;

            // A sea spawner stands on the shore, outside its creatures' water: it joins where they swim.
            if (c.Haunt >= 0 && hauntComp[c.Haunt] == 0 && c.Aggressive)
            {
                hauntComp[c.Haunt] = number;
                comps[number - 1].Haunts.Add(c.Haunt);
            }
        }

        for (var g = 0; g < landGroups.Count; g++)
        {
            var observed = observedOf[g];
            var comp = comps[g];

            for (var k = 0; k < observed.Count; k++)
            {
                comp.ObservedWeight += input.ObservedWeight[observed[k]];
            }
        }

        var assigned = Stopwatch.GetTimestamp();
        var reference = Math.Max(1.0, input.RefBotPower);

        for (var k = 0; k < comps.Count; k++)
        {
            Reckon(comps[k], input, s, reference);
        }

        var reckoned = Stopwatch.GetTimestamp();

        // The fringe: every patrol dilated by the reach of what patrols it.
        for (var k = 0; k < comps.Count; k++)
        {
            var comp = comps[k];

            if (comp.Kept && comp.Layer != Passive)
            {
                Fringe(comp, w, h);
            }
        }

        var fringed = Stopwatch.GetTimestamp();
        List<Comp> kept = [];

        for (var k = 0; k < comps.Count; k++)
        {
            if (comps[k].Kept)
            {
                comps[k].Index = kept.Count;
                kept.Add(comps[k]);
            }
        }

        // The raster: hunting grounds first, then fringes weakest first, then patrols weakest first — the stronger claim wins a cell.
        var cells = input.Buffer;

        Array.Clear(cells);

        for (var k = 0; k < kept.Count; k++)
        {
            var comp = kept[k];

            if (comp.Layer == Passive)
            {
                WriteCore(comp, cells, (ushort)(comp.Index + 1));
            }
        }

        List<Comp> ranked = [];

        for (var k = 0; k < kept.Count; k++)
        {
            if (kept[k].Layer != Passive)
            {
                ranked.Add(kept[k]);
            }
        }

        ranked.Sort((a, b) => a.Level != b.Level ? a.Level.CompareTo(b.Level) : a.VsBot.CompareTo(b.VsBot));

        for (var k = 0; k < ranked.Count; k++)
        {
            var comp = ranked[k];
            var value = (ushort)((comp.Index + 1) | BotZoneState.Fringe);

            for (var y = 0; y < comp.AH; y++)
            {
                for (var x = 0; x < comp.AW; x++)
                {
                    if (comp.Aggro[y * comp.AW + x] != 0)
                    {
                        cells[(comp.AY + y) * w + comp.AX + x] = value;
                    }
                }
            }
        }

        for (var k = 0; k < ranked.Count; k++)
        {
            WriteCore(ranked[k], cells, (ushort)(ranked[k].Index + 1));
        }

        // The fight at every painted cell, and how local the map is.
        var fightBuffer = input.FightBuffer is { } given && given.Length == w * h ? given : new ushort[w * h];

        Array.Clear(fightBuffer);
        FightRaster(cells, fightBuffer, kept, live, kinds, s, w, h);

        var paintedCells = 0;
        Comp largest = null;
        int landZones = 0, landCore = 0, landAggressive = 0, strayZones = 0;

        for (var i = 0; i < cells.Length; i++)
        {
            var v = cells[i];

            if (v != 0 && kept[(v & BotZoneState.IndexMask) - 1].Layer == Land)
            {
                paintedCells++;
            }
        }

        for (var k = 0; k < kept.Count; k++)
        {
            var comp = kept[k];

            if (comp.Layer != Land)
            {
                continue;
            }

            landZones++;
            landCore += comp.Count;
            landAggressive += comp.Aggressive;

            if (comp.Stray)
            {
                strayZones++;
            }

            if (comp.Event != null)
            {
                eventZones++;
            }

            if (largest == null || comp.Count > largest.Count)
            {
                largest = comp;
            }
        }

        var townsInside = 0;

        for (var i = 0; i < input.Towns.Length; i++)
        {
            var v = CellValue(cells, input.Towns[i].X1, input.Towns[i].Y1, w, h);

            if (v != 0 && kept[(v & BotZoneState.IndexMask) - 1].Layer == Land)
            {
                townsInside++;
            }
        }

        // The dead: counted where they fell, and a zone that keeps killing is read as deadlier than its arithmetic.
        var deaths = input.Deaths;

        for (var i = 0; i < deaths.Length; i++)
        {
            var v = CellValue(cells, deaths[i].X, deaths[i].Y, w, h);

            if (v != 0)
            {
                kept[(v & BotZoneState.IndexMask) - 1].Deaths++;
            }
        }

        for (var k = 0; k < kept.Count; k++)
        {
            var comp = kept[k];

            if (comp.Layer != Passive && comp.Level == BotZoneLevel.Hostile && s.DeathsPromote > 0 && comp.Deaths >= s.DeathsPromote)
            {
                comp.Level = BotZoneLevel.Deadly;
                comp.Promoted = true;
            }
        }

        int deathsDeadly = 0, deathsHostile = 0, deathsEdge = 0, deathsClear = 0;

        for (var i = 0; i < deaths.Length; i++)
        {
            var v = CellValue(cells, deaths[i].X, deaths[i].Y, w, h);

            if (v == 0)
            {
                deathsClear++;
            }
            else if ((v & BotZoneState.Fringe) != 0)
            {
                deathsEdge++;
            }
            else
            {
                switch (kept[(v & BotZoneState.IndexMask) - 1].Level)
                {
                    case BotZoneLevel.Deadly:
                        deathsDeadly++;
                        break;
                    case BotZoneLevel.Hostile:
                        deathsHostile++;
                        break;
                    case BotZoneLevel.Edge:
                        deathsEdge++;
                        break;
                    default:
                        deathsClear++;
                        break;
                }
            }
        }

        var nextId = Match(kept, input.Previous, w, out var matched);

        for (var k = 0; k < kept.Count; k++)
        {
            Describe(kept[k], input, s);
        }

        // The outlines and the JSON, off the same numbers.
        var shapesBegan = Stopwatch.GetTimestamp();

        _ringsTicks = 0;
        _ringsTraced = 0;
        _ringsReused = 0;
        _ringsNext.Clear();
        var buffer = new ArrayBufferWriter<byte>(256 * 1024);
        var zones = new BotZone[kept.Count];
        int clear = 0, hostile = 0, deadly = 0, water = 0;

        using (var json = new Utf8JsonWriter(buffer))
        {
            json.WriteStartArray();

            for (var k = 0; k < kept.Count; k++)
            {
                var comp = kept[k];
                var zone = Zone(comp);

                zones[k] = zone;

                switch (comp.Level)
                {
                    case BotZoneLevel.Clear:
                        clear++;
                        break;
                    case BotZoneLevel.Deadly:
                        deadly++;
                        break;
                    default:
                        hostile++;
                        break;
                }

                if (comp.Layer == Sea)
                {
                    water++;
                }

                Write(json, comp, zone, s, w);
            }

            json.WriteEndArray();
        }

        var shapesMs = Stopwatch.GetElapsedTime(shapesBegan).TotalMilliseconds;
        var ringsMs = _ringsTicks * 1000.0 / Stopwatch.Frequency;

        // What this build traced or reused is what the next one may reuse; the rest is dropped.
        (_rings, _ringsNext) = (_ringsNext, _rings);
        _ringsNext.Clear();
        var phases = string.Create(
            CultureInfo.InvariantCulture,
            $"paint and label {Ms(began, painted):F1}, assign {Ms(painted, assigned):F1}, reckon {Ms(assigned, reckoned):F1}, fringe {Ms(reckoned, fringed):F1}, raster and numbers {Ms(fringed, shapesBegan):F1}, rings {ringsMs:F1} ({_ringsTraced} traced, {_ringsReused} reused) and JSON {shapesMs - ringsMs:F1}"
        );

        return new BotZoneState
        {
            Map = input.Map,
            Width = w,
            Height = h,
            Cells = cells,
            Fight = fightBuffer,
            Zones = zones,
            PaintedCells = paintedCells,
            LandZones = landZones,
            LandCoreCells = landCore,
            LandAggressive = landAggressive,
            StrayZones = strayZones,
            GrownHaunts = grownHaunts,
            EventZones = eventZones,
            TrailCells = trailCells,
            Largest = largest == null ? null : zones[largest.Index],
            TownsInside = townsInside,
            GuardedKept = _guardedKept,
            NextId = nextId,
            At = input.At,
            RefBotPower = input.RefBotPower,
            Clear = clear,
            Edge = 0,
            Hostile = hostile,
            Deadly = deadly,
            WaterZones = water,
            Spawners = haunts.Length,
            Creatures = live.Length,
            ObservedCells = input.Observed.Length,
            Kept = matched,
            Born = kept.Count - matched,
            DeathsDeadly = deathsDeadly,
            DeathsHostile = deathsHostile,
            DeathsEdge = deathsEdge,
            DeathsClear = deathsClear,
            ZonesUtf8 = buffer.WrittenSpan.ToArray(),
            ShapesMs = shapesMs,
            Phases = phases,
            OffLoop = input.OffLoop,
            Generation = input.Generation,
            BuildMs = Stopwatch.GetElapsedTime(began).TotalMilliseconds
        };
    }

    // ---------------------------------------------------------------------------------------------------------------------
    // Painting and labelling
    // ---------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Marks every cell whose middle lies within <paramref name="radius"/> tiles (and half a cell) of the tile, and always the
    /// cell under it, unless a cell already carries a <paramref name="skip"/> bit.
    /// </summary>
    private static void Paint(int x, int y, int radius, byte bit, byte skip, int w, int h, bool force = false)
    {
        var reach = Math.Max(0, radius) + BotZoneState.Cell / 2;
        var reach2 = reach * reach;
        var x0 = Math.Max(0, (x - reach) >> BotZoneState.Shift);
        var y0 = Math.Max(0, (y - reach) >> BotZoneState.Shift);
        var x1 = Math.Min(w - 1, (x + reach) >> BotZoneState.Shift);
        var y1 = Math.Min(h - 1, (y + reach) >> BotZoneState.Shift);
        var ux = x >> BotZoneState.Shift;
        var uy = y >> BotZoneState.Shift;

        for (var cy = y0; cy <= y1; cy++)
        {
            var dy = (cy << BotZoneState.Shift) + BotZoneState.Cell / 2 - y;

            for (var cx = x0; cx <= x1; cx++)
            {
                var dx = (cx << BotZoneState.Shift) + BotZoneState.Cell / 2 - x;

                if (dx * dx + dy * dy <= reach2 || cx == ux && cy == uy)
                {
                    Set(cy * w + cx, bit, skip, force);
                }
            }
        }
    }

    /// <summary>The cell and the square of <paramref name="margin"/> cells round it.</summary>
    private static void Block(int cx, int cy, int margin, byte bit, int w, int h)
    {
        for (var y = Math.Max(0, cy - margin); y <= Math.Min(h - 1, cy + margin); y++)
        {
            for (var x = Math.Max(0, cx - margin); x <= Math.Min(w - 1, cx + margin); x++)
            {
                Set(y * w + x, bit, 0, false);
            }
        }
    }

    /// <summary>
    /// Marks a cell with a bit and, when a comp is being painted (<see cref="_painting"/>), claims it for that comp's cell list —
    /// once per comp, though two comps may claim the same cell. Guarded ground takes no land paint — the guards kill what
    /// attacks there, and a town is never a patrol's — unless <paramref name="force"/>: a rooted creature's own cell, which is
    /// where it grew, whatever the region.
    /// </summary>
    private static void Set(int i, byte bit, byte skip, bool force)
    {
        if (!force && _guarded != null && bit != Sea && bit != SeaGroup && _guarded[i] != 0)
        {
            // Counted once a cell: the owner mark is borrowed for it, and cleared with the rest.
            if (_owner[i] != -1)
            {
                if (_mark[i] == 0 && _owner[i] == 0)
                {
                    _touched.Add(i);
                }

                _owner[i] = -1;
                _guardedKept++;
            }

            return;
        }

        var m = _mark[i];

        if ((m & skip) != 0)
        {
            return;
        }

        if (m == 0)
        {
            _touched.Add(i);
        }

        if ((m & bit) == 0)
        {
            _marked++;
        }

        _mark[i] = (byte)(m | bit);

        var comp = _painting;

        if (comp == null || _owner[i] == comp.Number)
        {
            return;
        }

        _owner[i] = comp.Number;
        _cells.Add(i);
        comp.Count++;

        if (_label[i] == 0)
        {
            _label[i] = comp.Number;
        }

        var x = i % _width;
        var y = i / _width;

        comp.MinX = Math.Min(comp.MinX, x);
        comp.MaxX = Math.Max(comp.MaxX, x);
        comp.MinY = Math.Min(comp.MinY, y);
        comp.MaxY = Math.Max(comp.MaxY, y);
    }

    /// <summary>The raster's width, for <see cref="Set"/> to place a cell; set by the build before any paint.</summary>
    private static int _width;

    // ---------------------------------------------------------------------------------------------------------------------
    // Camps, wanderers and the observed
    // ---------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// How far past its declaration a spawner's patrol grows on what was seen (08.10.2026): to the high percentile of its
    /// creatures' distance from home, at most <see cref="BotZoneSettings.ReachBeyond"/> past the declared range; nought when it
    /// does not grow — nothing seen, the percentile inside the declaration, or a creature that stands where it grew.
    /// </summary>
    private static int Grown(BotZoneHauntIn haunt, bool rooted, BotZoneSettings s) =>
        rooted || s.ReachBeyond <= 0 || haunt.Reach <= haunt.Radius ? 0 : Math.Min(haunt.Reach, haunt.Radius + s.ReachBeyond);

    /// <summary>
    /// A wanderer's last places painted as the line it walked, newest first from where it stands now, until
    /// <paramref name="most"/> cells have taken the land's bit for the first time (08.10.2026). A line between two sightings is a
    /// guess at the way between them, and the reason for the cap: a chase seen twice forty tiles apart paints the road between.
    /// Returns the cells it added.
    /// </summary>
    private static int Trail(BotZoneInput input, BotZoneLiveIn c, int radius, int most, int w, int h)
    {
        var trail = input.Trail;

        if (c.TrailCount <= 0 || most <= 0 || trail == null)
        {
            return 0;
        }

        var start = _marked;
        var px = c.X;
        var py = c.Y;

        for (var k = 0; k < c.TrailCount && _marked - start < most; k++)
        {
            var t = (c.TrailFrom + k) * 2;

            if (t < 0 || t + 1 >= trail.Length)
            {
                break;
            }

            var x = trail[t];
            var y = trail[t + 1];
            var steps = Math.Max(1, Math.Max(Math.Abs(x - px), Math.Abs(y - py)) / BotZoneState.Cell);

            for (var j = 1; j <= steps && _marked - start < most; j++)
            {
                Paint(px + (x - px) * j / steps, py + (y - py) * j / steps, radius, Land, 0, w, h);
            }

            px = x;
            py = y;
        }

        return _marked - start;
    }

    /// <summary>
    /// The spawners of one layer as camps: two are one camp when their discs touch within <see cref="BotZoneSettings.JoinGap"/>
    /// tiles and the camp they make stays within <see cref="BotZoneSettings.ZoneMostTiles"/> across — nearest pairs first, so a
    /// fort is one zone and a chain of camps along a road is a chain of zones. A disc wider than
    /// <see cref="BotZoneSettings.DeclaredMost"/> joins as if it were that wide. Each haunt of the layer is in exactly one camp.
    /// </summary>
    private static List<List<int>> Group(BotZoneHauntIn[] haunts, bool[] member, bool[] rooted, BotZoneSettings s)
    {
        var n = haunts.Length;
        var parent = new int[n];
        var reach = new int[n];
        var x1 = new int[n];
        var y1 = new int[n];
        var x2 = new int[n];
        var y2 = new int[n];
        var most = Math.Max(BotZoneState.Cell, s.ZoneMostTiles);
        var gap = Math.Max(0, s.JoinGap);
        var bucket = Math.Max(64, 2 * Math.Max(1, s.DeclaredMost) + gap);
        Dictionary<(int X, int Y), List<int>> buckets = [];

        for (var i = 0; i < n; i++)
        {
            parent[i] = i;

            if (!member[i])
            {
                continue;
            }

            var haunt = haunts[i];

            reach[i] = rooted != null && rooted[i] ? 0 : Math.Min(haunt.Radius, s.DeclaredMost);
            x1[i] = haunt.X - reach[i];
            y1[i] = haunt.Y - reach[i];
            x2[i] = haunt.X + reach[i];
            y2[i] = haunt.Y + reach[i];

            var key = (haunt.X / bucket, haunt.Y / bucket);

            if (!buckets.TryGetValue(key, out var list))
            {
                buckets[key] = list = [];
            }

            list.Add(i);
        }

        int Find(int i)
        {
            while (parent[i] != i)
            {
                parent[i] = parent[parent[i]];
                i = parent[i];
            }

            return i;
        }

        List<(int A, int B, int D)> pairs = [];

        foreach (var ((bx, by), list) in buckets)
        {
            for (var k = 0; k < list.Count; k++)
            {
                var a = list[k];

                for (var dy = -1; dy <= 1; dy++)
                {
                    for (var dx = -1; dx <= 1; dx++)
                    {
                        if (!buckets.TryGetValue((bx + dx, by + dy), out var near))
                        {
                            continue;
                        }

                        for (var m = 0; m < near.Count; m++)
                        {
                            var b = near[m];

                            if (b <= a)
                            {
                                continue;
                            }

                            var d = Math.Max(Math.Abs(haunts[a].X - haunts[b].X), Math.Abs(haunts[a].Y - haunts[b].Y));

                            if (d <= reach[a] + reach[b] + gap)
                            {
                                pairs.Add((a, b, d));
                            }
                        }
                    }
                }
            }
        }

        pairs.Sort((p, q) => p.D.CompareTo(q.D));

        for (var p = 0; p < pairs.Count; p++)
        {
            var ra = Find(pairs[p].A);
            var rb = Find(pairs[p].B);

            if (ra == rb)
            {
                continue;
            }

            var nx1 = Math.Min(x1[ra], x1[rb]);
            var ny1 = Math.Min(y1[ra], y1[rb]);
            var nx2 = Math.Max(x2[ra], x2[rb]);
            var ny2 = Math.Max(y2[ra], y2[rb]);

            if (nx2 - nx1 + 1 > most || ny2 - ny1 + 1 > most)
            {
                continue;
            }

            parent[rb] = ra;
            x1[ra] = nx1;
            y1[ra] = ny1;
            x2[ra] = nx2;
            y2[ra] = ny2;
        }

        Dictionary<int, List<int>> byRoot = [];
        List<List<int>> groups = [];

        for (var i = 0; i < n; i++)
        {
            if (!member[i])
            {
                continue;
            }

            var root = Find(i);

            if (!byRoot.TryGetValue(root, out var group))
            {
                byRoot[root] = group = [];
                groups.Add(group);
            }

            group.Add(i);
        }

        return groups;
    }

    /// <summary>
    /// Gives each observed cell to the camp of the nearest attackers' spawner whose range (grown on what was seen, 08.10.2026)
    /// plus <see cref="BotZoneSettings.ObservedSlack"/> covers the cell's middle, as an index into the input's observed arrays. A
    /// cell no range covers is where a creature was seen on a chase or a lure, and goes to nobody.
    /// </summary>
    private static void Attribute(
        BotZoneInput input, BotZoneHauntIn[] haunts, int[] range, bool[] member, int[] groupOf, List<int>[] observedOf, BotZoneSettings s,
        int w, int h
    )
    {
        var observed = input.Observed;

        if (observed.Length == 0)
        {
            return;
        }

        const int bucket = 256;
        var slack = Math.Max(0, s.ObservedSlack);
        Dictionary<(int X, int Y), List<int>> buckets = [];

        for (var i = 0; i < haunts.Length; i++)
        {
            if (!member[i] || groupOf[i] < 0)
            {
                continue;
            }

            var key = (haunts[i].X / bucket, haunts[i].Y / bucket);

            if (!buckets.TryGetValue(key, out var list))
            {
                buckets[key] = list = [];
            }

            list.Add(i);
        }

        for (var k = 0; k < observed.Length; k++)
        {
            var cell = observed[k];

            if ((uint)cell >= (uint)(w * h))
            {
                continue;
            }

            var x = (cell % w << BotZoneState.Shift) + BotZoneState.Cell / 2;
            var y = (cell / w << BotZoneState.Shift) + BotZoneState.Cell / 2;
            var bx = x / bucket;
            var by = y / bucket;
            var best = -1;
            var bestD = int.MaxValue;

            for (var dy = -1; dy <= 1; dy++)
            {
                for (var dx = -1; dx <= 1; dx++)
                {
                    if (!buckets.TryGetValue((bx + dx, by + dy), out var list))
                    {
                        continue;
                    }

                    for (var m = 0; m < list.Count; m++)
                    {
                        var i = list[m];
                        var haunt = haunts[i];
                        var d = Math.Max(Math.Abs(x - haunt.X), Math.Abs(y - haunt.Y));

                        if (d <= Math.Min(bucket, range[i]) + slack && d < bestD)
                        {
                            bestD = d;
                            best = i;
                        }
                    }
                }
            }

            if (best >= 0)
            {
                observedOf[groupOf[best]].Add(k);
            }
        }
    }

    /// <summary>
    /// The fight at every painted cell: round each cell, the strongest living land attacker within
    /// <see cref="BotZoneSettings.ConvergeTiles"/> in full and the rest at the secondary weight — whatever zone they are of — and
    /// never below the owning zone's promised fight at one place weighed by <see cref="BotZoneSettings.EmptyWeight"/>. Nought
    /// on a hunting ground. Written in units of <see cref="BotZoneState.FightUnit"/>.
    /// </summary>
    private static void FightRaster(ushort[] cells, ushort[] fight, List<Comp> kept, BotZoneLiveIn[] live, BotZoneKindIn[] kinds, BotZoneSettings s, int w, int h)
    {
        var gen = ++_fightGen;
        var reach = Math.Max(1, s.ConvergeTiles);

        for (var i = 0; i < live.Length; i++)
        {
            var c = live[i];

            if (!c.Aggressive || c.Water)
            {
                continue;
            }

            var power = c.Power > 0f ? c.Power : (float)kinds[c.Kind].Power;

            if (power <= 0f)
            {
                continue;
            }

            var cx0 = Math.Max(0, (c.X - reach) >> BotZoneState.Shift);
            var cy0 = Math.Max(0, (c.Y - reach) >> BotZoneState.Shift);
            var cx1 = Math.Min(w - 1, (c.X + reach) >> BotZoneState.Shift);
            var cy1 = Math.Min(h - 1, (c.Y + reach) >> BotZoneState.Shift);

            for (var cy = cy0; cy <= cy1; cy++)
            {
                for (var cx = cx0; cx <= cx1; cx++)
                {
                    var i2 = cy * w + cx;

                    if (_fightStamp[i2] != gen)
                    {
                        _fightStamp[i2] = gen;
                        _fightSum[i2] = power;
                        _fightMax[i2] = power;
                    }
                    else
                    {
                        _fightSum[i2] += power;

                        if (power > _fightMax[i2])
                        {
                            _fightMax[i2] = power;
                        }
                    }
                }
            }
        }

        for (var i = 0; i < cells.Length; i++)
        {
            var v = cells[i];

            if (v == 0)
            {
                continue;
            }

            var comp = kept[(v & BotZoneState.IndexMask) - 1];

            if (comp.Layer == Passive)
            {
                continue;
            }

            var local = _fightStamp[i] == gen ? _fightMax[i] + s.Secondary * Math.Max(0.0, _fightSum[i] - _fightMax[i]) : 0.0;
            var floor = comp.Stray ? 0.0 : comp.Declared * s.EmptyWeight;
            var units = Math.Round(Math.Max(local, floor) / BotZoneState.FightUnit);

            fight[i] = (ushort)Math.Min(ushort.MaxValue, Math.Max(0, units));
        }
    }

    private static int LabelOf(int i, byte layer) =>
        layer == Sea ? _seaLabel.TryGetValue(i, out var n) ? n : 0 : _label[i];

    private static void SetLabel(int i, byte layer, int number)
    {
        if (layer == Sea)
        {
            _seaLabel[i] = number;
        }
        else
        {
            _label[i] = number;
        }
    }

    /// <summary>
    /// Connected patches of the cells carrying <paramref name="bit"/>, eight ways, appended to the list as zones of
    /// <paramref name="layer"/>; each cell's patch number is left in the layer's labels. When the grouping bit is not the layer's
    /// own (the sea), each patch keeps as its patrol only the cells carrying the layer's bit.
    /// </summary>
    private static void Label(byte bit, byte layer, List<Comp> comps, int w, int h)
    {
        var count = _touched.Count;

        for (var k = 0; k < count; k++)
        {
            var seed = _touched[k];

            if ((_mark[seed] & bit) == 0 || LabelOf(seed, layer) != 0)
            {
                continue;
            }

            var comp = new Comp { Number = comps.Count + 1, Layer = layer, Start = _cells.Count };

            comps.Add(comp);
            SetLabel(seed, layer, comp.Number);
            _queue.Clear();
            _queue.Add(seed);

            for (var head = 0; head < _queue.Count; head++)
            {
                var at = _queue[head];
                var x = at % w;
                var y = at / w;

                _cells.Add(at);

                if (x < comp.MinX)
                {
                    comp.MinX = x;
                }

                if (x > comp.MaxX)
                {
                    comp.MaxX = x;
                }

                if (y < comp.MinY)
                {
                    comp.MinY = y;
                }

                if (y > comp.MaxY)
                {
                    comp.MaxY = y;
                }

                for (var ny = Math.Max(0, y - 1); ny <= Math.Min(h - 1, y + 1); ny++)
                {
                    for (var nx = Math.Max(0, x - 1); nx <= Math.Min(w - 1, x + 1); nx++)
                    {
                        var next = ny * w + nx;

                        if ((_mark[next] & bit) != 0 && LabelOf(next, layer) == 0)
                        {
                            SetLabel(next, layer, comp.Number);
                            _queue.Add(next);
                        }
                    }
                }
            }

            comp.Count = _cells.Count - comp.Start;

            if (bit != layer)
            {
                Narrow(comp, layer, w);
            }
        }
    }

    /// <summary>Keeps at the front of a patch's cells only those carrying the layer's bit, and draws its box round them.</summary>
    private static void Narrow(Comp comp, byte layer, int w)
    {
        var kept = comp.Start;

        comp.MinX = comp.MinY = int.MaxValue;
        comp.MaxX = comp.MaxY = -1;

        for (var k = comp.Start; k < comp.Start + comp.Count; k++)
        {
            var cell = _cells[k];

            if ((_mark[cell] & layer) == 0)
            {
                continue;
            }

            (_cells[kept], _cells[k]) = (cell, _cells[kept]);
            kept++;

            var x = cell % w;
            var y = cell / w;

            comp.MinX = Math.Min(comp.MinX, x);
            comp.MaxX = Math.Max(comp.MaxX, x);
            comp.MinY = Math.Min(comp.MinY, y);
            comp.MaxY = Math.Max(comp.MaxY, y);
        }

        comp.Count = kept - comp.Start;
    }

    /// <summary>The patch of a layer under a tile, or nought.</summary>
    private static int At(int x, int y, byte layer, List<Comp> comps, int w, int h)
    {
        var cx = x >> BotZoneState.Shift;
        var cy = y >> BotZoneState.Shift;

        if ((uint)cx >= (uint)w || (uint)cy >= (uint)h)
        {
            return 0;
        }

        var number = LabelOf(cy * w + cx, layer);

        return number > 0 && comps[number - 1].Layer == layer ? number : 0;
    }

    private static int CellValue(ushort[] cells, int x, int y, int w, int h)
    {
        var cx = x >> BotZoneState.Shift;
        var cy = y >> BotZoneState.Shift;

        return (uint)cx >= (uint)w || (uint)cy >= (uint)h ? 0 : cells[cy * w + cx];
    }

    private static void WriteCore(Comp comp, ushort[] cells, ushort value)
    {
        for (var k = comp.Start; k < comp.Start + comp.Count; k++)
        {
            cells[_cells[k]] = value;
        }
    }

    // ---------------------------------------------------------------------------------------------------------------------
    // What a zone holds, and how strong it is
    // ---------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// How far round its patrol a creature attacks a bot: what it sees, or a caster's or an archer's blow where that lands
    /// farther (08.10.2026). Not the two added: a creature picks a target only within its sight (<c>MageAI</c>), so it casts at
    /// nothing past it. A creature that cannot move reaches as far as it strikes; a sea creature from the water's edge.
    /// </summary>
    private static int ReachOf(bool water, bool rooted, bool caster, int perception, int strike, BotZoneSettings s)
    {
        var sight = Math.Clamp(perception, 1, Math.Max(1, s.PerceptionMost));

        if (water)
        {
            return caster ? Math.Min(sight, s.WaterCasterReach) : s.WaterMeleeReach;
        }

        if (rooted)
        {
            return caster ? sight : Math.Max(s.RootedReach, strike);
        }

        return Math.Max(sight, strike);
    }

    private static void Reckon(Comp comp, BotZoneInput input, BotZoneSettings s, double reference)
    {
        var kinds = input.Kinds;
        var haunts = input.Haunts;
        var live = input.Live;
        var attackers = new List<(int X, int Y, double Power)>();
        Dictionary<int, (int Live, double Max)> byKind = [];

        for (var k = 0; k < comp.Live.Count; k++)
        {
            var c = live[comp.Live[k]];

            comp.Alive++;
            byKind[c.Kind] = byKind.TryGetValue(c.Kind, out var had) ? (had.Live + 1, had.Max) : (1, 0.0);

            if (!c.Aggressive)
            {
                continue;
            }

            comp.Aggressive++;

            if (c.Caster)
            {
                comp.Casters++;
            }

            var power = c.Power > 0 ? c.Power : kinds[c.Kind].Power;

            attackers.Add((c.X, c.Y, power));
            comp.Total += power;
            comp.Worst = Math.Max(comp.Worst, power);
            comp.Reach = Math.Max(comp.Reach, ReachOf(c.Water, c.Rooted, c.Caster, c.Perception, c.Strike, s));
        }

        var declared = 0.0;
        var declaredTotal = 0.0;
        var declaredWorst = 0.0;
        var weight = 0.0;
        var confidence = 0.0;
        var awake = 0.0;

        for (var k = 0; k < comp.Haunts.Count; k++)
        {
            var haunt = haunts[comp.Haunts[k]];
            var worst = 0.0;
            var sum = 0.0;

            comp.Max += haunt.Count;
            comp.DeclaredRange = Math.Max(comp.DeclaredRange, haunt.Radius);
            comp.ObservedRange = Math.Max(comp.ObservedRange, haunt.ObservedRange);
            comp.Samples += haunt.Samples;

            if (haunt.LastAwake > comp.LastSeen)
            {
                comp.LastSeen = haunt.LastAwake;
            }

            weight += haunt.Count;
            confidence += haunt.Confidence * haunt.Count;
            awake += haunt.AwakeSeconds * haunt.Count;

            for (var e = 0; e < haunt.Kinds.Length; e++)
            {
                var index = haunt.Kinds[e];
                var kind = kinds[index];
                var expected = haunt.Expected[e];

                byKind[index] = byKind.TryGetValue(index, out var had) ? (had.Live, had.Max + expected) : (0, expected);

                if (!kind.Aggressive || kind.Power <= 0.0)
                {
                    continue;
                }

                worst = Math.Max(worst, kind.Power);
                sum += expected * kind.Power;
                comp.Reach = Math.Max(
                    comp.Reach,
                    ReachOf(kind.WaterAt(SeaCamp(haunt, kinds)), kind.Rooted, kind.Caster, kind.Perception, kind.Strike, s)
                );
            }

            declaredTotal += sum;
            declaredWorst = Math.Max(declaredWorst, worst);

            // At one place, not over the whole disc: a convergence disc holds that share of the spawner's range, so of ten
            // reapers kept over a radius of 150 a walker meets one or two at once, and of ten orcs in a camp of radius 10, all.
            var share = Math.Min(1.0, Math.Pow(s.ConvergeTiles / (double)Math.Max(1, haunt.Radius), 2.0));

            declared = Math.Max(declared, worst + s.Secondary * Math.Max(0.0, sum - worst) * share);
        }

        comp.Declared = declared;
        comp.Confidence = weight > 0 ? confidence / weight : Math.Min(1.0, comp.ObservedWeight / Math.Max(1, s.ConfidentSamples));
        comp.AwakeSeconds = weight > 0 ? awake / weight : 0.0;

        if (comp.Haunts.Count == 0)
        {
            comp.Samples = (long)Math.Round(comp.ObservedWeight);
        }

        // Kept or dropped. A patch of the land with no spawner and nobody alive is an old sighting of creatures now dead; a camp
        // whose spawners paint nothing (all stopped, or wide and with nobody alive and nothing seen) has no ground to be a zone of.
        comp.Kept = comp.Count > 0 && comp.Layer switch
        {
            Land => comp.Haunts.Count > 0 || comp.Aggressive > 0,
            Sea => comp.Aggressive > 0,
            _ => comp.Alive >= Math.Max(1, s.ClearLeastLive)
        };

        if (!comp.Kept)
        {
            return;
        }

        if (comp.Layer == Passive)
        {
            comp.Level = BotZoneLevel.Clear;
            comp.Threat = comp.Worst;

            // Their strength if set upon: the strongest of the living, whether or not it would ever come first.
            for (var k = 0; k < comp.Live.Count; k++)
            {
                var c = live[comp.Live[k]];
                var power = c.Power > 0 ? c.Power : kinds[c.Kind].Power;

                comp.Total += power;
                comp.Threat = Math.Max(comp.Threat, power);
            }

            comp.Worst = comp.Threat;
            comp.VsBot = comp.Threat / reference;
        }
        else if (attackers.Count > 0)
        {
            (comp.Threat, comp.Converge) = Converging(attackers, s);
            comp.VsBot = comp.Threat / reference;
            comp.Level = comp.VsBot >= s.DeadlyVsBot ? BotZoneLevel.Deadly : BotZoneLevel.Hostile;
        }
        else
        {
            // Nobody alive now, and the spawner fills it again within minutes: the declared fight, weighed down.
            comp.Empty = true;
            comp.Total = declaredTotal;
            comp.Worst = declaredWorst;
            comp.Threat = declared * s.EmptyWeight;
            comp.VsBot = comp.Threat / reference;
            comp.Level = comp.VsBot >= s.DeadlyVsBot ? BotZoneLevel.Deadly : BotZoneLevel.Hostile;
        }

        if (comp.Reach == 0 && comp.Layer != Passive)
        {
            comp.Reach = Math.Min(16, s.PerceptionMost);
        }

        // Kinds, the strongest share first: attackers before the rest.
        List<BotZoneKindCount> list = [];

        foreach (var (index, count) in byKind)
        {
            var kind = kinds[index];

            list.Add(new BotZoneKindCount(kind.Name, count.Live, (int)Math.Round(count.Max), Math.Round(kind.Power), kind.Aggressive, kind.Caster));
        }

        list.Sort(
            (a, b) =>
            {
                if (a.Aggressive != b.Aggressive)
                {
                    return a.Aggressive ? -1 : 1;
                }

                var ai = Math.Max(a.Live, a.Max) * a.Power;
                var bi = Math.Max(b.Live, b.Max) * b.Power;

                return bi.CompareTo(ai);
            }
        );

        var most = Math.Max(1, s.MostKinds);

        comp.OtherKinds = Math.Max(0, list.Count - most);

        if (list.Count > most)
        {
            list.RemoveRange(most, list.Count - most);
        }

        comp.Kinds = list.ToArray();

        // The middle, the box, and where the tour stands: the spawner nearest the middle, whose tile is real floor.
        long sx = 0, sy = 0;

        for (var k = comp.Start; k < comp.Start + comp.Count; k++)
        {
            sx += _cells[k] % input.Width;
            sy += _cells[k] / input.Width;
        }

        comp.CenterX = (int)(sx * BotZoneState.Cell / Math.Max(1, comp.Count)) + BotZoneState.Cell / 2;
        comp.CenterY = (int)(sy * BotZoneState.Cell / Math.Max(1, comp.Count)) + BotZoneState.Cell / 2;
        comp.Stand = new Point3D(comp.CenterX, comp.CenterY, 0);

        var nearest = long.MaxValue;

        for (var k = 0; k < comp.Haunts.Count; k++)
        {
            var haunt = haunts[comp.Haunts[k]];
            var dx = (long)(haunt.X - comp.CenterX);
            var dy = (long)(haunt.Y - comp.CenterY);
            var d = dx * dx + dy * dy;

            if (d < nearest)
            {
                nearest = d;
                comp.Stand = new Point3D(haunt.X, haunt.Y, haunt.Z);
                comp.StandIsReal = true;
            }
        }
    }

    /// <summary>
    /// The worst fight in a set of attackers: round each of the strongest (at most 96), everyone within reach of it — that one's
    /// worst in full and the rest at the secondary weight. The largest such crowd comes back with it.
    /// </summary>
    private static (double Threat, int Crowd) Converging(List<(int X, int Y, double Power)> attackers, BotZoneSettings s)
    {
        attackers.Sort((a, b) => b.Power.CompareTo(a.Power));

        var reach = Math.Max(1, s.ConvergeTiles);
        var anchors = Math.Min(attackers.Count, 96);
        var threat = 0.0;
        var crowd = 0;

        for (var a = 0; a < anchors; a++)
        {
            var anchor = attackers[a];
            var worst = 0.0;
            var sum = 0.0;
            var count = 0;

            for (var b = 0; b < attackers.Count; b++)
            {
                var other = attackers[b];

                if (Math.Abs(other.X - anchor.X) > reach || Math.Abs(other.Y - anchor.Y) > reach)
                {
                    continue;
                }

                count++;
                sum += other.Power;
                worst = Math.Max(worst, other.Power);
            }

            threat = Math.Max(threat, worst + s.Secondary * Math.Max(0.0, sum - worst));
            crowd = Math.Max(crowd, count);
        }

        return (threat, crowd);
    }

    /// <summary>
    /// The aggro fringe round a patrol: every cell within its reach, by a chamfer distance (three a step, four a diagonal) over
    /// the patrol's box widened by the reach. Kept as a local mask for the raster and the outline.
    /// </summary>
    private static void Fringe(Comp comp, int w, int h)
    {
        var reachTiles = comp.Reach + BotZoneState.Cell / 2;
        var r = (reachTiles + BotZoneState.Cell - 1) / BotZoneState.Cell;
        var x0 = Math.Max(0, comp.MinX - r);
        var y0 = Math.Max(0, comp.MinY - r);
        var x1 = Math.Min(w - 1, comp.MaxX + r);
        var y1 = Math.Min(h - 1, comp.MaxY + r);
        var aw = x1 - x0 + 1;
        var ah = y1 - y0 + 1;
        var d = new int[aw * ah];
        const int far = int.MaxValue / 4;

        Array.Fill(d, far);

        for (var k = comp.Start; k < comp.Start + comp.Count; k++)
        {
            var cell = _cells[k];

            d[(cell / w - y0) * aw + cell % w - x0] = 0;
        }

        for (var y = 0; y < ah; y++)
        {
            for (var x = 0; x < aw; x++)
            {
                var i = y * aw + x;
                var v = d[i];

                if (x > 0)
                {
                    v = Math.Min(v, d[i - 1] + 3);
                }

                if (y > 0)
                {
                    v = Math.Min(v, d[i - aw] + 3);

                    if (x > 0)
                    {
                        v = Math.Min(v, d[i - aw - 1] + 4);
                    }

                    if (x < aw - 1)
                    {
                        v = Math.Min(v, d[i - aw + 1] + 4);
                    }
                }

                d[i] = v;
            }
        }

        for (var y = ah - 1; y >= 0; y--)
        {
            for (var x = aw - 1; x >= 0; x--)
            {
                var i = y * aw + x;
                var v = d[i];

                if (x < aw - 1)
                {
                    v = Math.Min(v, d[i + 1] + 3);
                }

                if (y < ah - 1)
                {
                    v = Math.Min(v, d[i + aw] + 3);

                    if (x < aw - 1)
                    {
                        v = Math.Min(v, d[i + aw + 1] + 4);
                    }

                    if (x > 0)
                    {
                        v = Math.Min(v, d[i + aw - 1] + 4);
                    }
                }

                d[i] = v;
            }
        }

        // Three chamfer units a cell, four tiles a cell. Guarded ground is no fringe: the guards meet what comes.
        var limit = (int)Math.Round(reachTiles * 3.0 / BotZoneState.Cell);
        var mask = new byte[aw * ah];
        var count = 0;

        for (var i = 0; i < d.Length; i++)
        {
            if (d[i] > limit)
            {
                continue;
            }

            if (!comp.Rooted && _guarded != null && _guarded[(y0 + i / aw) * w + x0 + i % aw] != 0)
            {
                _guardedKept++;

                continue;
            }

            mask[i] = 1;
            count++;
        }

        comp.Aggro = mask;
        comp.AX = x0;
        comp.AY = y0;
        comp.AW = aw;
        comp.AH = ah;
        comp.AggroCount = count;
    }

    // ---------------------------------------------------------------------------------------------------------------------
    // Numbers that last, and words
    // ---------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Gives each zone the number of the zone it most overlaps in the previous build, of the same family (the sea, the land's
    /// attackers, the hunting grounds), largest overlap first so a zone that split keeps its number in its larger half. Returns
    /// the next unused number; how many kept one comes back in <paramref name="matched"/>.
    /// </summary>
    private static int Match(List<Comp> kept, BotZoneState previous, int w, out int matched)
    {
        matched = 0;

        var nextId = Math.Max(1, previous?.NextId ?? 1);
        List<(int Comp, int Id, int Overlap)> pairs = [];

        if (previous?.Zones != null && previous.Width == w)
        {
            // Matched on each zone's own cells rather than on the raster, where a stronger neighbour's patrol or fringe may have
            // overwritten a weaker zone's ground: family by family, the old owners laid into the scratch labels (free by now),
            // counted under the new zones, and taken out again.
            Dictionary<int, int> overlap = [];
            byte[] families = [Land, Sea, Passive];

            // This build's own labels are done with; cleared so only the old owners are read below.
            for (var k = 0; k < _touched.Count; k++)
            {
                _label[_touched[k]] = 0;
            }

            for (var f = 0; f < families.Length; f++)
            {
                var family = families[f];
                var old = previous.Zones;

                for (var z = 0; z < old.Length; z++)
                {
                    if (Family(old[z]) != family || old[z].CellList == null)
                    {
                        continue;
                    }

                    var list = old[z].CellList;

                    for (var c = 0; c < list.Length; c++)
                    {
                        _label[list[c]] = z + 1;
                    }
                }

                for (var k = 0; k < kept.Count; k++)
                {
                    var comp = kept[k];

                    if (comp.Layer != family)
                    {
                        continue;
                    }

                    overlap.Clear();

                    for (var c = comp.Start; c < comp.Start + comp.Count; c++)
                    {
                        var owner = _label[_cells[c]];

                        // An event's zone takes only an event's number and a camp only a camp's (08.10.2026): an invasion laid over a
                        // small camp must not walk off with the camp's number.
                        if (owner > 0 && old[owner - 1].Event == (comp.Event != null))
                        {
                            var id = old[owner - 1].Id;

                            overlap[id] = overlap.TryGetValue(id, out var n) ? n + 1 : 1;
                        }
                    }

                    foreach (var (id, n) in overlap)
                    {
                        pairs.Add((k, id, n));
                    }
                }

                for (var z = 0; z < old.Length; z++)
                {
                    var list = old[z].CellList;

                    if (Family(old[z]) != family || list == null)
                    {
                        continue;
                    }

                    for (var c = 0; c < list.Length; c++)
                    {
                        _label[list[c]] = 0;
                    }
                }
            }
        }

        // Largest overlap first; on a tie the permanent camp before an event, then by order and number, so the same input always
        // gives the same numbers.
        pairs.Sort(
            (a, b) =>
            {
                var byOverlap = b.Overlap.CompareTo(a.Overlap);

                if (byOverlap != 0)
                {
                    return byOverlap;
                }

                var byEvent = (kept[a.Comp].Event != null).CompareTo(kept[b.Comp].Event != null);

                return byEvent != 0 ? byEvent : a.Comp != b.Comp ? a.Comp.CompareTo(b.Comp) : a.Id.CompareTo(b.Id);
            }
        );

        HashSet<int> taken = [];

        for (var p = 0; p < pairs.Count; p++)
        {
            var (k, id, _) = pairs[p];

            if (kept[k].Id != 0 || !taken.Add(id))
            {
                continue;
            }

            kept[k].Id = id;
            matched++;
        }

        for (var k = 0; k < kept.Count; k++)
        {
            if (kept[k].Id == 0)
            {
                kept[k].Id = nextId++;
            }
        }

        return nextId;
    }

    private static byte Family(BotZone zone) => zone.Water ? Sea : zone.Level == BotZoneLevel.Clear ? Passive : Land;

    private static void Describe(Comp comp, BotZoneInput input, BotZoneSettings s)
    {
        var who = comp.Event != null ? $"{Who(comp)} of {comp.Event}" : comp.Stray ? $"wandering {Who(comp)}" : Who(comp);
        var where = Place(comp.CenterX, comp.CenterY, comp.Layer == Sea, input);

        comp.Label = where.Length == 0 ? who : $"{who}, {where}";

        if (comp.Layer == Passive)
        {
            comp.Why = Invariant($"{comp.Alive} that do not attack first; the strongest is ×{comp.VsBot:F1} a bot if set upon");
        }
        else if (comp.Event != null)
        {
            comp.Why = Invariant($"{comp.Aggressive} attack on sight, of {comp.Event}: painted while they live, where they stand and where they walked; up to {comp.Converge} can close at once: ×{comp.VsBot:F1} a bot");
        }
        else if (comp.Stray)
        {
            comp.Why = Invariant($"{comp.Aggressive} attack on sight, away from any spawner's range (a chase, a lure), painted where they stand and where they walked; up to {comp.Converge} can close at once: ×{comp.VsBot:F1} a bot where they stand");
        }
        else if (comp.Empty)
        {
            comp.Why = Invariant($"none alive now; {Plural(comp.Haunts.Count, "spawner")} keep {comp.Max}, ×{comp.VsBot:F1} a bot counted at half until they return");
        }
        else if (comp.Layer == Sea)
        {
            comp.Why = Invariant($"{comp.Aggressive} that cannot leave the water; ×{comp.VsBot:F1} a bot within {comp.Reach} tiles of where they swim");
        }
        else
        {
            var casters = comp.Casters > 0 ? $" ({Plural(comp.Casters, "caster")})" : "";
            var line = comp.Level == BotZoneLevel.Deadly && !comp.Promoted ? $", past the ×{s.DeadlyVsBot:F0} line" : "";

            comp.Why = Invariant($"{comp.Aggressive} attack on sight{casters}; up to {comp.Converge} can close at once: ×{comp.VsBot:F1} a bot{line}");
        }

        if (comp.Promoted)
        {
            comp.Why += Invariant($"; deadly for {comp.Deaths} bots killed here in {s.DeathsWindowHours}h");
        }
        else if (comp.Deaths > 0)
        {
            comp.Why += Invariant($"; {Plural(comp.Deaths, "bot")} killed here in {s.DeathsWindowHours}h");
        }
    }

    /// <summary>Numbers with a point, whatever the machine's culture: the page and the log read "×1.7", never "×1,7".</summary>
    private static string Invariant(FormattableString text) => text.ToString(CultureInfo.InvariantCulture);

    private static string Plural(int n, string word) => n == 1 ? $"1 {word}" : $"{n} {word}s";

    /// <summary>The two kinds that make the zone what it is, in words: "orcs and orcish mages".</summary>
    private static string Who(Comp comp)
    {
        var first = -1;
        var second = -1;

        for (var k = 0; k < comp.Kinds.Length; k++)
        {
            var kind = comp.Kinds[k];

            if (comp.Layer != Passive && !kind.Aggressive)
            {
                continue;
            }

            if (first < 0)
            {
                first = k;
            }
            else if (second < 0)
            {
                second = k;

                break;
            }
        }

        if (first < 0)
        {
            return comp.Layer == Passive ? "animals" : "creatures";
        }

        var one = Words(comp.Kinds[first].Kind);

        return second < 0 ? one : $"{one} and {Words(comp.Kinds[second].Kind)}";
    }

    /// <summary>A type name as plural words: OrcishMage to "orcish mages", Ratman to "ratmen", Wolf to "wolves".</summary>
    public static string Words(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return "creatures";
        }

        Span<char> chars = stackalloc char[name.Length * 2];
        var n = 0;

        for (var i = 0; i < name.Length && n < chars.Length - 1; i++)
        {
            var c = name[i];

            if (char.IsUpper(c) && i > 0)
            {
                chars[n++] = ' ';
            }

            chars[n++] = char.ToLowerInvariant(c);
        }

        var words = new string(chars[..n]);

        if (words.EndsWith("man", StringComparison.Ordinal))
        {
            return string.Concat(words.AsSpan(0, words.Length - 3), "men");
        }

        if (words.EndsWith("wolf", StringComparison.Ordinal))
        {
            return string.Concat(words.AsSpan(0, words.Length - 1), "ves");
        }

        if (words.EndsWith("sheep", StringComparison.Ordinal) || words.EndsWith("deer", StringComparison.Ordinal))
        {
            return words;
        }

        if (words.EndsWith('s') || words.EndsWith('x') || words.EndsWith("sh", StringComparison.Ordinal) || words.EndsWith("ch", StringComparison.Ordinal))
        {
            return words + "es";
        }

        if (words.Length > 1 && words.EndsWith('y') && "aeiou".IndexOf(words[^2]) < 0)
        {
            return string.Concat(words.AsSpan(0, words.Length - 1), "ies");
        }

        return words + "s";
    }

    /// <summary>"in Shame", "by Britain", "NE of Minoc", "off Jhelom".</summary>
    private static string Place(int x, int y, bool water, BotZoneInput input)
    {
        var deeps = input.Deeps;

        for (var i = 0; i < deeps.Length; i++)
        {
            var deep = deeps[i];

            if (x >= deep.X1 && x <= deep.X2 && y >= deep.Y1 && y <= deep.Y2)
            {
                return $"in {deep.Name}";
            }
        }

        var towns = input.Towns;
        var best = -1;
        var bestDistance = double.MaxValue;

        for (var i = 0; i < towns.Length; i++)
        {
            var dx = (double)(x - towns[i].X1);
            var dy = (double)(y - towns[i].Y1);
            var d = Math.Sqrt(dx * dx + dy * dy);

            if (d < bestDistance)
            {
                bestDistance = d;
                best = i;
            }
        }

        if (best < 0)
        {
            return "";
        }

        var town = towns[best];
        var compass = Compass(x - town.X1, y - town.Y1);

        if (water)
        {
            return bestDistance <= 150 ? $"off {town.Name}" : $"at sea {compass} of {town.Name}";
        }

        return bestDistance <= 100 ? $"by {town.Name}" : $"{compass} of {town.Name}";
    }

    private static string Compass(int dx, int dy)
    {
        // Screen y grows southward.
        var angle = Math.Atan2(-dy, dx) * 180.0 / Math.PI;
        var sector = (int)Math.Round((angle + 360.0) % 360.0 / 45.0) % 8;

        return sector switch
        {
            0 => "E",
            1 => "NE",
            2 => "N",
            3 => "NW",
            4 => "W",
            5 => "SW",
            6 => "S",
            _ => "SE"
        };
    }

    private static BotZone Zone(Comp comp) =>
        new()
        {
            Id = comp.Id,
            Level = comp.Level,
            Label = comp.Label,
            Why = comp.Why,
            Water = comp.Layer == Sea,
            Stray = comp.Stray,
            Event = comp.Event != null,
            Declared = comp.Declared,
            CenterX = comp.CenterX,
            CenterY = comp.CenterY,
            X1 = comp.MinX * BotZoneState.Cell,
            Y1 = comp.MinY * BotZoneState.Cell,
            X2 = comp.MaxX * BotZoneState.Cell + BotZoneState.Cell - 1,
            Y2 = comp.MaxY * BotZoneState.Cell + BotZoneState.Cell - 1,
            CoreCells = comp.Count,
            AggroCells = comp.AggroCount,
            Live = comp.Alive,
            Max = comp.Max,
            Aggressive = comp.Aggressive,
            Casters = comp.Casters,
            Converge = comp.Converge,
            Kinds = comp.Kinds,
            OtherKinds = comp.OtherKinds,
            PowerTotal = comp.Total,
            Worst = comp.Worst,
            Threat = comp.Threat,
            VsBot = comp.VsBot,
            Empty = comp.Empty,
            DeclaredRange = comp.DeclaredRange,
            ObservedRange = comp.ObservedRange,
            AggroRange = comp.Layer == Passive ? 0 : comp.Reach,
            Confidence = comp.Confidence,
            Samples = comp.Samples,
            AwakeSeconds = comp.AwakeSeconds,
            LastSeen = comp.LastSeen,
            Deaths = comp.Deaths,
            Haunts = comp.Haunts.Count,
            Stand = comp.Stand,
            StandIsReal = comp.StandIsReal,
            CellList = _cells.GetRange(comp.Start, comp.Count).ToArray()
        };

    // ---------------------------------------------------------------------------------------------------------------------
    // The JSON of one zone
    // ---------------------------------------------------------------------------------------------------------------------

    private static void Write(Utf8JsonWriter json, Comp comp, BotZone zone, BotZoneSettings s, int w)
    {
        json.WriteStartObject();
        json.WriteNumber("id", zone.Id);
        json.WriteString("level", BotZones.Word(zone.Level));
        json.WriteString("label", zone.Label);
        json.WriteString("why", zone.Why);

        if (zone.Water)
        {
            json.WriteBoolean("water", true);
        }

        if (zone.Stray)
        {
            json.WriteBoolean("stray", true);
        }

        if (comp.Event != null)
        {
            json.WriteString("event", comp.Event);
        }

        json.WritePropertyName("center");
        json.WriteStartArray();
        json.WriteNumberValue(zone.CenterX);
        json.WriteNumberValue(zone.CenterY);
        json.WriteEndArray();

        json.WritePropertyName("bounds");
        json.WriteStartArray();
        json.WriteNumberValue(zone.X1);
        json.WriteNumberValue(zone.Y1);
        json.WriteNumberValue(zone.X2);
        json.WriteNumberValue(zone.Y2);
        json.WriteEndArray();

        var most = comp.Layer switch
        {
            Land => s.MostPoints,
            Sea => s.SeaMostPoints,
            _ => s.ClearMostPoints
        };

        // The patrol's outline off its own cells, in its own box.
        var cw = comp.MaxX - comp.MinX + 1;
        var ch = comp.MaxY - comp.MinY + 1;
        var core = new byte[cw * ch];

        for (var k = comp.Start; k < comp.Start + comp.Count; k++)
        {
            var cell = _cells[k];

            core[(cell / w - comp.MinY) * cw + cell % w - comp.MinX] = 1;
        }

        json.WritePropertyName("core");
        RingsJson(json, core, cw, ch, comp.MinX, comp.MinY, s, most);

        json.WritePropertyName("aggro");

        if (comp.Aggro == null)
        {
            json.WriteStartArray();
            json.WriteEndArray();
        }
        else
        {
            RingsJson(json, comp.Aggro, comp.AW, comp.AH, comp.AX, comp.AY, s, most);
        }

        json.WriteStartObject("mobs");
        json.WriteNumber("live", zone.Live);
        json.WriteNumber("max", zone.Max);
        json.WriteNumber("aggressive", zone.Aggressive);
        json.WriteNumber("casters", zone.Casters);
        json.WriteNumber("converge", zone.Converge);
        json.WriteStartArray("kinds");

        for (var k = 0; k < zone.Kinds.Length; k++)
        {
            var kind = zone.Kinds[k];

            json.WriteStartObject();
            json.WriteString("kind", kind.Kind);
            json.WriteNumber("live", kind.Live);
            json.WriteNumber("max", kind.Max);
            json.WriteNumber("power", Math.Round(kind.Power));
            json.WriteBoolean("aggressive", kind.Aggressive);
            json.WriteBoolean("caster", kind.Caster);
            json.WriteEndObject();
        }

        json.WriteEndArray();
        json.WriteNumber("others", zone.OtherKinds);
        json.WriteEndObject();

        json.WriteStartObject("power");
        json.WriteNumber("total", Math.Round(zone.PowerTotal));
        json.WriteNumber("worst", Math.Round(zone.Worst));
        json.WriteNumber("threat", Math.Round(zone.Threat));
        json.WriteNumber("vsBot", Math.Round(zone.VsBot, 2));
        json.WriteNumber("declared", Math.Round(zone.Declared));
        json.WriteEndObject();

        json.WriteStartObject("patrol");
        json.WriteNumber("declared", zone.DeclaredRange);

        if (zone.Samples > 0 && zone.Haunts > 0)
        {
            json.WriteNumber("observed", zone.ObservedRange);
        }
        else
        {
            json.WriteNull("observed");
        }

        json.WriteNumber("aggroRange", zone.AggroRange);

        // The farthest a spawner of it paints past its declaration on what was seen (08.10.2026); nought when none grew.
        json.WriteNumber("grown", comp.Grown);
        json.WriteEndObject();

        json.WriteNumber("confidence", Math.Round(zone.Confidence, 2));

        json.WriteStartObject("observed");
        json.WriteNumber("samples", zone.Samples);
        json.WriteNumber("awakeSeconds", Math.Round(zone.AwakeSeconds));

        if (zone.LastSeen == DateTime.MinValue)
        {
            json.WriteNull("lastSeen");
        }
        else
        {
            json.WriteString("lastSeen", zone.LastSeen.ToLocalTime().ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture));
        }

        json.WriteEndObject();

        json.WriteStartObject("deaths");
        json.WriteNumber("bots", zone.Deaths);
        json.WriteString("window", $"{s.DeathsWindowHours}h");
        json.WriteEndObject();

        json.WriteEndObject();
    }

    private static void Rings(Utf8JsonWriter json, List<int[]> rings)
    {
        json.WriteStartArray();

        for (var r = 0; r < rings.Count; r++)
        {
            var ring = rings[r];

            json.WriteStartArray();

            for (var k = 0; k + 1 < ring.Length; k += 2)
            {
                json.WriteStartArray();
                json.WriteNumberValue(ring[k]);
                json.WriteNumberValue(ring[k + 1]);
                json.WriteEndArray();
            }

            json.WriteEndArray();
        }

        json.WriteEndArray();
    }
}
