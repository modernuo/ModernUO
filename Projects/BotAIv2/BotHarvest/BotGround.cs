using System;
using System.Collections.Generic;
using System.Diagnostics;
using Server.Engines.Craft;
using Server.Engines.Harvest;
using Server.Logging;
using Server.Mobiles;
using Server.Regions;
using Server.Text;

namespace Server.BotAI.V2;

/// <summary>One remembered patch of workable rock, and what is in it.</summary>
public readonly struct BotSeam
{
    public BotSeam(Map map, Point3D where, string ore, double required)
    {
        Map = map;
        Where = where;
        Ore = ore;
        Required = required;
    }

    public Map Map { get; }

    public Point3D Where { get; }

    /// <summary>What the seam yields, by the ore's own type name. For the log, never branched on.</summary>
    public string Ore { get; }

    /// <summary>The mining skill the seam asks for. Below it the engine hands back plain iron instead.</summary>
    public double Required { get; }

    public bool Exists => Map != null;

    public override string ToString() => $"{Ore} at {Where}";
}

/// <summary>
/// What the population knows about the ground: where the rock is, where metal can be melted, and where
/// money and goods can be put away.
///
/// <para>
/// <b>Found by one bounded sweep, kept for everybody.</b> A bot standing in a town cannot see a mine and a
/// bot standing in a mine cannot see a forge, so knowledge of places cannot come from looking around: it
/// has to be swept for once and remembered. The first version reached the same conclusion the expensive
/// way — every workshop on this shard is part of the map rather than an object in it, so no spatial query
/// finds one, and a smith could only discover a forge by standing within two tiles of a forge it had no
/// reason to walk to. It never happened, all night.
/// </para>
///
/// <para>
/// <b>Swept where bots actually are, rather than around a list of towns.</b> The first version swept
/// vendor clusters and missed the only town that mattered: Britain's cluster centre is the average of its
/// shopkeepers' spawn points, which lands inside a wall 246 tiles from the smithy, so it recorded eight
/// forges on four facets and <em>none</em> on Felucca, where the whole population lived. A bot asking
/// where it may work triggers the sweep around itself, which cannot miss the place bots are.
/// </para>
///
/// <para>
/// This is not a persistent map and is not written to disk. It is rebuilt when the world is, because
/// everything in it is a fact about a world that has just been replaced.
/// </para>
/// </summary>
public static class BotGround
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotGround));

    /// <summary>
    /// How far around a bot one sweep looks. A hundred and sixty tiles each way: town and its outskirts,
    /// or a mine and the road out of it.
    /// </summary>
    public static int Reach { get; set; } = 160;

    /// <summary>
    /// How coarsely the sweep samples for rock. Mountains are hundreds of tiles across, so every fourth
    /// tile finds all of them at a sixteenth of the cost. Forges are one tile and are looked for on every
    /// tile — that difference is the reason there are two sampling rates in one pass.
    /// </summary>
    public static int Stride { get; set; } = 4;

    /// <summary>How far apart two rock tiles have to be to be remembered as two seams.</summary>
    public static int SeamSpacing { get; set; } = 16;

    /// <summary>
    /// How far a miner will walk before distance starts halving what a seam is worth to it.
    ///
    /// <para>
    /// <b>It was <see cref="SeamSpacing"/>, and that was one number doing two jobs.</b> Spacing is how far
    /// apart two surveyed seams have to be to count as different rock — a fact about bookkeeping, sixteen
    /// tiles, and correct. Read as patience it says a miner values a seam sixteen tiles off at half, one at
    /// a hundred and sixty at a eleventh, and one in the mountains at nothing at all. So every miner on the
    /// shard took whatever rock was nearest to town however poor it was, and the whole trade collected
    /// within sight of the walls.
    /// </para>
    ///
    /// <para>
    /// Eighty is about a minute's run. At that scale a seam in the mountains is worth a third of one at the
    /// gate rather than a twentieth, so richness can actually win — which is the point of having measured
    /// richness at all.
    /// </para>
    /// </summary>
    public static int Patience { get; set; } = 80;

    /// <summary>
    /// A place worth surveying whether or not a bot has ever walked past it.
    ///
    /// <para>
    /// <b>Every sweep this shard has ever made happened around a bot, which means the map it holds is a map
    /// of where bots already were.</b> That is fine for finding the forge and the bank, and it is exactly
    /// wrong for ore: the good rock is in the mountains, no bot has a reason to go to the mountains until
    /// there is rock recorded there, and there is no rock recorded there because no bot has been. A closed
    /// circle, and the only way out of it is to name one place from outside.
    /// </para>
    ///
    /// <para>
    /// The open cave at (1446, 1227) on Felucca, by order. One sweep of it at boot puts real seams on the
    /// board a quarter of the roam away, and from there the ordinary arithmetic does the rest — patience
    /// carries a miner that far, and what the population learns about what came out of the ground carries
    /// the next one.
    /// </para>
    /// </summary>
    public static Point3D Lode { get; set; } = new(1446, 1227, 0);

    /// <summary>
    /// Sweeps the named lode, once, so that there is ore on the board somewhere no bot has yet been.
    ///
    /// Called from the module's start rather than lazily, because the question it answers — is there
    /// anywhere outside the walls worth walking to — is asked by the first miner in the first minute, and an
    /// answer that arrives later is an answer that arrives after the habit has formed.
    /// </summary>
    public static int Prospect(Map map)
    {
        if (map == null || map == Map.Internal || Lode == Point3D.Zero)
        {
            // Said rather than returned quietly. A nought here means the mountains were never put on the
            // board and every miner will spend the session within sight of the walls, which is a large
            // consequence for a silence.
            logger.Error(
                "The lode at ({X}, {Y}) could not be prospected: there is no facet to sweep it on, so no ore outside the walls has been recorded",
                Lode.X,
                Lode.Y
            );

            return 0;
        }

        Survey(map, Lode);

        // Walked a slice a tick from here (Sweep, 01.10.2026, build 382): what it found is said by the sweep's own line.
        logger.Information(
            "The lode at ({X}, {Y}) is queued to be swept; {Total} seams on the board before it",
            Lode.X,
            Lode.Y,
            _seams.Count
        );

        return 0;
    }

    /// <summary>How far apart two forges have to be to be two workshops. A forge is several tiles wide.</summary>
    public static int PlaceSpacing { get; set; } = 12;

    /// <summary>How near a forge an anvil must stand for the pair to be a workshop.</summary>
    public static int AnvilReach { get; set; } = 3;

    /// <summary>
    /// How far above or below a bot's own feet a workshop may stand and still be its workshop.
    ///
    /// <para>
    /// <b>Two of this island's four forges stand on an upper storey and cost the shard 93 errands in one
    /// night.</b> (1424, 1558, 30) and (1361, 1574, 30) are <c>SmallForgeAddon</c>s thirty units above the
    /// street, on floors whose only standable surface is that same thirty. <see cref="HasAnvil"/> sweeps for
    /// items at <c>z = 0</c>, so a forge three storeys up is found from the pavement, filed, chosen as the
    /// nearest, walked at, and refused — over and over, because nothing in the choosing ever asked how high
    /// it was.
    /// </para>
    ///
    /// <para>
    /// Asked of the bot rather than of the place, which is the same rule the reach ledger keeps: height is a
    /// fact about a bot and a place together. A smith that ever does find itself on that floor may use that
    /// forge. Twenty-four is <see cref="BotStep.StandingReach"/> — two storeys, enough for stairs, a cellar
    /// or a hillside, not enough for a roof — and it is the number the debugger's own <c>tile</c> verb uses
    /// to answer "does a body fit here". Set it past 127 to turn the rule off on a running shard.
    /// </para>
    /// </summary>
    public static int Storey { get; set; } = BotStep.StandingReach;

    /// <summary>Choices steered away from a workshop on a floor the asker is not on. See <see cref="Storey"/>.</summary>
    public static long Upstairs { get; private set; }

    /// <summary>Choices steered away from ground that has refused the population. See <see cref="BotRefused"/>.</summary>
    public static long Refused { get; private set; }

    /// <summary>
    /// <b>Rock beside something that has killed lately is not this bot's rock (29.09.2026).</b> Seven of the thirteen deaths
    /// in the last half hour of build 293 were miners, at seams beside ettins, harpies and trolls. A seam whose quadrant
    /// reads a <c>BotQuad.Muscle</c> past the bot's strength times this is passed over, at the odds the roads
    /// (<c>BotTraveller.Odds</c>) and the caves (<c>BotVenturer.Odds</c>) use. Nought turns it off.
    /// </summary>
    public static double MineOdds { get; set; } = 1.5;

    /// <summary>Seams passed over for lying where something past the bot's strength has killed lately. See <see cref="MineOdds"/>.</summary>
    public static long Outmuscled { get; private set; }

    /// <summary>Seams passed over for a road that asks more than the miner brings. See <see cref="BotDanger.Road"/>.</summary>
    public static long RoadOutmuscled { get; private set; }

    /// <summary>Seams passed over because a walk had proved nobody can stand at them. See the note in <c>Choose</c>.</summary>
    public static long SeamsUnfooted { get; private set; }

    /// <summary>
    /// Most seams remembered at once.
    ///
    /// <para>
    /// <b>Ninety-six was sized for sweeps that happen around bots, and a named lode is not one of those.</b>
    /// Every sweep until 27.08.2026 was a bot's own surroundings — a few dozen tiles of rock each, from
    /// wherever the population happened to be — so the list filled slowly and from everywhere. The first
    /// sweep of the cave at (1446, 1227) returned ninety-six seams by itself and filled the board outright,
    /// and a full board <em>refuses</em> everything after it: no rock anywhere else on the island could be
    /// recorded for the rest of the session, including the town rock the new rule was written to refuse.
    /// The ban read nought, which looked like the ban working and was the ban never being asked.
    /// </para>
    ///
    /// <para>
    /// <para>
    /// <b>Five hundred filled too, and it filled in twenty minutes.</b> With the sweep ceiling raised from
    /// sixteen to sixty-four on 08.09.2026 the list reached 511 of 512 before the shard was half an hour
    /// old, and a full list refuses everything after it — including the rock that replaces what the miners
    /// are working out. Seams are struck off as they are emptied, so a ceiling that binds is a population
    /// that runs out of ground to dig on a shard which still has plenty.
    /// </para>
    ///
    /// <para>
    /// Two thousand. The cost is one comparison per seam per miner per review, and a review is every fifteen
    /// seconds for eleven miners; the sweep's own duplicate check is the quadratic one, and at this size a
    /// three-hundred-seam sweep is under a million comparisons — milliseconds.
    /// </para>
    ///
    /// Raised rather than made to evict, and the difference matters. Eviction needs a rule for which seam to
    /// throw away, and every rule available here is wrong: the poorest is the one a novice can work, the
    /// furthest is the mountains this was all done to reach, the oldest is whatever the shard learned first.
    /// Five hundred is a few sweeps' worth of a large island and costs one comparison per seam per miner per
    /// review, which against the movement budget is nothing.
    /// </para>
    /// </summary>
    public static int MaxSeams { get; set; } = 2048;

    /// <summary>
    /// Hearths, fires and counters kept. <b>128 since build 320 (29.09.2026):</b> the bots live in the other towns now
    /// (BotResidence), and 48 was one budget for the whole island — 19 hearths were already known from Britain alone.
    /// </summary>
    public static int MaxPlaces { get; set; } = 128;

    /// <summary>
    /// How many sweeps the population may run in one world. A backstop, not a policy: a sweep is tens of
    /// thousands of tile reads, and a population wandering a continent should not be able to spend its
    /// afternoon surveying it.
    ///
    /// <para>
    /// <b>Sixteen was the backstop and it became the budget, which is the second time that happened in one
    /// day.</b> A sweep covers 250 tiles around a bot and a fresh one is refused within 125 of an old one,
    /// so sixteen of them cover a patch a couple of thousand tiles across at best — and the population is
    /// allowed to want things a thousand tiles from home in every direction, which is a square four times
    /// that. On 08.09.2026 the shard hit the ceiling and said so, once, at error level, and the consequences
    /// arrived wearing other names: "Roderic could not be offered mining: no fire within its own reach",
    /// "found no shopkeeper selling cloth within its own reach", two counters and four forges known to a
    /// population of forty-nine.
    /// </para>
    ///
    /// <para>
    /// Sixty-four, and the cost is bounded by what a sweep actually measures at: this shard's own log has
    /// them between 7ms and 156ms, most of them under 20ms, spread over hours. The whole allowance is a
    /// couple of seconds of game loop across a session — against a population that could not find a bank.
    /// See BotQuad.Most for the same defect on the island map, found the same afternoon.
    /// </para>
    ///
    /// <para>
    /// <b>192 since build 320 (29.09.2026):</b> a population living in every fit town sweeps round each of them, and sixty-four
    /// was sized for one town's hinterland.
    /// </para>
    /// </summary>
    public static int MaxSurveys { get; set; } = 192;

    private static readonly List<BotSeam> _seams = [];

    private static readonly List<(Map Map, Point3D Where)> _fires = [];

    private static readonly List<(Map Map, Point3D Where)> _hearths = [];

    private static readonly List<(Map Map, Point3D Where)> _counters = [];

    private static readonly List<(Map Map, Point3D Where)> _surveyed = [];

    private static bool _saidCapped;

    private static bool _saidFullOfSeams;

    public static IReadOnlyList<BotSeam> Seams => _seams;

    public static IReadOnlyList<(Map Map, Point3D Where)> Fires => _fires;

    /// <summary>
    /// Every fire on record, which is a wider thing than <see cref="Fires"/>.
    ///
    /// <para>
    /// <b>A forge is a fire, but most fires are not forges.</b> The list above is a list of workshops: a
    /// forge that also has an anvil beside it, because that pair is what a smith needs and remembering a
    /// lone forge would send one on a walk to a place it cannot work. A cook needs neither the anvil nor
    /// the forge — it needs heat, and the engine counts ovens, fireplaces, campfires, firepits, heating
    /// stands and braziers as heat exactly as it counts a forge. Kept apart rather than merged, because
    /// widening the smith's list to let the cook work would send every smith to a bakery.
    /// </para>
    /// </summary>
    public static IReadOnlyList<(Map Map, Point3D Where)> Hearths => _hearths;

    public static IReadOnlyList<(Map Map, Point3D Where)> Counters => _counters;

    public static int Surveys => _surveyed.Count;

    /// <summary>
    /// Seams passed over because there was known to be no way through to them.
    ///
    /// A named number: "the miners have worked out all the rock" and "the rock they can see is behind a
    /// wall" are different facts about the island and were the same silence.
    /// </summary>
    public static long Walled { get; private set; }

    /// <summary>Seams passed over because the ground they stand on has lately refused somebody. See <see cref="Seam(IBotWilful, Point3D)"/>.</summary>
    public static long Unwalked { get; private set; }

    /// <summary>Asks for a sweep made from inside a dungeon, and refused. See <see cref="Survey"/>.</summary>
    public static long Underground { get; private set; }

    /// <summary>A forge, in either of the two forms this shard has them in.</summary>
    public static bool IsForgeId(int id) => id is 4017 or (>= 6522 and <= 6569) or 11736;

    /// <summary>
    /// Anything the engine will cook over, asked of the engine rather than answered here.
    ///
    /// See <c>CraftItem.IsHeatSource</c>. A second copy of that table living in this file is a second thing
    /// to keep in step, and the one that fell out of step would fail the way this defect failed: silently,
    /// with a bot swinging a skillet at nothing.
    /// </summary>
    public static bool IsHearthId(int id) => CraftItem.IsHeatSource(id);

    /// <summary>An anvil, which is the other half of a workshop.</summary>
    public static bool IsAnvilId(int id) => id is 4015 or 4016 or 11733 or 11734;

    /// <summary>
    /// A point past the edge of what has been swept, for somebody to go and look at.
    ///
    /// <para>
    /// <b>Outwards from the town, in the direction of the lode and beyond it.</b> The named lode is the one
    /// place on this island known from outside to be worth walking to, so the line from home through it is
    /// the best guess the shard has about where more rock lies — and past the lode is the only part of that
    /// line nobody has swept. Stepped out one sweep's width at a time so that each trip is the next ring
    /// rather than a leap into the sea, and refused once the map runs out.
    /// </para>
    ///
    /// <para>
    /// Nought when everything within a day's walk has been looked at. That is a real answer and not a
    /// failure: an island whose ground is all known and all barren is a fact about the island, and the
    /// summary should be able to say it rather than send bots out for ever.
    /// </para>
    /// </summary>
    public static Point3D Frontier(Map map, Point3D from)
    {
        if (map == null || map == Map.Internal || Lode == Point3D.Zero)
        {
            return Point3D.Zero;
        }

        var dx = Lode.X - from.X;
        var dy = Lode.Y - from.Y;
        var span = Math.Max(1, (int)Math.Sqrt(dx * dx + dy * dy));

        // Ring by ring, starting at the lode itself and stepping out by a sweep each time.
        for (var step = 1; step <= Rings; step++)
        {
            var out0 = span + step * Reach;
            var x = from.X + dx * out0 / span;
            var y = from.Y + dy * out0 / span;

            if (x < 8 || y < 8 || x >= map.Width - 8 || y >= map.Height - 8)
            {
                break;
            }

            // A height taken from the map rather than invented: a Z of nought out of arithmetic is a place
            // that works on a plain and cannot be stood on anywhere else. See BotStep.Settle.
            if (!BotStep.Settle(map, x, y, out var z))
            {
                continue;
            }

            var where = new Point3D(x, y, z);

            // Ground the population has already failed to reach is not worth surveying: the prospector would
            // walk at it, give up, and hand the same square to the next one. See BotRefused.
            if (BotRefused.Refusing(map, where))
            {
                continue;
            }

            if (!Surveyed(map, where))
            {
                return where;
            }
        }

        return Point3D.Zero;
    }

    /// <summary>How many sweeps out from the lode the prospector will consider. Half a map's worth.</summary>
    public static int Rings { get; set; } = 12;

    /// <summary>Whether this patch of the world has already been swept.</summary>
    public static bool Surveyed(Map map, Point3D around)
    {
        for (var i = 0; i < _surveyed.Count; i++)
        {
            if (_surveyed[i].Map == map && Utility.InRange(_surveyed[i].Where, around, Reach / 2))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Asks for the ground around a point to be swept once: rock, fires and counters in a single pass over the tiles,
    /// plus one spatial query each for the counters and the fires that are items. The sweep itself is walked a slice a
    /// tick by <see cref="Sweep"/>.
    ///
    /// <para>
    /// <b>Queued rather than walked here, because it was walked on one bot's turn and the loop stood still for it
    /// (01.10.2026, build 382).</b> A sweep is 501 × 501 tiles at a reach of 250, every tile's statics read for a forge and
    /// a hearth and every fourth in both directions examined for rock; warm, 10 to 29ms, and on ground the engine has not
    /// read from disk yet 116, 143, 596 and 701ms — on the evening of 30.09.2026 every session's first slow ticks were
    /// those, the whole tick "in the bots' turns". Rule 3 of the fork: heavy work that needs game state is chunked across
    /// ticks. So the first bot to ask no longer pays; the population's clock does, <see cref="SliceMs"/> at a time, and
    /// whoever asks meanwhile is told the ground is being read (<see cref="Sweeping"/>).
    /// </para>
    /// </summary>
    public static void Survey(Map map, Point3D around)
    {
        if (map == null || map == Map.Internal || Surveyed(map, around))
        {
            return;
        }

        // <b>Not from underground.</b> A gatherer in a delve party is asked about mining like any other, and the sweep round
        // its feet filed the dungeon: every session of 24.09.2026 swept the Orc Caves at (5303-5318, 1306-1336) and took
        // 182-188 seams, 5 fires and 22-35 braziers from it, which filled the hearths to MaxPlaces (48) — and every sweep
        // of the island after it found "0 hearths" whatever stood there (21:07 round (841, 1910), 21:12 round (1900, 1156)).
        // The trades are worked on the island; a dungeon is a delve's, and its fires are nobody's kitchen.
        if (BotDungeon.Under(around))
        {
            Underground++;

            return;
        }

        if (_surveyed.Count >= MaxSurveys)
        {
            // Said once. Otherwise a population that has wandered past its sweep allowance simply stops
            // finding work, and the census would report bots with nothing worth doing without anything
            // anywhere explaining why.
            if (!_saidCapped)
            {
                _saidCapped = true;

                logger.Error(
                    "The ground has been swept {Count} times, which is the limit; bots further out will find no seams, fires or counters",
                    _surveyed.Count
                );
            }

            return;
        }

        // On the surveyed list from the asking, so a second bot standing in the same ground asks for no second sweep.
        _surveyed.Add((map, around));
        _pending.Add((map, around));
    }

    /// <summary>
    /// How much of one tick a sweep may take. Six milliseconds: with the bots' turns beside it a tick stays under
    /// <c>BotBeat.SlowMs</c>, and a warm sweep of 20ms is three or four ticks, a cold one of 700ms a dozen seconds.
    /// </summary>
    public static double SliceMs { get; set; } = 6.0;

    /// <summary>Slices walked, what they cost the loop in all, and the worst one. For the summary.</summary>
    public static long Slices { get; private set; }

    public static double SweptMs { get; private set; }

    public static double SliceWorstMs { get; private set; }

    /// <summary>Sweeps asked for and not yet begun, oldest first. See <see cref="Survey"/>.</summary>
    private static readonly List<(Map Map, Point3D Around)> _pending = [];

    private static bool _sweeping;

    private static Map _sweepMap;

    private static Point3D _sweepAround;

    /// <summary>The next column of the sweep under way. See <see cref="Sweep"/>.</summary>
    private static int _sweepX;

    private static int _sweepSeams;

    private static int _sweepFires;

    private static int _sweepHearths;

    private static int _sweepSlices;

    private static double _sweepMs;

    /// <summary>
    /// Whether the ground round this point is asked for and not yet read: queued, or under way. What a proposer asks
    /// before saying there is no fire here.
    /// </summary>
    public static bool Sweeping(Map map, Point3D around)
    {
        if (map == null)
        {
            return false;
        }

        if (_sweeping && _sweepMap == map && Utility.InRange(_sweepAround, around, Reach / 2))
        {
            return true;
        }

        for (var i = 0; i < _pending.Count; i++)
        {
            if (_pending[i].Map == map && Utility.InRange(_pending[i].Around, around, Reach / 2))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// One slice of the sweep under way, or the start of the next one waiting. Called by <c>BotBeat</c> every tick;
    /// costs a count when nothing is waiting. See <see cref="Survey"/> for why it is not walked whole.
    /// </summary>
    public static void Sweep()
    {
        if (!_sweeping)
        {
            if (_pending.Count == 0)
            {
                return;
            }

            (_sweepMap, _sweepAround) = _pending[0];
            _pending.RemoveAt(0);

            if (_sweepMap == null || _sweepMap == Map.Internal)
            {
                return;
            }

            _sweeping = true;
            _sweepX = _sweepAround.X - Reach;
            _sweepSeams = 0;
            _sweepFires = 0;
            _sweepHearths = 0;
            _sweepSlices = 0;
            _sweepMs = 0.0;
        }

        var started = Stopwatch.GetTimestamp();
        var deadline = started + (long)(SliceMs * Stopwatch.Frequency / 1000.0);
        var map = _sweepMap;
        var around = _sweepAround;
        var system = Mining.System;
        var last = around.X + Reach;
        var done = false;

        // A column a go between looks at the clock: 501 tiles, tens of microseconds warm and a millisecond or so cold.
        while (true)
        {
            if (_sweepX > last)
            {
                done = true;

                break;
            }

            var x = _sweepX++;

            if (x >= 0 && x < map.Width)
            {
                for (var y = around.Y - Reach; y <= around.Y + Reach; y++)
                {
                    if (y < 0 || y >= map.Height)
                    {
                        continue;
                    }

                    NoteTile(map, x, y, ref _sweepFires, ref _sweepHearths);

                    if (system == null || x % Stride != 0 || y % Stride != 0)
                    {
                        continue;
                    }

                    if (NoteSeam(map, x, y, system))
                    {
                        _sweepSeams++;
                    }
                }
            }

            if (Stopwatch.GetTimestamp() >= deadline)
            {
                break;
            }
        }

        var ms = (Stopwatch.GetTimestamp() - started) * 1000.0 / Stopwatch.Frequency;

        _sweepMs += ms;
        _sweepSlices++;
        Slices++;
        SweptMs += ms;

        if (ms > SliceWorstMs)
        {
            SliceWorstMs = ms;
        }

        if (!done)
        {
            return;
        }

        // The columns are walked: the queries, and the line, as one sweep always ended.
        _sweepFires += NoteItemFires(map, around);
        _sweepHearths += NoteItemHearths(map, around);

        var counters = NoteCounters(map, around);

        _sweeping = false;

        // Named and counted, because a count on its own is not knowledge: the first version had eight
        // forges on record all night while a smith standing on Felucca with a pack of ore was told there
        // were none, and no number could have said which of the two was wrong.
        logger.Information(
            "Swept {Reach} tiles around {Where} on {Map} in {Elapsed:F0}ms over {Slices} slices: {Seams} seams, {Fires} fires, {Hearths} hearths, {Counters} counters (now {AllSeams}, {AllFires}, {AllHearths}, {AllCounters})",
            Reach,
            around,
            map,
            _sweepMs,
            _sweepSlices,
            _sweepSeams,
            _sweepFires,
            _sweepHearths,
            counters,
            _seams.Count,
            _fires.Count,
            _hearths.Count,
            _counters.Count
        );

        // <b>And which fires, by address.</b> A count said "four forges" all night while two of them stood
        // thirty units above the street and could not be reached by anybody; no number could have told the
        // two apart, and finding out took a person asking the debugger about five coordinates by hand. There
        // are never more than a few dozen, so naming them costs one line a sweep and answers the question
        // outright. See Storey.
        if (_sweepFires > 0)
        {
            var where = ValueStringBuilder.Create(256);

            try
            {
                for (var i = 0; i < _fires.Count; i++)
                {
                    if (i > 0)
                    {
                        where.Append(", ");
                    }

                    where.Append('(');
                    where.Append(_fires[i].Where.X);
                    where.Append(", ");
                    where.Append(_fires[i].Where.Y);
                    where.Append(", ");
                    where.Append(_fires[i].Where.Z);
                    where.Append(')');
                }

                logger.Information(
                    "The fires now known, by address, against a population standing at z {Feet}: {Fires}",
                    BotPopulation.Where.Z,
                    where.ToString()
                );
            }
            finally
            {
                where.Dispose();
            }
        }
    }

    private static bool NoteSeam(Map map, int x, int y, HarvestSystem system)
    {
        if (_seams.Count >= MaxSeams)
        {
            // Said once, for the reason the sweep ceiling is said once: a list that has stopped growing looks
            // exactly like an island with no more rock in it, and the difference only shows up hours later as
            // miners with nowhere to dig. See MaxSurveys.
            if (!_saidFullOfSeams)
            {
                _saidFullOfSeams = true;

                logger.Error(
                    "The seam list is full at {Max} and no more rock will be recorded anywhere on the island",
                    MaxSeams
                );
            }

            return false;
        }

        if (BotOre.Examine(map, x, y, system) == null)
        {
            return false;
        }

        var where = new Point3D(x, y, map.GetAverageZ(x, y));

        // <b>Nobody digs inside the walls, and that is a property of the tile rather than an opinion about
        // the moment.</b> The rule was enforced where a seam is chosen, so every miner asked it of every
        // guarded seam on every beat, for the life of the shard: 120 277 region lookups in ninety minutes on
        // 03.09.2026, about twenty-two a second, all of them arriving at the answer they arrived at the first
        // time. Asked once here instead, and such rock never enters the list — which also keeps the list
        // short, since the castle is built into a hillside and the survey sees a great deal of it.
        //
        // Kept as a counter under the same name so the line still says how much rock the walls are holding.
        if (Region.Find(where, map)?.IsPartOf<GuardedRegion>() == true)
        {
            Townbound++;

            return false;
        }

        for (var i = 0; i < _seams.Count; i++)
        {
            if (_seams[i].Map == map && Utility.InRange(_seams[i].Where, where, SeamSpacing))
            {
                return false;
            }
        }

        var vein = BotOre.VeinAt(map, x, y);

        _seams.Add(new BotSeam(map, where, BotOre.NameOf(vein), vein?.ReqSkill ?? 0.0));

        return true;
    }

    /// <summary>
    /// Both kinds of fire on one tile, out of one read of it.
    ///
    /// <para>
    /// <b>One read where there were two (01.10.2026, build 382).</b> The forge and the hearth were asked for by two methods,
    /// each reading the tile's statics for itself, so every tile of a sweep — 251,001 at a reach of 250 — was read twice, and
    /// the read is the sweep's whole cost. The first forge tile on (x, y) decides the forge and the first hearth tile the
    /// hearth, as the two did apart.
    /// </para>
    /// </summary>
    private static void NoteTile(Map map, int x, int y, ref int fires, ref int hearths)
    {
        var wantFire = _fires.Count < MaxPlaces;
        var wantHearth = _hearths.Count < MaxPlaces;

        if (!wantFire && !wantHearth)
        {
            return;
        }

        var fireSeen = false;
        var hearthSeen = false;

        foreach (var tile in map.Tiles.GetStaticAndMultiTiles(x, y))
        {
            if (wantFire && !fireSeen && IsForgeId(tile.ID))
            {
                fireSeen = true;

                var where = new Point3D(x, y, tile.Z);

                // A fire on its own is a fire. Smelting only needs the fire, but a workshop is what a crafter
                // will want later, and remembering the pair costs nothing now.
                if (HasAnvil(map, x, y) && !Known(_fires, map, where) && Footed(map, where))
                {
                    _fires.Add((map, where));
                    fires++;
                }
            }

            if (wantHearth && !hearthSeen && IsHearthId(tile.ID))
            {
                hearthSeen = true;

                var where = new Point3D(x, y, tile.Z);

                if (!Known(_hearths, map, where) && Footed(map, where))
                {
                    _hearths.Add((map, where));
                    hearths++;
                }
            }

            if ((fireSeen || !wantFire) && (hearthSeen || !wantHearth))
            {
                break;
            }
        }
    }

    /// <summary>
    /// Forges that are objects rather than map tiles, in one query for the whole sweep.
    ///
    /// <para>
    /// Both kinds have to be looked for and the first version proved why: its sweeps found forges as
    /// statics on four facets and <em>none</em> on Felucca, where every bot lived, because on this shard
    /// Felucca's smithies were placed by the decoration pass as ordinary items.
    /// </para>
    ///
    /// <para>
    /// One spatial query rather than one per tile. Asking per tile would be a hundred thousand queries for
    /// a sweep, which is the difference between a sweep costing milliseconds and costing a visible pause.
    /// </para>
    /// </summary>
    private static int NoteItemFires(Map map, Point3D around)
    {
        var found = 0;

        foreach (var item in map.GetItemsInRange(around, Reach))
        {
            if (item.Deleted || _fires.Count >= MaxPlaces || !IsForgeId(item.ItemID))
            {
                continue;
            }

            var where = item.GetWorldLocation();

            if (Known(_fires, map, where) || !HasAnvil(map, where.X, where.Y) || !Footed(map, where))
            {
                continue;
            }

            _fires.Add((map, where));
            found++;
        }

        return found;
    }


    /// <summary>Fires that are objects rather than map tiles. See <see cref="NoteItemFires"/>.</summary>
    private static int NoteItemHearths(Map map, Point3D around)
    {
        var found = 0;

        foreach (var item in map.GetItemsInRange(around, Reach))
        {
            if (item.Deleted || _hearths.Count >= MaxPlaces || !IsHearthId(item.ItemID))
            {
                continue;
            }

            var where = item.GetWorldLocation();

            if (Known(_hearths, map, where) || !Footed(map, where))
            {
                continue;
            }

            _hearths.Add((map, where));
            found++;
        }

        return found;
    }

    private static int NoteCounters(Map map, Point3D around)
    {
        var found = 0;

        // Bankers are mobiles, so unlike everything else here they answer a spatial query. One query for
        // the whole sweep.
        foreach (var banker in map.GetMobilesInRange<Banker>(around, Reach))
        {
            if (banker.Deleted || _counters.Count >= MaxPlaces)
            {
                continue;
            }

            var where = banker.Location;

            if (Known(_counters, map, where))
            {
                continue;
            }

            _counters.Add((map, where));
            found++;
        }

        return found;
    }

    /// <summary>
    /// Whether a body could stand at this workshop, or beside it, at the height the workshop is at.
    ///
    /// <para>
    /// <b>A place with no floor under it was filed as work and walked at forty-three times.</b> The hearth
    /// at (1512, 1426, 15) answers the debugger with "the floor could not be found" - no standable surface
    /// at that tile at any height - and 43 of the 206 cooking errands that failed on the road in one session
    /// went to it. Nothing between the filing and the walking ever asked the question, so every cook in
    /// range was handed it in turn.
    /// </para>
    ///
    /// <para>
    /// The same test <see cref="BotPath"/> makes when it turns a destination into a place to walk to, in the
    /// same order and with the same tolerances: the tile's own height first, the floor under it second if
    /// that floor is within a person's height of what was asked for, and the ring around it last. Written
    /// out here rather than borrowed because the one in BotPath is private to the search and takes an
    /// arrival with it; if the two ever disagree, this is the copy that is wrong.
    /// </para>
    /// </summary>
    private static bool Footed(Map map, Point3D where)
    {
        if (!NeedFooting)
        {
            return true;
        }

        var z = (sbyte)Math.Clamp(where.Z, sbyte.MinValue, sbyte.MaxValue);

        if (BotStep.Mask(map, where.X, where.Y, z).WalkMask != 0)
        {
            return true;
        }

        if (BotStep.Settle(map, where.X, where.Y, out var under)
            && Math.Abs(under - where.Z) <= BotArrival.PersonHeight)
        {
            return true;
        }

        // <b>Tighter than the tile's own test, on both counts, and the loose version let the worst hearth on
        // the island straight back in.</b> The first draft copied BotPath.Footing exactly: a ring of two
        // tiles, accepting any floor within a person's height. (1512, 1426, 15) passed it — the hearth is
        // fifteen units up, the street below is at nought, and fifteen is inside sixteen — so it was filed,
        // and Roderic walked at it until his stamina ran out.
        //
        // Two corrections. The ring is one tile, because that is the arrival almost every errand actually
        // asks for and BotPath then measures its own footing at; two was a copy of a ceiling, not of the
        // real reach. And the height is half a person rather than a whole one: a brazier standing on a table
        // is six units above the floor beside it and must still count, while fifteen is a storey and the
        // floor beside it is a different room's.
        for (var dx = -FootingSweep; dx <= FootingSweep; dx++)
        {
            for (var dy = -FootingSweep; dy <= FootingSweep; dy++)
            {
                if ((dx != 0 || dy != 0)
                    && BotStep.Settle(map, where.X + dx, where.Y + dy, out var rz)
                    && Math.Abs(rz - where.Z) <= BotArrival.PersonHeight / 2)
                {
                    return true;
                }
            }
        }

        Unfooted++;

        return false;
    }

    /// <summary>
    /// How far around a workshop to look for something to stand on.
    ///
    /// One tile: the arrival nearly every errand asks for is <c>Beside</c>, and BotPath measures its footing
    /// over exactly the tiles the arrival allows. Two was copied from that method's ceiling rather than from
    /// its reach, and the difference filed a hearth a storey above the street.
    /// </summary>
    public static int FootingSweep { get; set; } = 1;

    /// <summary>
    /// Whether a workshop has to have somewhere to stand before it is filed at all.
    ///
    /// <para>
    /// A switch rather than a constant because this rule removes places from the shard's list and there is
    /// no undoing that on a running world - a swept square is never swept again. If a morning ever finds the
    /// island with no fires on it, this is the number to put back and restart, and the survey line names
    /// every fire by address so that argument can be had with facts.
    /// </para>
    /// </summary>
    public static bool NeedFooting { get; set; } = true;

    /// <summary>Workshops refused for having no floor at them or beside them. See <see cref="Footed"/>.</summary>
    public static long Unfooted { get; private set; }

    private static bool HasAnvil(Map map, int x, int y)
    {
        for (var dx = -AnvilReach; dx <= AnvilReach; dx++)
        {
            for (var dy = -AnvilReach; dy <= AnvilReach; dy++)
            {
                foreach (var tile in map.Tiles.GetStaticAndMultiTiles(x + dx, y + dy))
                {
                    if (IsAnvilId(tile.ID))
                    {
                        return true;
                    }
                }
            }
        }

        foreach (var item in map.GetItemsInRange(new Point3D(x, y, 0), AnvilReach))
        {
            if (IsAnvilId(item.ItemID))
            {
                return true;
            }
        }

        return false;
    }

    private static bool Known(List<(Map Map, Point3D Where)> places, Map map, Point3D where)
    {
        for (var i = 0; i < places.Count; i++)
        {
            if (places[i].Map == map && Utility.InRange(places[i].Where, where, PlaceSpacing))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// The seam this bot should go to, or one that does not exist when there is none.
    ///
    /// <para>
    /// <b>Richness against distance, decided here rather than by the appraisal.</b> The decision layer
    /// weighs one offer per proposer, so if this handed over the richest seam on the facet it would be
    /// offering a valorite vein three hundred tiles away against nothing, and the appraisal would have no
    /// nearer alternative to prefer. Comparing seams is this subsystem's own job, because it is the only
    /// one that knows what is in them.
    /// </para>
    ///
    /// <para>
    /// Places the bot's own ledger is wary of are skipped: a seam where its last trip went badly is not
    /// worth offering, and offering it anyway wastes the one offer this proposer gets.
    /// </para>
    /// </summary>
    public static BotSeam Seam(IBotWilful bot) => Seam(bot, Point3D.Zero);

    /// <summary>
    /// How long a bot's answer to "which seam" stands before the list is walked for it again.
    ///
    /// <para>
    /// <b>Patrick's order of 03.09.2026: put a cooldown on asking, two or three seconds, so they stop
    /// spamming it a hundred and twenty thousand times.</b> The scan is not one lookup — it is every
    /// remembered seam, each one asked whether somebody else is on it, whether the ledger is wary of it,
    /// whether there is a way through, and what the population has been paid out of that hillside. There are
    /// four hundred of them and thirty-four bots asking on their own beats.
    /// </para>
    ///
    /// <para>
    /// Nothing is lost by the delay. Seams move on the timescale of a mining trip, not of a beat, and the
    /// two things that do change quickly — somebody claiming a seam, and a road being proved shut — are both
    /// re-checked by the errand itself when it gets there. A bot that just finished a vein waits at most
    /// this long to be told where the next one is, and it is already walking home with a full pack.
    /// </para>
    /// </summary>
    public static int AskEveryMs { get; set; } = 2500;

    /// <summary>What each bot was last told, and when. Keyed by serial, pruned with the bot.</summary>
    private static readonly Dictionary<Serial, (long Tick, Point3D Except, BotSeam Seam)> _told = [];

    /// <summary>Scans of the seam list that were answered out of the last one instead.</summary>
    public static long Spared { get; private set; }

    /// <summary>A bot that is gone should not be remembered, or the table grows for the life of the shard.</summary>
    public static void Forget(Mobile bot)
    {
        if (bot != null)
        {
            _told.Remove(bot.Serial);
        }
    }

    /// <summary>
    /// Whether this bot has been told lately, and what it was told.
    ///
    /// The exception is part of the key rather than ignored: an errand that has just proved one seam
    /// unreachable is asking a different question from the one asked a second ago, and answering it out of
    /// the cache would hand back the very seam it is trying to get away from.
    /// </summary>
    private static bool Told(IBotWilful bot, Mobile body, Point3D except, out BotSeam seam)
    {
        seam = default;

        if (!_told.TryGetValue(body.Serial, out var last) || last.Except != except)
        {
            return false;
        }

        if (Core.TickCount - last.Tick >= AskEveryMs)
        {
            return false;
        }

        // <b>Unless this bot has been made wary of that seam since.</b> The ledger is the bot's own, so what writes
        // to it cannot reach this table the way Barren and Drained do; it is asked here instead, one lookup, and
        // only of an answer about to be served.
        // And unless another miner has taken the seam since: the hold is taken on the walk now, and an answer served past
        // it would send this bot to arrive second. See BotDig's long walk.
        if (last.Seam.Exists
            && (bot.Resolve?.Ledger?.Cautious(BotDig.Trade, last.Seam.Map, last.Seam.Where) == true || !Free(body, last.Seam.Where)))
        {
            _told.Remove(body.Serial);
            Stale++;

            return false;
        }

        seam = last.Seam;
        Spared++;

        return true;
    }

    /// <summary>Answers taken back out of the cache because their seam had left the board or been feared since.</summary>
    public static long Stale { get; private set; }

    private static readonly List<Serial> _untold = [];

    /// <summary>
    /// Takes a seam that has just left the board out of every answer still being served from the cache.
    ///
    /// <para>
    /// <b>The cache outlived the fact it was built from.</b> <see cref="AskEveryMs"/> promised that nothing is lost
    /// by two and a half seconds because seams change on the timescale of a trip — and one thing changes on the
    /// timescale of a beat: the trip that fails on arrival. Quill at 23:00:41 on 14.09.2026 took mine, found no
    /// rock, struck the seam off, and was handed the same seam out of the cache on each of the next five beats;
    /// six failures in a second, the breaker tripped, and mining rested for five minutes on account of one empty
    /// patch of ground. Every bot told that seam is untold here, and asks the list again.
    /// </para>
    /// </summary>
    private static void Untell(Point3D where)
    {
        foreach (var (serial, told) in _told)
        {
            if (told.Seam.Exists && told.Seam.Where == where)
            {
                _untold.Add(serial);
            }
        }

        for (var i = 0; i < _untold.Count; i++)
        {
            _told.Remove(_untold[i]);
            Stale++;
        }

        _untold.Clear();
    }

    /// <summary>
    /// The same, ignoring one seam. For an undertaking whose way to a seam turned out not to exist: without
    /// the exception it would be handed the same seam and walk into the same wall.
    /// </summary>
    public static BotSeam Seam(IBotWilful bot, Point3D except)
    {
        var body = bot?.Self;
        var map = body?.Map;

        if (map == null || map == Map.Internal)
        {
            return default;
        }

        // Answered out of the last scan while it is still warm. See AskEveryMs, and Untell for when it is not.
        if (Told(bot, body, except, out var lately))
        {
            return lately;
        }

        var best = Choose(bot, body, map, except);

        // <b>And not a seam whose rocks the engine has emptied, which is the one question the trip asks on arrival and the
        // choice did not.</b> BotDig rests a seam as worked out when every rock within BotOre.Reach of its middle has an
        // empty bank — and the rest runs out on a clock, so the seam came back onto the board whether the ore had or not.
        // On build 61, 17:21–18:31, seventeen trips failed that way and the same seams came round every half hour:
        // (1400, 1468) at 17:27, 17:58 and 18:24, (1356, 1448) at 17:30 and 18:07, four others twice. Asked of the winner
        // only, with the trip's own rings and leash; a seam found empty rests exactly as the trip would have rested it, and
        // the choice is made again without it, StockPasses times at most.
        for (var pass = 0; pass < StockPasses && best.Exists && !BotOre.Stocked(map, best.Where); pass++)
        {
            Drained(best.Where);
            Hollow++;

            best = Choose(bot, body, map, except);
        }

        _told[body.Serial] = (Core.TickCount, except, best);

        return best;
    }

    /// <summary>How many times one ask may find its winner worked out and choose again. See <see cref="Seam(IBotWilful, Point3D)"/>.</summary>
    public static int StockPasses { get; set; } = 2;

    /// <summary>Seams found worked out as they were about to be chosen, and rested before anybody walked to them.</summary>
    public static long Hollow { get; private set; }

    /// <summary>The best seam on the board for this bot by every test but the engine's count of ore. See <see cref="Seam(IBotWilful, Point3D)"/>.</summary>
    private static BotSeam Choose(IBotWilful bot, Mobile body, Map map, Point3D except)
    {
        var ledger = bot.Resolve?.Ledger;
        var best = default(BotSeam);
        var bestScore = 0.0;
        var bestPremium = 0.0;

        // The choice the old reckoning would have made, kept beside the new one only to count the veins passed for a metal nobody
        // buys (BotOre.Passed).
        var oldScore = 0.0;
        var oldPremium = 0.0;
        var oldWhere = Point3D.Zero;

        for (var i = 0; i < _seams.Count; i++)
        {
            var seam = _seams[i];

            if (seam.Map != map || (except != Point3D.Zero && seam.Where == except))
            {
                continue;
            }

            // Outside the ground this population is allowed to work. See BotPopulation.Roam: a seam across
            // the continent is a bot walking across the continent, and this shard is not ready for that.
            if (!BotPopulation.Within(map, seam.Where, body))
            {
                continue;
            }

            // A seam on a cliff top is not a seam. See BotPopulation.Reachable.
            if (!BotPopulation.Reachable(map, body.Location, seam.Where))
            {
                continue;
            }

            // <b>Nor one a walk has already proved nobody can stand at (29.09.2026).</b> The walker notes such a tile for
            // everybody (BotFooting.Note: "kept for everybody, not only for this walk"), and the rock chooser (BotOre) read the
            // note — this chooser did not: between 16:37 and 19:00, 33 of the mine's 47 failures were "no way through", the
            // same seams handed out again and again — (1176, 1316, 36) to Cerys at 16:48, to Amberle at 16:54, to Cerys again at
            // 17:02, each time "dropped because there is nowhere there to stand".
            if (BotFooting.Footless(map, seam.Where.X, seam.Where.Y))
            {
                SeamsUnfooted++;

                continue;
            }

            // See MineOdds.
            if (MineOdds > 0.0 && BotQuad.Muscle(map, seam.Where) > BotThreat.Power(body) * MineOdds)
            {
                Outmuscled++;

                continue;
            }

            // And the road to the seam, by the same odds (01.10.2026, build 388): 21 of 386's deaths were miners on the way. See BotDanger.Road.
            if (BotDanger.Road(body, map, body.Location, seam.Where, BotThreat.Power(body), out _, out _, out _))
            {
                RoadOutmuscled++;

                continue;
            }


            if (ledger != null && ledger.Cautious(BotDig.Trade, map, seam.Where))
            {
                continue;
            }

            // <b>Rock that has just been worked out is not rock, and until now it was handed straight back
            // to the next miner.</b> BotGround.Barren strikes a seam off only when nobody ever found
            // anything there; a seam that gave up its ore and ran dry stayed on the board exactly as it was.
            // Measured 08.09.2026: 225 mining errands in forty minutes ending "emptied 8 rocks and found no
            // more", between them 87 gold and no goods at all, and the shard's finishing rate falling from
            // 89% to 67% as the miners worked through a list of holes.
            //
            // Rested rather than removed, because ore does come back and the survey does not run again on
            // its own. See Drained.
            if (Draining(seam.Where))
            {
                continue;
            }

            // Somebody is already on it.
            if (!Free(body, seam.Where))
            {
                continue;
            }

            // <b>And not ground the population has lately failed to reach, which every other chooser of a place already
            // asks.</b> BotRefused is read by the frontier, the herbs, the hunt and the workshops (Nearest), and this, the
            // one chooser whose places are cut into hillsides, did not: on build 50, between 06:04 and 06:25 on 15.09.2026,
            // 47 of the 53 miners turned away from a seam were turned away on the ridge west of Britain, and three of those
            // seams turned four miners away each. One dictionary lookup, and anybody arriving clears it.
            if (BotRefused.Refusing(map, seam.Where))
            {
                Unwalked++;

                continue;
            }

            // <b>And there is a way to it.</b> A survey records rock it can see, and seeing is not walking:
            // a vein inside the castle wall, on a ledge, or across a river is close, scores beautifully on
            // the distance below, and is picked by every miner in turn — each of which walks up to it, fails,
            // marks its own five minutes of caution and is replaced by the next one. From the outside it is
            // gatherers crowding around a castle, which is what Patrick was looking at on 27.08.2026.
            //
            // The reach ledger answers from pockets already proved closed by searches that failed, so this
            // stays a dictionary lookup — the same question a patrol, a harrowing and a lesson all ask before
            // they offer anywhere, and the price a real search per candidate would cost was written down once
            // in BotHunter and is not paid again here.
            if (BotReach.Ask(map, body.Location, seam.Where, BotArrival.Within(BotOre.Reach)) == BotReachVerdict.Sealed)
            {
                Walled++;

                continue;
            }

            // Worth is zero for iron and for anything past this bot's skill, so a green miner rates every
            // seam alike and simply takes the nearest — which is correct: it would be handed iron anyway.
            //
            // <b>And what this ground has actually paid, which is a different fact from what it demands.</b>
            // The requirement is a property of the tile; BotCommons.Dug is the population's memory of what came
            // out of that hillside, and the better of the two metals is weighed.
            //
            // <b>By what the metal is paid for, not by how hard it is to dig (06.10.2026, money15).</b> The worth was the floor
            // over ten plus the rank dug (a gold seam 8.5 + 6 against iron's nought), so a skilled miner walked seven to ten times
            // as far for a metal no shopkeeper buys, and 46 % of the bars of 04.10 had no buyer. See BotOre.Premium.
            var seen = BotCommons.Richest(map, seam.Where);
            var worth = BotOre.Premium(body, map, seam.Where, seen);
            var was = (BotOre.CanWork(body, seam.Required) ? seam.Required / 10.0 : 0.0) + seen;

            // <b>By the road, the fourth of the family.</b> The prowl (210), the lairs (218) and the fires and counters (221)
            // learned that a place over the river west of Britain is hundreds of tiles further round than it is straight;
            // the seams had not, and a coloured vein is worth seven to ten times an iron one here, so the miners went far
            // for it: 56 mining trips failed in the window 07:56–08:41 on 25.09.2026, 29 of them walks that stopped closing
            // 49 to 193 tiles short (median 105). See BotRoads.Behind.
            var away = body.GetDistanceToSqrt(seam.Where) + System.Math.Max(0, BotRoads.Behind(map, body.Location, seam.Where));
            var score = (1.0 + worth) / (1.0 + away / Patience);
            var old = (1.0 + was) / (1.0 + away / Patience);

            if (old > oldScore)
            {
                oldScore = old;
                oldPremium = worth;
                oldWhere = seam.Where;
            }

            if (score <= bestScore)
            {
                continue;
            }

            best = seam;
            bestScore = score;
            bestPremium = worth;
        }

        if (best.Exists)
        {
            BotOre.Chose(bestPremium, oldWhere != Point3D.Zero && oldWhere != best.Where && oldPremium < 0.0);
        }

        return best;
    }

    /// <summary>The nearest place metal can be melted, or <see cref="Point3D.Zero"/>.</summary>
    public static Point3D Fire(Map map, Point3D from) => Nearest(_fires, map, from);

    /// <summary>
    /// The same, ignoring one that turned out to be unreachable.
    ///
    /// <para>
    /// <b>Without this a miner with a pack of ore and no way to the nearest forge has nowhere to go, for
    /// ever.</b> It picks the nearest, the road is refused, the undertaking fails, the proposer offers mining
    /// again because the bot is still carrying ore, and it picks the same forge — three failures a second,
    /// indefinitely. There are four forges on this shard and the other three may be perfectly reachable; the
    /// bot only has to be allowed to ask for a different one.
    /// </para>
    /// </summary>
    public static Point3D Fire(Map map, Point3D from, Point3D except) => Nearest(_fires, map, from, except);

    /// <summary>The nearest place money and goods can be put away, or <see cref="Point3D.Zero"/>.</summary>
    public static Point3D Counter(Map map, Point3D from) => Nearest(_counters, map, from);

    /// <summary>The same, ignoring one that turned out to be unreachable.</summary>
    public static Point3D Counter(Map map, Point3D from, Point3D except) => Nearest(_counters, map, from, except);

    /// <summary>
    /// The ledger's key for "I could not get to a forge here", and the same for a counter.
    ///
    /// <para>
    /// <b>Per bot, and that correction matters more than it looks.</b> The first version of this was one list
    /// for the whole population, on the reasoning that a forge across water is across water for everybody —
    /// which is only true of bots standing on the same bank. One gatherer that wandered west and got itself
    /// behind a river declared every forge on the shard unreachable, mining stopped being offered to anybody,
    /// and the log said so in as many words: <c>no a fire has been found near any bot yet</c>. Reachability is
    /// a fact about a bot and a place together, never about the place alone.
    /// </para>
    ///
    /// <para>
    /// It rides on <see cref="BotLedger.Beware"/> rather than a list of its own: that is already per bot,
    /// already expires, and already evicts what has not been thought about lately.
    /// </para>
    /// </summary>
    public const string FireKind = "fire";

    public const string CounterKind = "counter";

    /// <summary>The ledger's key for "I could not get to a fire here". See <see cref="FireKind"/>.</summary>
    public const string HearthKind = "hearth";

    /// <summary>
    /// How long one miner holds a seam before anybody else may work it.
    ///
    /// <para>
    /// <b>Two bots on one seam is two bots doing one bot's work.</b> A seam is a patch of rock a few tiles
    /// across; the second miner to arrive spends its swings on the same block, drains it twice as fast, and
    /// the pair of them stand on each other's tiles doing it. Watched live, they simply looked like they were
    /// milling about.
    /// </para>
    ///
    /// <para>
    /// Renewed on every beat the holder is actually digging, so it lapses on its own the moment that bot
    /// walks off, dies, or takes on something else. There are ninety-odd seams on record and fifteen bots:
    /// nobody is short of rock because somebody else got there first.
    /// </para>
    /// </summary>
    public static int DigClaimMs { get; set; } = 90000;

    private static readonly Dictionary<Point3D, (Serial Miner, long Tick)> _digging = [];

    /// <summary>Takes, or renews, this bot's hold on a seam.</summary>
    public static void Working(Mobile miner, Point3D seam)
    {
        if (miner != null && seam != Point3D.Zero && Free(miner, seam))
        {
            _digging[seam] = (miner.Serial, Core.TickCount);
        }
    }

    /// <summary>Whether this bot may work that seam: nobody holds it, or this bot does.</summary>
    public static bool Free(Mobile miner, Point3D seam)
    {
        if (miner == null || !_digging.TryGetValue(seam, out var held))
        {
            return true;
        }

        if (Core.TickCount - held.Tick >= DigClaimMs)
        {
            _digging.Remove(seam);

            return true;
        }

        return held.Miner == miner.Serial;
    }

    /// <summary>Done with it, however that came about.</summary>
    public static void Leave(Point3D seam) => _digging.Remove(seam);

    /// <summary>
    /// A miner walked to this seam, stood on it, and found nothing to swing at. The row goes.
    ///
    /// <para>
    /// <b>The survey samples every fourth tile, so a seam is a sighting and not a promise.</b> When the
    /// sighting turns out to be wrong the only thing that used to happen was a note in that one bot's own
    /// ledger — so the next miner walked the same distance to learn the same thing, and the one after that,
    /// twelve of them in ten minutes on 27.08.2026. What one bot proves by standing on the ground is true
    /// for all of them.
    /// </para>
    ///
    /// <para>
    /// Removed rather than marked, and rock is allowed to come back: the sweeps run wherever bots go, so
    /// ground that grows ore again is recorded again from nothing. That is the same ruling
    /// <c>BotPeril.Cleared</c> makes about a harrowed square — the row goes, and reality writes a new one.
    /// </para>
    /// </summary>
    /// <summary>
    /// How long a seam that has been worked out is left alone before anybody is sent to it again.
    ///
    /// <para>
    /// <b>The engine's own refill time plus a minute, and not a number anybody chose.</b> This read ten
    /// minutes, which is exactly <em>the earliest</em> moment the engine could have put the ore back — see
    /// <see cref="BotOre.RespawnMs"/>, where the whole measurement is written down. A rest shorter than the
    /// refill hands a barren seam back to the next miner, and the next miner cannot tell the difference from
    /// outside: it walks out, works through its allowance of rocks, finds nothing and rests the seam again.
    /// </para>
    ///
    /// <para>
    /// Settable, because it is still a dial and Patrick may want to see what a shorter one does. The default
    /// is the only part taken from the engine.
    /// </para>
    /// </summary>
    public static int DrainedMs { get; set; } = 600000;

    /// <summary>
    /// Sets the rest from the engine's own refill time, once, at start-up.
    ///
    /// Not a static initialiser: the mining system is built by content initialisation, and a value read
    /// before that would be the fallback for the life of the shard — the same trap
    /// <c>BotShopper.Makeable</c> guards against by refusing to cache before the systems exist.
    /// </summary>
    public static int RestFromEngine()
    {
        DrainedMs = BotOre.RespawnMs + 60000;

        return DrainedMs;
    }

    /// <summary>Seams resting because somebody worked them out. See <see cref="Drained"/>.</summary>
    public static long Dry { get; private set; }

    private static readonly Dictionary<Point3D, long> _drained = [];

    /// <summary>Somebody took the last of the ore here. The seam rests before it is offered again.</summary>
    public static void Drained(Point3D where)
    {
        _drained[where] = Core.TickCount;
        Dry++;
        Untell(where);
    }

    /// <summary>Whether this seam is still resting after being worked out.</summary>
    public static bool Draining(Point3D where) =>
        _drained.TryGetValue(where, out var when) && Core.TickCount - when < DrainedMs;

    /// <summary>
    /// How long a hearth a cook stood beside and found cold is passed over by every cook. Twenty minutes.
    ///
    /// <para>
    /// A rest and not a removal, and a soft rule asked on the first pass only (see <see cref="Nearest"/>): a sweep does
    /// not come back to ground it has already seen, so a fire struck off by mistake would be gone until a restart, and a
    /// rest that covered every fire in reach must not stop a cook from trying the nearest one anyway.
    /// </para>
    /// </summary>
    public static int ColdMs { get; set; } = 1200000;

    /// <summary>Hearths rested because a cook stood beside one and the engine found no fire. See <see cref="Cold"/>.</summary>
    public static long Cooled { get; private set; }

    private static readonly Dictionary<Point3D, long> _cold = [];

    /// <summary>How near a hearth found cold another remembered one must be to be looked at with it. See <see cref="Cold"/>.</summary>
    public static int OutReach { get; set; } = 4;

    /// <summary>Hearths struck off because nothing on their tile burns any more. See <see cref="Cold"/>.</summary>
    public static long Out { get; private set; }

    /// <summary>
    /// A cook stood beside this hearth and the engine would not cook over it.
    ///
    /// <para>
    /// <b>A fire that has gone is struck off; a fire still there is rested.</b> The rest alone was for a fire the engine
    /// would not use from where the cook stood, and it came back every twenty minutes for a fire that no longer existed:
    /// on 24.09.2026 33 cooks failed "no fire the engine will cook over", 11 of them round (1938, 1316) and 11 round
    /// (1531, 1007), where Argus found "nothing lying on it" — camp fires, filed by the sweep as items and gone with their
    /// camps, one entry to each tile of a fire. So every remembered hearth within <see cref="OutReach"/> of this one with
    /// nothing on its tile the engine calls a heat source (an item or a static) is struck off; the tile is asked of the
    /// engine itself, so nothing still burning is lost. What is left, this one included when it still burns, is rested for
    /// <see cref="ColdMs"/> as before.
    /// </para>
    /// </summary>
    public static void Cold(Map map, Point3D where)
    {
        if (map != null)
        {
            for (var i = _hearths.Count - 1; i >= 0; i--)
            {
                var (on, at) = _hearths[i];

                if (on != map || Math.Abs(at.X - where.X) > OutReach || Math.Abs(at.Y - where.Y) > OutReach || Burning(map, at))
                {
                    continue;
                }

                _hearths.RemoveAt(i);
                _cold.Remove(at);
                Out++;
            }

            if (!Burning(map, where))
            {
                return;
            }
        }

        _cold[where] = Core.TickCount;
        Cooled++;
    }

    /// <summary>Whether anything on this tile, item or static, is a fire the engine cooks over. See <see cref="Cold"/>.</summary>
    private static bool Burning(Map map, Point3D at)
    {
        foreach (var item in map.GetItemsAt(at.X, at.Y))
        {
            if (!item.Deleted && IsHearthId(item.ItemID))
            {
                return true;
            }
        }

        foreach (var tile in map.Tiles.GetStaticAndMultiTiles(at.X, at.Y))
        {
            if (IsHearthId(tile.ID))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Whether this hearth is still resting after a cook found it cold.</summary>
    public static bool Cooling(Point3D where) =>
        _cold.TryGetValue(where, out var when) && Core.TickCount - when < ColdMs;

    /// <summary>
    /// How long a forge is passed over after a smith stood beside it and the engine would accept no anvil there.
    ///
    /// <para>
    /// <b>The same fault as the cold hearth, at a forge, and longer because nothing relights an anvil.</b> On build 44 ten
    /// of the eleven "no anvil the engine will accept" failures were one forge, (1424, 1558): Ulric three times, Wulfric
    /// twice, Emeric twice, Alaric, Roderic and Hale, across forty minutes. <c>BotForge</c> wrote the refusal into the one
    /// smith's own ledger and the sweep kept offering the forge to everybody else. An hour, soft like the hearth's rest:
    /// when every forge in reach is resting the nearest is still offered.
    /// </para>
    /// </summary>
    public static int UnfitMs { get; set; } = 3600000;

    /// <summary>Forges rested because a smith stood at one and the engine accepted no anvil. See <see cref="UnfitMs"/>.</summary>
    public static long Unfitted { get; private set; }

    private static readonly Dictionary<Point3D, long> _unfit = [];

    /// <summary>A smith stood beside this forge and the engine accepted no anvil within reach. Every smith passes it over for a while.</summary>
    public static void Unfit(Point3D where)
    {
        _unfit[where] = Core.TickCount;
        Unfitted++;
    }

    /// <summary>Whether this forge is still resting after a smith found no anvil the engine would take there.</summary>
    public static bool Unfitting(Point3D where) =>
        _unfit.TryGetValue(where, out var when) && Core.TickCount - when < UnfitMs;

    /// <summary>
    /// How many walks may give up a long way short of a seam before the seam is struck off for it.
    ///
    /// <para>
    /// <b>A walk that stops closing a hundred tiles from a seam has proved nothing about the seam.</b> In the first hour
    /// of 22.09.2026 (build 176) 55 seams were struck off by a long walk that gave up — 13 of them within fifty tiles of
    /// the seam, 19 between fifty and a hundred, and 23 at a hundred or more: Alden "no way through to the CopperOre in
    /// 179 tiles", twice, and 171, from wherever in the south he was standing. <see cref="Barren"/> takes the seam off
    /// the board for every miner, on the reasoning that what one bot proves by standing on the ground is true for all of
    /// them — and this bot was not standing on it. The same rate held all through the three hours before, so it was
    /// not a restart's price: it was the board being eaten from a distance, some 25 seams an hour out of 1,362.
    /// </para>
    ///
    /// <para>
    /// Three, as <c>BotDig.WalledLimit</c> is and for its reason. Between them the seam rests in <c>BotRefused</c>,
    /// which doubles and which an arrival clears, so three means three walks minutes apart and none of them arriving.
    /// </para>
    /// </summary>
    public static int ShiedLimit { get; set; } = 3;

    /// <summary>Long walks that gave up far from their seam and left it on the board. See <see cref="ShiedLimit"/>.</summary>
    public static long Shied { get; private set; }

    private static readonly Dictionary<Point3D, int> _shied = [];

    /// <summary>
    /// A walk gave up far short of this seam. True when that has now happened <see cref="ShiedLimit"/> times and the seam
    /// is struck off after all; otherwise the seam stays and its ground rests in the refusal memory.
    /// </summary>
    public static bool Shy(Map map, Point3D where)
    {
        _shied.TryGetValue(where, out var times);
        times++;

        if (times >= ShiedLimit)
        {
            _shied.Remove(where);

            return Barren(where);
        }

        _shied[where] = times;
        Shied++;
        BotRefused.Refuse(map, where);

        return false;
    }

    public static bool Barren(Point3D where)
    {
        _shied.Remove(where);

        for (var i = 0; i < _seams.Count; i++)
        {
            if (_seams[i].Where != where)
            {
                continue;
            }

            _seams.RemoveAt(i);
            _digging.Remove(where);
            Emptied++;
            Untell(where);

            return true;
        }

        return false;
    }

    /// <summary>Seams struck off because somebody stood on them and found nothing. See <see cref="Barren"/>.</summary>
    public static long Emptied { get; private set; }

    /// <summary>Seams put on the board by a bot that walked out past the frontier. See BotProspect.</summary>
    public static long Prospected { get; private set; }

    /// <summary>Prospecting walks that found nothing in the ground at all.</summary>
    public static long Fruitless { get; private set; }


    /// <summary>Noted by BotProspect, which is the only thing that may.</summary>
    internal static void Found(int seams) => Prospected += seams;

    /// <summary>The same, for a walk that found nothing.</summary>
    internal static void FoundNothing() => Fruitless++;

    /// <summary>Seams passed over for being inside a town's walls.</summary>
    public static long Townbound { get; private set; }

    /// <summary>The nearest forge this bot has not lately failed to reach.</summary>
    public static Point3D Fire(IBotWilful bot, Point3D from, Point3D except = default) =>
        Nearest(_fires, bot?.Self?.Map, from, except, bot, FireKind);

    /// <summary>The nearest counter this bot has not lately failed to reach.</summary>
    public static Point3D Counter(IBotWilful bot, Point3D from, Point3D except = default) =>
        Nearest(_counters, bot?.Self?.Map, from, except, bot, CounterKind);

    /// <summary>The nearest fire of any kind this bot has not lately failed to reach.</summary>
    public static Point3D Hearth(IBotWilful bot, Point3D from, Point3D except = default) =>
        Nearest(_hearths, bot?.Self?.Map, from, except, bot, HearthKind);

    /// <summary>The nearest fire of any kind, without asking whose it is.</summary>
    public static Point3D Hearth(Map map, Point3D from) => Nearest(_hearths, map, from);

    private static Point3D Nearest(
        List<(Map Map, Point3D Where)> places, Map map, Point3D from, Point3D except = default,
        IBotWilful bot = null, string kind = null
    )
    {
        var ledger = kind == null ? null : bot?.Resolve?.Ledger;

        // <b>Two passes, and the second one is what stops a rule from starving the island.</b> There are two
        // counters on this shard and four fires. A record that can rest a square - and BotRefused rests one
        // for two minutes on the first refusal, doubling to two hours - can therefore rest every bank the
        // population has, and then nobody walks to one, and then nobody arrives to disprove it, and the
        // record has locked the door it is standing behind. That shape has cost this project a night before:
        // an instrument paid for out of the waste it was meant to end, which switches itself off exactly when
        // it is needed.
        //
        // So the soft rule - a square resting after refusing somebody - is asked on the first pass only. If
        // nothing survives it, the second pass drops it and takes the nearest thing there is. Counted, so
        // that a day when the population is living on the second pass is a day that says so.
        //
        // <b>The storey rule is not soft and is asked on both.</b> See where it is applied: a rest may have
        // expired, a floor has not moved.
        for (var pass = 0; pass < 2; pass++)
        {
            var found = Pick(places, map, from, except, ledger, kind, pass == 0, bot?.Self);

            if (found != Point3D.Zero)
            {
                if (pass > 0)
                {
                    Anyway++;
                }

                return found;
            }
        }

        return Point3D.Zero;
    }

    /// <summary>Times the choice had to be made with the soft rules switched off. See <see cref="Nearest"/>.</summary>
    public static long Anyway { get; private set; }

    /// <summary>One pass of <see cref="Nearest"/>, with the soft rules on or off.</summary>
    private static Point3D Pick(
        List<(Map Map, Point3D Where)> places, Map map, Point3D from, Point3D except, BotLedger ledger, string kind,
        bool choosy, Mobile who = null
    )
    {
        var best = Point3D.Zero;
        var bestAway = double.MaxValue;

        for (var i = 0; i < places.Count; i++)
        {
            var (on, where) = places[i];

            if (on != map || !BotPopulation.Within(on, where, who))
            {
                continue;
            }

            if (who != null && !BotPopulation.Reachable(on, who.Location, where))
            {
                continue;
            }

            if (except != Point3D.Zero && where == except)
            {
                continue;
            }

            // <b>Somebody else's, and this one is not soft.</b> A forge in a guild hall belongs to that
            // guild — Patrick's order of 08.09.2026 — and unlike the rules below it is asked on both passes,
            // because a rest may expire and ownership does not. It is safe to make hard here for one reason:
            // it can only ever refuse a place standing inside a house, and every fire, forge and counter in
            // Britain stands in the street. See BotEstate.MayUse.
            if (who != null && !BotEstate.MayUse(who, on, where))
            {
                if (choosy)
                {
                    BotEstate.Bar();
                }

                continue;
            }

            // <b>Barred ground, and hard on both passes for exactly the reason above.</b> The refusal ledger
            // below is a measurement that expires, so it is right to switch it off when there is nothing
            // else to choose; this is a statement about the map that does not. A cooking fire stood inside a
            // walled pocket east of Britain took seventy walk failures in one session — every one of them a
            // cook that chose it because it was the only fire it could see, on the pass where the soft rules
            // are off. "There is no other fire" is not a reason to walk at a place with no way in; the right
            // answer is that there is no fire. See BotBarred.
            if (BotBarred.Barred(on, where))
            {
                continue;
            }

            // Somewhere this bot itself could not get to lately. Everybody else may still use it.
            //
            // <b>And this has never once fired for a counter or a forge, because the two ends speak different
            // words.</b> The caution is filed by <c>BotWill.Settle</c> under the <em>undertaking's</em> name —
            // "unload", "mine", "forge" — and asked for here under the <em>place's</em> name, "counter" or
            // "fire". Both keys are correct and they never meet, so the summary above this method promising
            // "the nearest counter this bot has not lately failed to reach" was a promise nothing kept: Edda
            // walked at the same unreachable counter seventy-six times in eleven minutes on 25.08.2026, and it
            // was the largest single source of failure on the shard.
            //
            // Kept, because a bot that lost money at a place should still avoid it under its own trade's name.
            if (ledger?.Cautious(kind, on, where) == true)
            {
                continue;
            }

            // What the shard has already proved about the ground itself, in a word nobody has to agree on.
            // A pocket walked to its edges answers in one comparison — the same free question the hunt asks of
            // every place it thinks about prowling to — and unlike the ledger it is not a private opinion:
            // no way through is no way through for anybody.
            if (BotReach.Ask(on, from, where, BotArrival.Within(1)) == BotReachVerdict.Sealed)
            {
                continue;
            }

            // A floor nobody standing here could be on. See Storey: two of the four forges this shard knows
            // are thirty units above the street, and until this line every smith on the pavement chose one of
            // them whenever it was the nearest.
            // <b>Asked on both passes, unlike the resting rule beside it, and the difference is what the two
            // facts are made of.</b> A square that refused somebody may have refused them for a reason that
            // has since gone — a creature in a doorway, a bot in the way — so relaxing it when nothing else
            // is left costs one wasted walk at worst. A workshop three storeys up is geometry: it will be
            // three storeys up on the next beat and on every beat after, and handing it to a smith who has
            // nowhere else to go is not a fallback, it is a guaranteed failure dressed as one.
            //
            // Measured 08.09.2026: 55 choices had to be made with both rules off, and Emeric and Wulfric
            // spent them walking at (1361, 1574, 30) — one of the two forges nobody can reach — while their
            // purses went from 340gp to 40 and 13. The right answer when every forge is on another floor is
            // that there is no forge, which BotSmith already counts and reports as NoForge.
            if (Math.Abs(where.Z - from.Z) > Storey)
            {
                Upstairs++;

                continue;
            }

            // And what the population as a whole has already proved about this ground. BotRefused is written
            // by whoever gives up on a road and cleared by whoever arrives, and until now it was read by the
            // frontier, the herbs and the hunt and by nothing that picks a workshop — which is the shape of
            // defect this project has paid for more often than any other: a note filed and not read.
            if (choosy && BotRefused.Refusing(on, where))
            {
                Refused++;

                continue;
            }

            // A hearth a cook stood beside and found cold, and soft for the same reason: it may be lit again, and when
            // every fire in reach is resting the nearest one is still worth a look. See Cold.
            if (choosy && (ReferenceEquals(places, _hearths) && Cooling(where) || ReferenceEquals(places, _fires) && Unfitting(where)))
            {
                continue;
            }

            var away = Math.Sqrt(
                (double)(where.X - from.X) * (where.X - from.X) + (double)(where.Y - from.Y) * (where.Y - from.Y)
            );

            // <b>Nearest by the road, the third time this family has had to learn it.</b> A fire, a forge or a counter over
            // the river west of Britain is hundreds of tiles further round than it is straight; the prowl learned it in
            // build 210 and the errand's lairs in 218, and the cook had not: Sable Ashdown set out for the fire at
            // (1043, 1400) four times on the evening of 24.09.2026 and each time stopped 124 tiles short after 600 beats.
            // How much further round from home the place lies than the bot (BotRoads.Behind) is added to the distance.
            if (away >= bestAway)
            {
                continue;
            }

            away += Math.Max(0, BotRoads.Behind(on, from, where));

            if (away >= bestAway)
            {
                continue;
            }

            best = where;
            bestAway = away;
        }

        return best;
    }

    /// <summary>
    /// Everything back to nothing. Called when the world is reloaded: every point in these lists is a
    /// place in a world that has just been replaced, and a forge remembered from the last one is a bot
    /// walking to an empty field.
    /// </summary>
    public static void Reset()
    {
        _seams.Clear();
        _fires.Clear();
        _hearths.Clear();
        _counters.Clear();
        _surveyed.Clear();
        _pending.Clear();
        _sweeping = false;
        Slices = 0;
        SweptMs = 0.0;
        SliceWorstMs = 0.0;
        _digging.Clear();
        _told.Clear();
        _drained.Clear();
        _cold.Clear();
        _unfit.Clear();
        _shied.Clear();
        Shied = 0;
        Dry = 0;
        Cooled = 0;
        Out = 0;
        Underground = 0;
        Unfitted = 0;
        Spared = 0;
        Stale = 0;
        Upstairs = 0;
        Unfooted = 0;
        Refused = 0;
        Anyway = 0;
        Unwalked = 0;

        _saidCapped = false;
    }

    public static string Describe() =>
        $"{_surveyed.Count} sweeps ({_pending.Count} waiting{(_sweeping ? ", one under way" : "")}) in {Slices} slices costing {SweptMs:F0}ms of the loop, the worst slice {SliceWorstMs:F1}ms against {SliceMs:F0} allowed, {BotMiner.Awaiting} asks for rock answered with nothing for ground still being swept: {_seams.Count} seams, {_fires.Count} fires, {_hearths.Count} hearths, {_counters.Count} counters; {Walled} seams passed over with no way through, {Unwalked} for standing on ground that had lately refused somebody, {Townbound} for being inside the walls, {Outmuscled} for lying where something past the miner's strength (×{MineOdds:F1}) has killed lately and {RoadOutmuscled} for a road there that asks more, {SeamsUnfooted} for standing where a walk had found no footing, {Emptied} struck off as barren, {Shied} left on the board because the walk gave up far short of them, and {Dry} rested after being worked out ({Hollow} of them found so as they were about to be chosen, before anybody walked there),{Cooled} hearths rested after a cook stood beside one and found it cold and {Out} struck off for nothing burning on their tile any more, {Underground} asks for a sweep from inside a dungeon refused, {Unfitted} forges rested after a smith stood at one and the engine accepted no anvil, {BotMiner.Sent} sent out past the frontier and {Prospected} seams found there over {Fruitless} empty walks, {BotMiner.Burdened} seams not offered to a pack with no room for ore, {BotDig.Unwalkable} struck off for nobody getting nearer to them, {BotDig.FarSide} rocks found only by looking again from the middle of a seam, {BotDig.Standless} rocks with no floor within swinging reach, {BotDig.Repicked} trips sent on to another seam because another miner already held theirs and {BotDig.Beaten} given up for finding no other free, {BotDig.WorkedOut} rested rather than struck off because every rock in reach was there and worked out,{BotDig.Drained} rocks given up with the engine's bank under them empty against {BotDig.Fumbled} still holding ore the miner kept missing and {BotDig.Allowanced} trips that stopped on their own allowance of eight (the miners that gave up on full rock were being given {(BotDig.Fumbled > 0 ? BotDig.FumbledChance / BotDig.Fumbled : 0.0):P0} a swing by the engine, {BotDig.Locked} swings were taken with the pickaxe still locked by the one before, and {BotDig.Stirred} quiet swings were taken by a bot that had moved since the last one, {BotDig.Adrift} swings the engine cancelled for that and {BotDig.Laden} whose ore was lost to a full pack, {BotDig.NextPickaxe} trips carried on with the next pickaxe, {BotDig.Spurned} melts the forge took none of the ore for, {BotDig.Nearer} of them tried again from beside it and {BotDig.Forsaken} forges rested for it), {Spared} asks answered out of the last scan and {Stale} answers taken back out of it for a seam gone or feared since, {Unfooted} workshops never filed for having no floor at them, {Upstairs} passed over for standing on another floor and {Refused} for ground that has refused the population ({Anyway} choices then had to be made with the resting rule off, or there would have been nowhere at all), patience {Patience} tiles; the lode is at ({Lode.X}, {Lode.Y}); {BotHeard.Describe()}";
}
