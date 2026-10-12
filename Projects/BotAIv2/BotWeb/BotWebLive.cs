using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Server.Guilds;
using Server.Items;
using Server.Logging;
using Server.Mobiles;
using Server.Spells;

namespace Server.BotAI.V2;

/// <summary>
/// The live window (Patrick, 02.10.2026: "a window that shows in real time what is happening in the game and with a bot,
/// say once a second"): a frame for each bot somebody is looking at, and a page for the debugger squad, both built on the
/// game loop once a second and handed to the listener as strings.
///
/// <para>
/// <b>Built only for what somebody asked about in the last ten seconds, and for at most four bots.</b> Asking for a frame
/// is what marks a bot as watched; a page closed or switched to another tab stops asking, and ten seconds later nothing is
/// built for it. With nobody watching the clock costs one tick comparison and two reads, nothing is allocated, and the
/// event hook is taken off <see cref="BotEvents"/>.
/// </para>
///
/// <para>
/// <b>The same two rules as the rest of the folder.</b> The listener reads nothing from the world: a frame is a string the
/// loop wrote. Every figure in it is one the snapshot, the log or the event stream already prints — the bot's own numbers,
/// the engine's own notoriety, <see cref="BotThreat.Menacing"/> and <see cref="BotRegard.Hostile"/> for who is hostile, the
/// radar colours the map picture is drawn from. The session-log lines are read off the file by a task on the thread pool,
/// which never touches the world, and handed to the loop under the lock.
/// </para>
///
/// <para>
/// <b>Around the bot only.</b> The mobiles and items come from the map's own range queries round the bot
/// (<c>GetMobilesInRange</c>, <c>GetItemsInRange</c>); the terrain from the tile matrix in eight-by-eight blocks that are kept
/// once read, so a bot walking along a road costs the new blocks at its edge and nothing else, and at most
/// <see cref="TerrainBudget"/> tiles are read in one second whatever happens.
/// </para>
/// </summary>
public static partial class BotWebLive
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotWebLive));

    /// <summary>Whether the live window is built at all. <c>Live</c> in bot-web.json.</summary>
    public static bool Enabled { get; set; } = true;

    /// <summary>
    /// Whether the frame also carries the scene the page draws in the game's own pictures, and the pictures are served.
    /// <c>LivePicture</c> in bot-web.json.
    /// </summary>
    public static bool Picture { get; set; } = true;

    /// <summary>How often a frame is built for each watched bot.</summary>
    public static int EveryMs { get; set; } = 1000;

    /// <summary>How long a bot stays watched after the last time anybody asked for its frame.</summary>
    public static int KeepMs { get; set; } = 10000;

    /// <summary>Most bots watched at once; a fifth asked for takes the place of the one asked about longest ago.</summary>
    public static int MostWatched { get; set; } = 4;

    /// <summary>The square round the bot, in tiles each way: 20 gives the 41 by 41 the page draws.</summary>
    public static int Radius { get; set; } = 20;

    /// <summary>How far the bot may walk from where the terrain was centred before it is centred again.</summary>
    public static int Slack { get; set; } = 4;

    /// <summary>Most tiles of terrain read from the tile matrix in one beat, across every watched bot.</summary>
    public static int TerrainBudget { get; set; } = 1024;

    /// <summary>Most terrain blocks kept; past it the cache is emptied and filled again from what is watched.</summary>
    public static int MostBlocks { get; set; } = 8192;

    /// <summary>Most events and most log lines carried in a frame, and most mobiles and items.</summary>
    public static int FeedKeep { get; set; } = 40;

    public static int MostMobiles { get; set; } = 80;

    public static int MostItems { get; set; } = 60;

    /// <summary>Most waypoints of the route ahead in a frame.</summary>
    public static int MostRoute { get; set; } = 48;

    /// <summary>How far back in the session log a newly watched bot's lines are looked for.</summary>
    public static int TailBytes { get; set; } = 256 * 1024;

    /// <summary>Most of the session log read in one pass.</summary>
    public static int MostLogBytes { get; set; } = 2 * 1024 * 1024;

    // ---- the counts /api/health prints ----------------------------------------------------------------------

    /// <summary>Frames built since boot.</summary>
    public static long Frames { get; private set; }

    public static double FramesMs { get; private set; }

    public static double WorstFrameMs { get; private set; }

    /// <summary>What the last beat with anything to build cost in all: frames, terrain and the debuggers' page.</summary>
    public static double LastBeatMs { get; private set; }

    public static double WorstBeatMs { get; private set; }

    public static long Beats { get; private set; }

    public static double BeatsMs { get; private set; }

    public static long TerrainTiles { get; private set; }

    /// <summary>Times the block cache was emptied for being full.</summary>
    public static long Emptied { get; private set; }

    public static double TerrainMs { get; private set; }

    public static long DebuggerPages { get; private set; }

    public static double DebuggerMs { get; private set; }

    public static long LogLines { get; private set; }

    public static int Watched => _count;

    // ---- state ----------------------------------------------------------------------------------------------

    private sealed class Watch
    {
        public string Name;

        /// <summary>The bot's own spelling, once found; the log is searched for this.</summary>
        public string Real;

        public long AskedMs;

        public Serial Serial = Serial.MinusOne;

        public long Seq;

        public string Frame;

        public readonly Queue<BotEvent> Events = new();

        public readonly Queue<(long Id, string Text)> Log = new();

        /// <summary>The session-log offset this bot's lines have been looked for up to; -1 until the reader first sees it.</summary>
        public long LogUpTo = -1;

        public Map TerrainMap;

        public int TerrainX;

        public int TerrainY;

        public bool TerrainComplete;

        public string Terrain;

        // ---- the picture's scene: written on the loop; Scene and SceneVer also read by the listener under the lock
        public Map SceneMap;

        public int SceneX;

        public int SceneY;

        public bool SceneComplete;

        public long SceneMultis;

        public int SceneVer;

        public string Scene;
    }

    private static readonly object _lock = new();

    private static readonly Dictionary<string, Watch> _watches = new(StringComparer.OrdinalIgnoreCase);

    private static readonly List<Watch> _due = [];

    private static readonly Dictionary<long, ushort[]> _blocks = [];

    private static readonly List<BotEvent> _eventScratch = [];

    private static readonly List<(long Id, string Text)> _logScratch = [];

    private static volatile int _count;

    private static volatile int _blockCount;

    private static long _tick;

    private static long _debuggersAskedMs = long.MinValue / 2;

    private static volatile string _debuggers;

    private static int _reading;

    private static string _logFile;

    private static long _logFoundMs = long.MinValue / 2;

    private static long _logId;

    private static int _throws;

    private static readonly JsonWriterOptions Options = new() { Indented = false, SkipValidation = true };

    // ---- the listener's side: any thread, reads only what the loop wrote ------------------------------------

    /// <summary>
    /// The latest frame for this bot, and its number, and marks it watched. Null until the loop has built the first one, which
    /// is within a second. Any thread.
    /// </summary>
    public static (long Seq, string Json) Ask(string name)
    {
        if (!Enabled || string.IsNullOrWhiteSpace(name))
        {
            return (0, null);
        }

        name = name.Trim();

        // The radar colours read on this thread, once, so the loop never waits on the disk for them.
        BotWebMap.Radar();

        lock (_lock)
        {
            if (!_watches.TryGetValue(name, out var watch))
            {
                if (_watches.Count >= Math.Max(1, MostWatched))
                {
                    Watch oldest = null;

                    foreach (var (_, w) in _watches)
                    {
                        if (oldest == null || w.AskedMs < oldest.AskedMs)
                        {
                            oldest = w;
                        }
                    }

                    if (oldest != null)
                    {
                        _watches.Remove(oldest.Name);
                    }
                }

                watch = new Watch { Name = name };
                _watches[name] = watch;
                _count = _watches.Count;
            }

            watch.AskedMs = Environment.TickCount64;

            return (watch.Seq, watch.Frame);
        }
    }

    /// <summary>The debugger squad's page, and marks it wanted for the next ten seconds. Any thread.</summary>
    public static string Debuggers()
    {
        // Switched off, the loop never builds the page, so the placeholder below would promise a page that never comes.
        if (!Enabled)
        {
            return "{\"at\":null,\"debuggers\":null,\"note\":\"the live window is off: bot-web.json says Live is false\"}";
        }

        Interlocked.Exchange(ref _debuggersAskedMs, Environment.TickCount64);

        return _debuggers
               ?? (BotWebHooks.Debuggers == null
                   ? "{\"at\":null,\"debuggers\":null,\"note\":\"the minds assembly has not reported a squad\"}"
                   : "{\"at\":null,\"debuggers\":[],\"note\":\"the first page is built on the loop within a second\"}");
    }

    /// <summary>The names watched now, for the health page.</summary>
    public static string[] Names()
    {
        lock (_lock)
        {
            var names = new string[_watches.Count];
            var i = 0;

            foreach (var (_, w) in _watches)
            {
                names[i++] = w.Real ?? w.Name;
            }

            return names;
        }
    }

    // ---- the loop's side ------------------------------------------------------------------------------------

    /// <summary>Called a few times a second from the web module's timer. Game loop only.</summary>
    public static void Tick(long now)
    {
        if (!Enabled || now - _tick < EveryMs)
        {
            return;
        }

        var wantDebuggers = Environment.TickCount64 - Interlocked.Read(ref _debuggersAskedMs) < KeepMs;

        if (_count == 0 && !wantDebuggers)
        {
            if (BotEvents.Live != null)
            {
                BotEvents.Live = null;
            }

            return;
        }

        _tick = now;

        var started = Stopwatch.GetTimestamp();

        try
        {
            if (_count > 0)
            {
                Build(now);
            }
            else
            {
                BotEvents.Live = null;
            }

            if (wantDebuggers)
            {
                Squad();
            }
        }
        catch (Exception e)
        {
            // Once, then every sixtieth: a builder that throws every second would bury the log.
            if (_throws++ % 60 == 0)
            {
                logger.Error(e, "The live window threw; the page keeps the last frame");
            }
        }

        var ms = Elapsed(started);

        Beats++;
        BeatsMs += ms;
        LastBeatMs = ms;

        if (ms > WorstBeatMs)
        {
            WorstBeatMs = ms;
        }
    }

    private static double Elapsed(long started) => (Stopwatch.GetTimestamp() - started) * 1000.0 / Stopwatch.Frequency;

    private static void Build(long now)
    {
        var nowMs = Environment.TickCount64;

        _due.Clear();

        lock (_lock)
        {
            List<string> stale = null;

            foreach (var (name, w) in _watches)
            {
                if (nowMs - w.AskedMs > KeepMs)
                {
                    (stale ??= []).Add(name);
                }
                else
                {
                    _due.Add(w);
                }
            }

            if (stale != null)
            {
                for (var i = 0; i < stale.Count; i++)
                {
                    _watches.Remove(stale[i]);
                }
            }

            _count = _watches.Count;
        }

        BotEvents.Live = _count > 0 ? _onEvent : null;

        if (_due.Count == 0)
        {
            return;
        }

        var budget = TerrainBudget;
        var blocks = SceneBlockBudget;

        for (var i = 0; i < _due.Count; i++)
        {
            var watch = _due[i];
            var started = Stopwatch.GetTimestamp();
            string json;

            // One bot whose frame throws does not stop the others'; its own frame says what went wrong.
            try
            {
                var bot = Find(watch);

                json = bot == null ? Missing(watch) : Frame(watch, bot, now, ref budget, ref blocks);
            }
            catch (Exception e)
            {
                if (_throws++ % 60 == 0)
                {
                    logger.Error(e, "The live window could not build a frame for {Name}", watch.Name);
                }

                json = BotJson.Object(
                    w =>
                    {
                        w.WriteString("bot", watch.Name);
                        w.WriteNumber("seq", watch.Seq + 1);
                        w.WriteString("at", DateTime.Now.ToString("HH:mm:ss"));
                        w.WriteBoolean("found", false);
                        w.WriteString("error", e.Message);
                    }
                );
            }

            lock (_lock)
            {
                watch.Seq++;
                watch.Frame = json;
            }

            var ms = Elapsed(started);

            Frames++;
            FramesMs += ms;

            if (ms > WorstFrameMs)
            {
                WorstFrameMs = ms;
            }
        }

        _due.Clear();

        // The session log, read off the loop. One read in flight at a time; a slow disk skips a beat rather than queueing them.
        if (Interlocked.CompareExchange(ref _reading, 1, 0) == 0)
        {
            _ = Task.Run(ReadLog);
        }
    }

    private static readonly Action<BotEvent> _onEvent = OnEvent;

    /// <summary>Every event about a watched bot, kept for its next frame. Called by <see cref="BotEvents.Post"/> on the loop.</summary>
    private static void OnEvent(BotEvent e)
    {
        if (e?.Bot == null || _count == 0)
        {
            return;
        }

        lock (_lock)
        {
            if (_watches.TryGetValue(e.Bot, out var watch))
            {
                watch.Events.Enqueue(e);

                while (watch.Events.Count > FeedKeep)
                {
                    watch.Events.Dequeue();
                }
            }
        }
    }

    /// <summary>The watched bot, by its serial once found, else by name among the population and those resting away.</summary>
    private static BotMobile Find(Watch watch)
    {
        if (watch.Serial != Serial.MinusOne && World.FindMobile(watch.Serial) is BotMobile { Deleted: false } known
            && string.Equals(known.Name, watch.Name, StringComparison.OrdinalIgnoreCase))
        {
            return known;
        }

        var found = Among(BotPopulation.Bots, watch.Name) ?? Among(BotPopulation.Away, watch.Name);

        if (found != null)
        {
            watch.Serial = found.Serial;

            lock (_lock)
            {
                watch.Real = found.Name;
            }
        }

        return found;
    }

    private static BotMobile Among(IReadOnlyList<BotMobile> bots, string name)
    {
        for (var i = 0; i < bots.Count; i++)
        {
            if (bots[i] is { Deleted: false } bot && string.Equals(bot.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return bot;
            }
        }

        return null;
    }

    private static string Missing(Watch watch) =>
        BotJson.Object(
            w =>
            {
                w.WriteString("bot", watch.Name);
                w.WriteNumber("seq", watch.Seq + 1);
                w.WriteString("at", DateTime.Now.ToString("HH:mm:ss"));
                w.WriteBoolean("found", false);
                w.WriteString("note", "no bot of that name in the population or resting away");
            }
        );

    private static string Frame(Watch watch, BotMobile bot, long now, ref int budget, ref int blocks)
    {
        var buffer = new ArrayBufferWriter<byte>(24 * 1024);
        var map = bot.Map;
        var away = map == null || map == Map.Internal;
        var resolve = bot.Resolve;
        var deed = away ? null : resolve?.Deed;

        using (var w = new Utf8JsonWriter(buffer, Options))
        {
            w.WriteStartObject();
            w.WriteString("bot", bot.Name ?? "?");
            w.WriteNumber("seq", watch.Seq + 1);
            w.WriteString("at", DateTime.Now.ToString("HH:mm:ss"));
            w.WriteBoolean("found", true);
            w.WriteBoolean("offline", away);
            w.WriteString("map", away ? "-" : map.Name);
            w.WriteNumber("x", bot.X);
            w.WriteNumber("y", bot.Y);
            w.WriteNumber("z", bot.Z);
            w.WriteNumber("radius", Radius);

            // ---- the body
            w.WriteString("class", bot.Class?.Name ?? "?");
            w.WriteString("role", bot.Class?.Role.ToString() ?? "?");
            w.WriteString("rank", bot.BotRank ?? "");
            w.WriteNumber("hp", bot.Hits);
            w.WriteNumber("hpMax", bot.HitsMax);
            w.WriteNumber("mana", bot.Mana);
            w.WriteNumber("manaMax", bot.ManaMax);
            w.WriteNumber("stam", bot.Stam);
            w.WriteNumber("stamMax", bot.StamMax);
            w.WriteBoolean("alive", bot.Alive);
            w.WriteBoolean("war", bot.Warmode);
            w.WriteBoolean("mounted", bot.Mounted);
            w.WriteBoolean("hidden", bot.Hidden);
            w.WriteBoolean("criminal", bot.Criminal);
            w.WriteBoolean("murderer", bot.Kills >= 5);
            BotJson.StringOrNull(w, "spell", bot.Spell is Spell spell ? spell.Name : null);
            Other(w, "combatant", bot, bot.Combatant);
            Other(w, "foe", bot, deed?.Foe);

            // ---- the work, as the snapshot writes it
            w.WriteString("standing", away ? "Offline" : resolve?.Standing.ToString() ?? "?");

            if (away)
            {
                w.WriteNull("work");
                w.WriteString("stage", BotRest.Offline(bot) ?? "away");
            }
            else if (deed != null)
            {
                w.WriteString("work", deed.Kind);
                w.WriteString("stage", deed.Stage ?? deed.ToString());
                w.WriteNumber("sinceMs", Math.Max(0, now - resolve.SinceTick));
                w.WriteNumber("expects", Math.Round(resolve.Expected, 1));
                BotJson.StringOrNull(w, "because", resolve.Because);
            }
            else
            {
                w.WriteNull("work");
                w.WriteString("stage", bot.Alive ? "nothing" : "dead");
            }

            // ---- whose it is
            var guild = bot.Guild as Guild;

            if (guild == null)
            {
                w.WriteNull("guild");
            }
            else
            {
                w.WritePropertyName("guild");
                w.WriteStartObject();
                w.WriteString("name", guild.Name);
                w.WriteString("abbr", guild.Abbreviation ?? "");
                w.WriteBoolean("leader", guild.Leader == bot);
                w.WriteEndObject();
            }

            if (bot.Squad is { Disbanded: false } squad)
            {
                var leader = squad.Leader?.Self;

                w.WritePropertyName("squad");
                w.WriteStartObject();
                w.WriteNumber("id", squad.Id);
                BotJson.StringOrNull(w, "leader", leader?.Name);
                w.WriteBoolean("leads", leader == bot);
                w.WriteNumber("members", squad.Count);
                w.WriteString("stance", squad.Stance.ToString());
                w.WriteBoolean("warring", squad.Warring);
                w.WriteEndObject();
            }
            else
            {
                w.WriteNull("squad");
            }

            w.WriteNumber("purse", bot.Backpack?.GetAmount(typeof(Gold)) ?? 0);
            w.WriteNumber("weight", bot.TotalWeight);
            w.WriteNumber("weightMax", bot.MaxWeight);

            // ---- where it is going
            var journey = bot.Journey;

            if (!away && journey is { Active: true })
            {
                var target = journey.Target;

                w.WritePropertyName("dest");
                w.WriteStartObject();
                w.WriteNumber("x", target.X);
                w.WriteNumber("y", target.Y);
                w.WriteString("why", journey.Reason ?? "");
                w.WriteBoolean("noWay", journey.NoWayThere);
                w.WriteEndObject();
            }
            else
            {
                w.WriteNull("dest");
            }

            Route(w, journey, bot, away);
            Skills(w, bot);

            if (!away)
            {
                Terrain(w, watch, map, bot.X, bot.Y, ref budget);
                Mobiles(w, bot, map);
                Items(w, bot, map);

                // The picture (02.10.2026): the bot and what lies about as the client would draw them, and the scene's number.
                if (Picture)
                {
                    WritePicture(w, watch, bot, map, ref blocks);
                }
            }

            Feed(w, watch);
            w.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    /// <summary>Somebody the bot is fighting or after: name, place, distance and health, or null.</summary>
    private static void Other(Utf8JsonWriter w, string name, BotMobile bot, Mobile other)
    {
        if (other is not { Deleted: false })
        {
            w.WriteNull(name);

            return;
        }

        w.WritePropertyName(name);
        w.WriteStartObject();
        w.WriteNumber("s", other.Serial.Value);
        w.WriteString("name", other.Name ?? "?");
        w.WriteString("kind", KindOf(other));
        w.WriteNumber("x", other.X);
        w.WriteNumber("y", other.Y);
        w.WriteNumber("dist", other.Map == bot.Map ? Math.Max(Math.Abs(other.X - bot.X), Math.Abs(other.Y - bot.Y)) : -1);
        w.WriteNumber("hp", Percent(other));
        w.WriteBoolean("alive", other.Alive);
        w.WriteEndObject();
    }

    private static int Percent(Mobile m) => m.HitsMax > 0 ? Math.Clamp(m.Hits * 100 / m.HitsMax, 0, 100) : 0;

    /// <summary>The next waypoints of the walk, as <see cref="BotWebSnapshot"/> takes them, from the bot onwards.</summary>
    private static void Route(Utf8JsonWriter w, BotJourney journey, BotMobile bot, bool away)
    {
        w.WritePropertyName("route");
        w.WriteStartArray();

        if (!away && journey is { Active: true })
        {
            var errand = journey.Current;
            var route = errand?.Route;
            var written = 0;

            if (route != null && route.Count > 0)
            {
                for (var i = Math.Clamp(errand.Leg, 0, route.Count); i < route.Count && written < MostRoute; i++, written++)
                {
                    w.WriteNumberValue(route[i].X);
                    w.WriteNumberValue(route[i].Y);
                }
            }
            else
            {
                var plan = journey.Plan;

                for (var i = Math.Max(0, plan.Count - journey.Remaining); i < plan.Count && written < MostRoute; i++, written++)
                {
                    w.WriteNumberValue(plan[i].X);
                    w.WriteNumberValue(plan[i].Y);
                }
            }
        }

        w.WriteEndArray();
    }

    private static readonly List<(string Name, double Value)> _skillScratch = new(64);

    private static void Skills(Utf8JsonWriter w, BotMobile bot)
    {
        _skillScratch.Clear();

        var skills = bot.Skills;

        if (skills != null)
        {
            for (var i = 0; i < skills.Length; i++)
            {
                if (skills[i] is { } skill && skill.Value >= 10.0)
                {
                    _skillScratch.Add((skill.Name, skill.Value));
                }
            }
        }

        _skillScratch.Sort((a, b) => b.Value.CompareTo(a.Value));

        w.WritePropertyName("skills");
        w.WriteStartArray();

        for (var i = 0; i < _skillScratch.Count && i < 6; i++)
        {
            w.WriteStartArray();
            w.WriteStringValue(_skillScratch[i].Name);
            w.WriteNumberValue(Math.Round(_skillScratch[i].Value, 1));
            w.WriteEndArray();
        }

        w.WriteEndArray();
    }

    // ---- terrain --------------------------------------------------------------------------------------------

    /// <summary>
    /// The radar colours of the square round the bot plus <see cref="Slack"/>, as base64 of little-endian sixteen-bit colours
    /// in rows, the same colours the map picture is drawn from. Centred again only when the bot has walked further than the
    /// slack or the last square was not finished; 0xFFFF is a tile not read yet, left for the next beat's budget.
    /// </summary>
    private static void Terrain(Utf8JsonWriter w, Watch watch, Map map, int x, int y, ref int budget)
    {
        var colours = BotWebMap.Radar();
        var half = Radius + Slack;
        var side = half * 2 + 1;

        var moved = watch.Terrain == null || watch.TerrainMap != map
                    || Math.Max(Math.Abs(x - watch.TerrainX), Math.Abs(y - watch.TerrainY)) > Slack;

        // Centred again when the bot has walked past the slack; filled in where it stands while the last square is unfinished.
        if (colours != null && (moved || !watch.TerrainComplete))
        {
            var cx = moved ? x : watch.TerrainX;
            var cy = moved ? y : watch.TerrainY;
            var window = new ushort[side * side];
            var complete = Square(map, colours, cx - half, cy - half, side, window, ref budget);

            watch.TerrainMap = map;
            watch.TerrainX = cx;
            watch.TerrainY = cy;
            watch.TerrainComplete = complete;
            watch.Terrain = Convert.ToBase64String(MemoryMarshal.AsBytes(window.AsSpan()));
        }

        if (watch.Terrain == null)
        {
            w.WriteNull("terrain");

            return;
        }

        w.WritePropertyName("terrain");
        w.WriteStartObject();
        w.WriteNumber("x0", watch.TerrainX - half);
        w.WriteNumber("y0", watch.TerrainY - half);
        w.WriteNumber("side", side);
        w.WriteBoolean("complete", watch.TerrainComplete);
        w.WriteString("data", watch.Terrain);
        w.WriteEndObject();
    }

    /// <summary>Fills the window from the block cache, reading the blocks it lacks while the budget lasts. True when whole.</summary>
    private static bool Square(Map map, ushort[] colours, int x0, int y0, int side, ushort[] window, ref int budget)
    {
        var complete = true;
        var bx0 = x0 >> 3;
        var by0 = y0 >> 3;
        var bx1 = (x0 + side - 1) >> 3;
        var by1 = (y0 + side - 1) >> 3;

        for (var by = by0; by <= by1; by++)
        {
            for (var bx = bx0; bx <= bx1; bx++)
            {
                var block = Block(map, colours, bx, by, ref budget, out var outside);

                if (block == null && !outside)
                {
                    complete = false;
                }

                for (var dy = 0; dy < 8; dy++)
                {
                    var wy = by * 8 + dy - y0;

                    if (wy < 0 || wy >= side)
                    {
                        continue;
                    }

                    for (var dx = 0; dx < 8; dx++)
                    {
                        var wx = bx * 8 + dx - x0;

                        if (wx < 0 || wx >= side)
                        {
                            continue;
                        }

                        window[wy * side + wx] = block != null ? block[dy * 8 + dx] : outside ? (ushort)0 : (ushort)0xFFFF;
                    }
                }
            }
        }

        return complete;
    }

    private static ushort[] Block(Map map, ushort[] colours, int bx, int by, ref int budget, out bool outside)
    {
        outside = bx < 0 || by < 0 || bx * 8 >= map.Width || by * 8 >= map.Height;

        if (outside)
        {
            return null;
        }

        var key = ((long)map.MapID << 40) | ((long)bx << 20) | (uint)by;

        if (_blocks.TryGetValue(key, out var block))
        {
            return block;
        }

        if (budget < 64)
        {
            return null;
        }

        var started = Stopwatch.GetTimestamp();
        var tiles = map.Tiles;

        budget -= 64;
        block = new ushort[64];

        for (var dy = 0; dy < 8; dy++)
        {
            var y = by * 8 + dy;

            for (var dx = 0; dx < 8; dx++)
            {
                var x = bx * 8 + dx;

                block[dy * 8 + dx] = x < map.Width && y < map.Height ? BotWebMap.Colour(tiles, colours, x, y) : (ushort)0;
            }
        }

        if (_blocks.Count >= MostBlocks)
        {
            _blocks.Clear();
            Emptied++;
        }

        _blocks[key] = block;
        _blockCount = _blocks.Count;
        TerrainTiles += 64;
        TerrainMs += Elapsed(started);

        return block;
    }

    // ---- who and what is about ------------------------------------------------------------------------------

    private static void Mobiles(Utf8JsonWriter w, BotMobile bot, Map map)
    {
        w.WritePropertyName("mobiles");
        w.WriteStartArray();

        var written = 0;

        foreach (var m in map.GetMobilesInRange(bot.Location, Radius))
        {
            if (written >= MostMobiles)
            {
                break;
            }

            if (m == bot || m.Deleted)
            {
                continue;
            }

            // Staff and the debuggers are hidden above Player; nobody in the world sees them, so neither does the window.
            if (m.Hidden && m.AccessLevel > AccessLevel.Player)
            {
                continue;
            }

            written++;

            var kind = KindOf(m);
            var hostile = m.Combatant == bot;
            var aggressive = false;

            if (m is BaseCreature creature)
            {
                aggressive = creature.FightMode is not (FightMode.None or FightMode.Aggressor);
                hostile = hostile || BotThreat.Menacing(bot, creature);
            }
            else if (m is BotMobile other)
            {
                hostile = hostile || BotRegard.Hostile(bot, other);
            }

            w.WriteStartObject();
            w.WriteString("n", m.Name ?? "?");
            w.WriteNumber("x", m.X);
            w.WriteNumber("y", m.Y);
            w.WriteNumber("hp", Percent(m));
            w.WriteString("k", kind);
            w.WriteNumber("noto", Notoriety.Compute(bot, m));

            if (!m.Alive)
            {
                w.WriteBoolean("dead", true);
            }

            if (m.Hidden)
            {
                w.WriteBoolean("hidden", true);
            }

            if (hostile)
            {
                w.WriteBoolean("hostile", true);
            }

            if (aggressive)
            {
                w.WriteBoolean("aggr", true);
            }

            if (m.Combatant is { Deleted: false } fighting)
            {
                w.WriteString("fights", fighting.Name ?? "?");
                w.WriteNumber("fx", fighting.X);
                w.WriteNumber("fy", fighting.Y);
            }

            if (m is BotMobile mate)
            {
                w.WriteString("cls", mate.Class?.Name ?? "?");
                BotJson.StringOrNull(w, "g", (mate.Guild as Guild)?.Abbreviation);
                BotJson.StringOrNull(w, "work", mate.Resolve?.Deed?.Kind);
            }

            if (Picture)
            {
                Look(w, m);
            }

            w.WriteEndObject();
        }

        w.WriteEndArray();
    }

    private static string KindOf(Mobile m) =>
        m switch
        {
            BotMobile => "bot",
            PlayerMobile => "player",
            BaseVendor => "vendor",
            BaseCreature { Controlled: true } or BaseCreature { Summoned: true } => "pet",
            BaseCreature c when c.Body.IsAnimal => "animal",
            BaseCreature c when c.Body.IsHuman && c.FightMode is (FightMode.None or FightMode.Aggressor) => "npc",
            BaseCreature => "monster",
            _ => "npc"
        };

    private static void Items(Utf8JsonWriter w, BotMobile bot, Map map)
    {
        w.WritePropertyName("items");
        w.WriteStartArray();

        var written = 0;
        _groundScratch.Clear();

        foreach (var item in map.GetItemsInRange(bot.Location, Radius))
        {
            if (Picture)
            {
                NoteGround(bot, item);
            }

            if (written >= MostItems)
            {
                if (!Picture || _groundScratch.Count >= MostGround)
                {
                    break;
                }

                continue;
            }

            if (item is Corpse corpse && !corpse.Deleted)
            {
                written++;

                var owner = corpse.Owner;

                w.WriteStartObject();
                w.WriteString("k", "corpse");
                w.WriteNumber("x", corpse.X);
                w.WriteNumber("y", corpse.Y);
                w.WriteString("of", owner?.Name ?? corpse.Name ?? "?");

                if (owner is BotMobile)
                {
                    w.WriteBoolean("bot", true);
                }

                if (owner == bot)
                {
                    w.WriteBoolean("own", true);
                }

                w.WriteEndObject();
            }
            else if (item is Campfire flame && !flame.Deleted)
            {
                written++;

                string keeper = null;
                var fires = BotCamp.Fires;

                for (var i = 0; i < fires.Count; i++)
                {
                    if (fires[i].Flame == flame)
                    {
                        keeper = fires[i].KeeperName;

                        break;
                    }
                }

                w.WriteStartObject();
                w.WriteString("k", "fire");
                w.WriteNumber("x", flame.X);
                w.WriteNumber("y", flame.Y);
                BotJson.StringOrNull(w, "of", keeper);

                if (keeper != null && keeper == bot.Name)
                {
                    w.WriteBoolean("own", true);
                }

                w.WriteEndObject();
            }
        }

        // Its own shop by a bank, which is a place the bot stands rather than an item. See BotShopkeep.
        var open = BotShopkeep.Open;

        for (var i = 0; i < open.Count; i++)
        {
            var front = open[i];

            if (front.Keeper != bot || !BotShopkeep.Live(front))
            {
                continue;
            }

            w.WriteStartObject();
            w.WriteString("k", "shop");
            w.WriteNumber("x", front.Location.X);
            w.WriteNumber("y", front.Location.Y);
            w.WriteString("of", bot.Name);
            w.WriteBoolean("own", true);
            w.WriteString("town", front.Town);
            w.WriteNumber("units", front.Units);
            w.WriteNumber("sold", front.Sold);
            w.WriteEndObject();
        }

        w.WriteEndArray();
    }

    /// <summary>
    /// The newest events about the bot and the newest session-log lines naming it, at most <see cref="FeedKeep"/> of each.
    /// Every frame carries the newest, not only what came since the frame before, so a reader that misses a frame misses
    /// nothing: the events have their sequence numbers and the lines their own, and the page keeps what it has not seen.
    /// </summary>
    private static void Feed(Utf8JsonWriter w, Watch watch)
    {
        _eventScratch.Clear();
        _logScratch.Clear();

        lock (_lock)
        {
            _eventScratch.AddRange(watch.Events);
            _logScratch.AddRange(watch.Log);
        }

        w.WritePropertyName("events");
        w.WriteStartArray();

        for (var i = 0; i < _eventScratch.Count; i++)
        {
            w.WriteRawValue(_eventScratch[i].Json, true);
        }

        w.WriteEndArray();

        w.WritePropertyName("log");
        w.WriteStartArray();

        for (var i = 0; i < _logScratch.Count; i++)
        {
            w.WriteStartArray();
            w.WriteNumberValue(_logScratch[i].Id);
            w.WriteStringValue(_logScratch[i].Text);
            w.WriteEndArray();
        }

        w.WriteEndArray();

        _eventScratch.Clear();
        _logScratch.Clear();
    }

    // ---- the session log, off the loop ----------------------------------------------------------------------

    /// <summary>
    /// Reads what the session log gained since the last pass and hands each watched bot the lines that name it. Thread pool:
    /// a file and the lock, never the world. A bot newly watched is given the lines naming it in the last
    /// <see cref="TailBytes"/> of the file, so its window does not open empty.
    /// </summary>
    private static void ReadLog()
    {
        try
        {
            var nowMs = Environment.TickCount64;

            if (_logFile == null || nowMs - _logFoundMs > 30000)
            {
                _logFoundMs = nowMs;

                var newest = BotWebServer.NewestSessionLog();

                if (newest != _logFile)
                {
                    _logFile = newest;

                    lock (_lock)
                    {
                        foreach (var (_, w) in _watches)
                        {
                            w.LogUpTo = -1;
                        }
                    }
                }
            }

            if (_logFile == null)
            {
                return;
            }

            using var stream = new FileStream(_logFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);

            var length = stream.Length;
            var fresh = Math.Max(0, length - TailBytes);
            List<(Watch Watch, string Name, long From)> who = [];

            lock (_lock)
            {
                foreach (var (_, w) in _watches)
                {
                    if (w.LogUpTo < 0 || w.LogUpTo > length)
                    {
                        w.LogUpTo = fresh;
                    }

                    who.Add((w, w.Real ?? w.Name, w.LogUpTo));
                }
            }

            if (who.Count == 0)
            {
                return;
            }

            var start = long.MaxValue;

            for (var i = 0; i < who.Count; i++)
            {
                start = Math.Min(start, who[i].From);
            }

            var take = (int)Math.Min(length - start, MostLogBytes);

            if (take <= 0)
            {
                return;
            }

            var bytes = new byte[take];

            stream.Seek(start, SeekOrigin.Begin);

            var read = 0;

            while (read < take)
            {
                var n = stream.Read(bytes, read, take - read);

                if (n <= 0)
                {
                    break;
                }

                read += n;
            }

            var lastNewline = read > 0 ? Array.LastIndexOf(bytes, (byte)'\n', read - 1) : -1;

            if (lastNewline < 0)
            {
                return;
            }

            // Lines are matched by where they start in the file, so each bot is given only what lies past its own mark; a line cut
            // at the start of a fresh read is skipped rather than half-matched.
            List<(Watch Watch, string Text)> found = [];
            var at = 0;

            while (at <= lastNewline)
            {
                var end = Array.IndexOf(bytes, (byte)'\n', at, lastNewline - at + 1);
                var offset = start + at;
                var whole = at > 0 || start == 0 || Preceded(stream, start);

                if (whole && end > at)
                {
                    var line = Encoding.UTF8.GetString(bytes, at, end - at).TrimEnd('\r');

                    for (var i = 0; i < who.Count; i++)
                    {
                        if (offset >= who[i].From && Mentions(line, who[i].Name))
                        {
                            found.Add((who[i].Watch, Shorten(line)));
                        }
                    }
                }

                at = end + 1;
            }

            var upTo = start + lastNewline + 1;

            lock (_lock)
            {
                for (var i = 0; i < found.Count; i++)
                {
                    var (watch, text) = found[i];

                    watch.Log.Enqueue((++_logId, text));

                    while (watch.Log.Count > FeedKeep)
                    {
                        watch.Log.Dequeue();
                    }
                }

                for (var i = 0; i < who.Count; i++)
                {
                    who[i].Watch.LogUpTo = Math.Max(who[i].Watch.LogUpTo, upTo);
                }

                LogLines += found.Count;
            }
        }
        catch
        {
            // A log being rolled over or locked for a moment: the next beat reads it.
        }
        finally
        {
            Interlocked.Exchange(ref _reading, 0);
        }
    }

    /// <summary>Whether the byte before <paramref name="start"/> is a line end, so the read begins on a whole line.</summary>
    private static bool Preceded(FileStream stream, long start)
    {
        var here = stream.Position;

        try
        {
            stream.Seek(start - 1, SeekOrigin.Begin);

            return stream.ReadByte() == '\n';
        }
        finally
        {
            stream.Seek(here, SeekOrigin.Begin);
        }
    }

    /// <summary>The name as a whole word: "Ash" is not in "Ashford".</summary>
    private static bool Mentions(string line, string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return false;
        }

        var from = 0;

        while (true)
        {
            var at = line.IndexOf(name, from, StringComparison.Ordinal);

            if (at < 0)
            {
                return false;
            }

            var before = at == 0 || !char.IsLetterOrDigit(line[at - 1]);
            var after = at + name.Length >= line.Length || !char.IsLetterOrDigit(line[at + name.Length]);

            if (before && after)
            {
                return true;
            }

            from = at + 1;
        }
    }

    /// <summary>The line without the source tag the log ends every line with.</summary>
    private static string Shorten(string line)
    {
        var cut = line.LastIndexOf(" <s:", StringComparison.Ordinal);

        return cut > 0 ? line[..cut] : line;
    }

    // ---- the debugger squad ---------------------------------------------------------------------------------

    private static void Squad()
    {
        var started = Stopwatch.GetTimestamp();

        IReadOnlyList<BotDebuggerReport> squad = null;

        try
        {
            squad = BotWebHooks.Debuggers?.Invoke();
        }
        catch
        {
            // The minds assembly answers for itself; a throw there is not the page's fault.
        }

        _debuggers = BotJson.Object(
            w =>
            {
                w.WriteString("at", DateTime.Now.ToString("HH:mm:ss"));

                if (squad == null)
                {
                    w.WriteNull("debuggers");
                    w.WriteString("note", "the minds assembly has not reported a squad");

                    return;
                }

                w.WritePropertyName("debuggers");
                w.WriteStartArray();

                for (var i = 0; i < squad.Count; i++)
                {
                    if (squad[i] is { } d)
                    {
                        Debugger(w, d);
                    }
                }

                w.WriteEndArray();
            }
        );

        DebuggerPages++;
        DebuggerMs += Elapsed(started);
    }

    private static void Debugger(Utf8JsonWriter w, BotDebuggerReport d)
    {
        w.WriteStartObject();
        w.WriteString("name", d.Name ?? "?");
        w.WriteNumber("hue", d.Hue);
        BotJson.StringOrNull(w, "charge", d.Charge);
        w.WriteBoolean("organiser", d.Organiser);
        BotJson.StringOrNull(w, "map", d.Map);
        w.WriteNumber("x", d.X);
        w.WriteNumber("y", d.Y);
        w.WriteBoolean("shown", d.Shown);
        BotJson.StringOrNull(w, "doing", d.Doing);
        BotJson.StringOrNull(w, "doingWhat", d.DoingWhat);
        BotJson.StringOrNull(w, "subject", d.Subject);

        if (d.ThoughtAt is { } at)
        {
            w.WritePropertyName("thought");
            w.WriteStartObject();
            w.WriteString("at", at.ToString("HH:mm:ss"));
            BotJson.StringOrNull(w, "kind", d.ThoughtKind);
            BotJson.StringOrNull(w, "question", d.Question);
            BotJson.StringOrNull(w, "text", d.Thought);
            BotJson.StringOrNull(w, "answer", d.Answer);
            BotJson.StringOrNull(w, "about", d.About);

            if (d.Sure >= 0)
            {
                w.WriteNumber("sure", Math.Round(d.Sure, 2));
            }

            w.WriteEndObject();
        }
        else
        {
            w.WriteNull("thought");
        }

        if (d.Asking != null)
        {
            w.WritePropertyName("asking");
            w.WriteStartObject();
            w.WriteString("what", d.Asking);
            BotJson.StringOrNull(w, "since", d.AskingSince?.ToString("HH:mm:ss"));
            w.WriteNumber("forMs", d.AskingSince is { } since ? Math.Max(0, (long)(DateTime.Now - since).TotalMilliseconds) : 0);
            w.WriteEndObject();
        }
        else
        {
            w.WriteNull("asking");
        }

        w.WritePropertyName("counts");
        w.WriteStartObject();
        w.WriteNumber("asked", d.Asked);
        w.WriteNumber("findings", d.Findings);
        w.WriteNumber("quiet", d.Quiet);
        w.WriteNumber("reflections", d.Reflections);
        w.WriteNumber("labels", d.Labels);
        w.WriteNumber("echoes", d.Echoes);
        w.WriteNumber("hops", d.Hops);
        w.WriteEndObject();

        w.WritePropertyName("feed");
        w.WriteStartArray();

        if (d.Feed != null)
        {
            for (var i = 0; i < d.Feed.Count; i++)
            {
                if (d.Feed[i] is { } line)
                {
                    w.WriteStartArray();
                    w.WriteStringValue(line.At.ToString("HH:mm:ss"));
                    w.WriteStringValue(line.Text ?? "");
                    w.WriteEndArray();
                }
            }
        }

        w.WriteEndArray();
        w.WriteEndObject();
    }

    // ---- what it costs --------------------------------------------------------------------------------------

    public static void Health(Utf8JsonWriter w)
    {
        w.WritePropertyName("live");
        w.WriteStartObject();
        w.WriteBoolean("enabled", Enabled);
        w.WriteNumber("watched", _count);
        w.WritePropertyName("names");
        w.WriteStartArray();

        foreach (var name in Names())
        {
            w.WriteStringValue(name);
        }

        w.WriteEndArray();
        w.WriteNumber("frames", Frames);
        w.WriteNumber("msEach", Frames <= 0 ? 0.0 : Math.Round(FramesMs / Frames, 3));
        w.WriteNumber("worstFrameMs", Math.Round(WorstFrameMs, 2));
        w.WriteNumber("beats", Beats);
        w.WriteNumber("msPerBeat", Beats <= 0 ? 0.0 : Math.Round(BeatsMs / Beats, 3));
        w.WriteNumber("lastBeatMs", Math.Round(LastBeatMs, 3));
        w.WriteNumber("worstBeatMs", Math.Round(WorstBeatMs, 2));
        w.WriteNumber("terrainTiles", TerrainTiles);
        w.WriteNumber("terrainMs", Math.Round(TerrainMs, 1));
        w.WriteNumber("terrainBlocks", _blockCount);
        w.WriteNumber("terrainEmptied", Emptied);
        w.WriteNumber("debuggerPages", DebuggerPages);
        w.WriteNumber("debuggerMsEach", DebuggerPages <= 0 ? 0.0 : Math.Round(DebuggerMs / DebuggerPages, 3));
        w.WriteNumber("logLines", LogLines);
        PictureHealth(w);
        w.WriteEndObject();
    }

    public static string Describe() =>
        Beats == 0
            ? "the live window has built nothing"
            : $"the live window built {Frames} frames at {FramesMs / Math.Max(1, Frames):F2}ms each ({WorstFrameMs:F1}ms the worst), {Beats} beats at {BeatsMs / Beats:F2}ms, "
              + $"{TerrainTiles} terrain tiles read in {TerrainMs:F0}ms, {DebuggerPages} debugger pages; {_count} bots watched now";
}
