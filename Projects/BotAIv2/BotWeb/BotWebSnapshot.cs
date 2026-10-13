using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using Server.Engines.Pathing.Tiered;
using Server.Guilds;
using Server.Items;
using Server.Logging;
using Server.Mobiles;

namespace Server.BotAI.V2;

/// <summary>
/// The whole shard as one JSON document, rebuilt on the game loop every couple of seconds and handed to the
/// web server as a string.
///
/// <para>
/// <b>Built here, served there.</b> Everything the page shows is read from the world in one pass on the
/// loop, written once, and then given away; the server never touches a bot. That is the same division as
/// the summary lines in the log — the loop writes, everybody else reads — and it is why a browser left
/// open all night costs the population nothing.
/// </para>
///
/// <para>
/// <b>Every number here is one the log already prints.</b> The page adds no counters of its own; it reads
/// the same statics the five-minute summary reads, so a figure on the page and a figure in the log can never
/// disagree about the shard.
/// </para>
/// </summary>
public static class BotWebSnapshot
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotWebSnapshot));

    public static int SnapshotMs { get; set; } = 2000;

    public static int PagesMs { get; set; } = 5000;

    public static int CraftMs { get; set; } = 15000;

    /// <summary>Plans without getting nearer before a walking bot is called stuck on the page.</summary>
    public static int StuckPlans { get; set; } = 3;

    /// <summary>Most route points sent per bot; a long route is thinned to this many.</summary>
    public static int MostRoutePoints { get; set; } = 64;

    public static volatile string State = "{\"at\":null,\"bots\":[]}";

    public static volatile string Paths = "{\"recent\":[],\"byBot\":[],\"byReason\":[],\"byOutcome\":{},\"hot\":[]}";

    public static volatile string Craft = "{\"chains\":[],\"refusals\":[],\"wants\":[]}";

    public static long Built { get; private set; }

    public static double LastBuildMs { get; private set; }

    public static double WorstBuildMs { get; private set; }

    public static double TotalBuildMs { get; private set; }

    private static long _snapTick;

    private static long _pagesTick;

    private static long _craftTick;

    private static readonly object _lock = new();

    private static readonly Dictionary<string, string> _bots = new(StringComparer.OrdinalIgnoreCase);

    private static string _build;

    private static DateTime _started;

    private static readonly JsonWriterOptions Options = new() { Indented = false, SkipValidation = true };

    public static void Start()
    {
        _started = DateTime.Now;

        try
        {
            var dll = typeof(BotWebSnapshot).Assembly.Location;

            _build = string.IsNullOrEmpty(dll) ? "?" : File.GetLastWriteTime(dll).ToString("yyyy-MM-dd HH:mm");
        }
        catch
        {
            _build = "?";
        }
    }

    /// <summary>Called a few times a second from the module's timer. Game loop only.</summary>
    public static void Tick(long now)
    {
        if (!BotWebMap.Ready && !BotWebMap.Failed)
        {
            BotWebMap.Slice();
        }

        // The live window: a frame a second for each bot somebody is looking at, and the debuggers' page. A tick comparison
        // and two reads when nobody is. See BotWebLive (02.10.2026).
        BotWebLive.Tick(now);

        if (now - _snapTick >= SnapshotMs)
        {
            _snapTick = now;

            try
            {
                Build(now);
            }
            catch (Exception e)
            {
                logger.Error(e, "The snapshot threw; the page keeps the last one");
            }
        }

        if (now - _pagesTick >= PagesMs)
        {
            _pagesTick = now;

            try
            {
                Paths = BuildPaths();
            }
            catch (Exception e)
            {
                logger.Error(e, "The paths page threw; it keeps the last one");
            }
        }

        if (now - _craftTick >= CraftMs)
        {
            _craftTick = now;

            try
            {
                Craft = BotWebCraft.Build();
            }
            catch (Exception e)
            {
                logger.Error(e, "The craft page threw; it keeps the last one");
            }
        }
    }

    private readonly struct Roll
    {
        public readonly Dictionary<string, int> ByClass;
        public readonly Dictionary<string, int> ByStanding;
        public readonly Dictionary<string, int> ByWork;
        public readonly List<(string Name, string Reason, long SinceMs, int X, int Y, Point3D Target)> Stuck;
        public readonly int[] Counts;

        public Roll()
        {
            ByClass = new Dictionary<string, int>(StringComparer.Ordinal);
            ByStanding = new Dictionary<string, int>(StringComparer.Ordinal);
            ByWork = new Dictionary<string, int>(StringComparer.Ordinal);
            Stuck = [];
            Counts = new int[3];
        }
    }

    private static void Build(long now)
    {
        var started = Stopwatch.GetTimestamp();
        var roll = new Roll();
        var buffer = new ArrayBufferWriter<byte>(64 * 1024);
        Dictionary<string, string> perBot = new(StringComparer.OrdinalIgnoreCase);
        var map = BotPopulation.Home ?? Map.Felucca;

        using (var w = new Utf8JsonWriter(buffer, Options))
        {
            w.WriteStartObject();

            var bots = BotPopulation.Bots;
            var away = BotPopulation.Away;

            // The bots first, because the roll-up above them is made while they are written.
            var botsBuffer = new ArrayBufferWriter<byte>(32 * 1024);

            using (var bw = new Utf8JsonWriter(botsBuffer, Options))
            {
                bw.WriteStartArray();

                for (var i = 0; i < bots.Count; i++)
                {
                    var bot = bots[i];

                    if (bot is not { Deleted: false })
                    {
                        continue;
                    }

                    var json = BotJsonOf(bot, now, roll, false);

                    perBot[bot.Name ?? $"#{bot.Serial}"] = json;
                    bw.WriteRawValue(json, true);
                }

                for (var i = 0; i < away.Count; i++)
                {
                    var bot = away[i];

                    if (bot is not { Deleted: false })
                    {
                        continue;
                    }

                    var json = BotJsonOf(bot, now, roll, true);

                    perBot[bot.Name ?? $"#{bot.Serial}"] = json;
                    bw.WriteRawValue(json, true);
                }

                bw.WriteEndArray();
            }

            w.WriteString("at", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"));
            w.WriteNumber("uptimeMs", (long)(DateTime.Now - _started).TotalMilliseconds);
            w.WriteNumber("quad", BotQuad.Side);
            w.WriteString("build", _build ?? "?");

            w.WritePropertyName("map");
            w.WriteStartObject();
            w.WriteString("name", map.Name);
            w.WriteNumber("width", map.Width);
            w.WriteNumber("height", map.Height);
            w.WriteString("image", BotWebMap.Ready ? "/api/map.png" : null);
            w.WriteNumber("scale", BotWebMap.Scale);
            w.WriteEndObject();

            w.WritePropertyName("population");
            w.WriteStartObject();
            w.WriteNumber("total", roll.Counts[0] + roll.Counts[1] + roll.Counts[2]);
            w.WriteNumber("alive", roll.Counts[0]);
            w.WriteNumber("dead", roll.Counts[1]);
            w.WriteNumber("offline", roll.Counts[2]);
            Counts(w, "byClass", roll.ByClass);
            Counts(w, "byStanding", roll.ByStanding);
            Counts(w, "byWork", roll.ByWork);
            w.WriteEndObject();

            WriteWork(w);

            // What the bots earn, the special classes left out (Patrick, 29.09.2026). See BotEarnings.
            BotEarnings.Write(w);
            WritePaths(w, map);

            w.WritePropertyName("bots");
            w.WriteRawValue(Encoding.UTF8.GetString(botsBuffer.WrittenSpan), true);

            w.WritePropertyName("stuck");
            w.WriteStartArray();

            roll.Stuck.Sort((a, b) => b.SinceMs.CompareTo(a.SinceMs));

            for (var i = 0; i < roll.Stuck.Count && i < 40; i++)
            {
                var (name, reason, since, x, y, target) = roll.Stuck[i];

                w.WriteStartObject();
                w.WriteString("name", name);
                w.WriteString("reason", reason);
                w.WriteNumber("sinceMs", since);
                w.WriteNumber("x", x);
                w.WriteNumber("y", y);

                if (target == Point3D.Zero)
                {
                    w.WriteNull("target");
                }
                else
                {
                    BotJson.PointObject(w, "target", target);
                }

                w.WriteEndObject();
            }

            w.WriteEndArray();

            WriteGuilds(w);
            WriteWars(w);

            // Relations between guilds, each guild's standing in its towns, and the meetings standing (29.09.2026).
            BotDiplomacyWeb.Write(w);
            WriteSquads(w);
            WriteGates(w);
            WriteTowns(w);

            // Bots' shops by the banks, written by their own module (29.09.2026). See BotShopkeep.Web.
            BotShopkeep.Web(w);

            // The guilds' colonies on the islands, the parties at the docks and at sea, and The Shadow on the quays (07.10.2026, sea20).
            BotColonies.Web(w);

            // The Needs board by Patrick's measure: lines opened, met in their first hour, the share, open now, failed by cause, and
            // the money the wants hold (02.10.2026, build 391). See BotLines.Web.
            BotLines.Web(w);

            // How long the bots take to answer a blow, a creature coming, a failing health, a cry, a wound and a fight's end
            // (06.10.2026). See BotReflex.
            BotReflex.Web(w);

            // The relics: each with its holder, place and pin, the lair standing, the counters and the news (07.10.2026, relics20).
            BotRelics.Web(w);

            // The bots' own houses (BotAbode): owner, kind, place, and whether the owner is at home (07.10.2026).
            WriteAbodes(w);

            // The daily cycle: the part of the day, offers taken by part, tavern guests by hour, who sleeps where (PLAN-life A1).
            BotDay.Write(w);

            // The stories and the Chronicle: books, chapters, the model's retellings and the day's entries (PLAN-life A3, 13.10.2026).
            BotStory.Web(w);

            // The zone watch: the danger map's accuracy both ways, the keeper, the end (07.10.2026). See BotZoneWatch.
            BotZoneWatch.Web(w);

            WriteBarred(w);
            WriteAlarms(w);
            WriteMinds(w);

            w.WriteNumber("eventsSeq", BotEvents.Seq);
            w.WriteNumber("stuckCount", roll.Stuck.Count);
            w.WriteEndObject();
        }

        var state = Encoding.UTF8.GetString(buffer.WrittenSpan);

        lock (_lock)
        {
            _bots.Clear();

            foreach (var (name, json) in perBot)
            {
                _bots[name] = json;
            }
        }

        State = state;
        Built++;

        var ms = (Stopwatch.GetTimestamp() - started) * 1000.0 / Stopwatch.Frequency;

        LastBuildMs = ms;
        TotalBuildMs += ms;

        if (ms > WorstBuildMs)
        {
            WorstBuildMs = ms;
        }

        BotWebHistory.Sample(now, roll.Counts[0], roll.Stuck.Count);
    }

    private static void Counts(Utf8JsonWriter w, string name, Dictionary<string, int> counts)
    {
        w.WritePropertyName(name);
        w.WriteStartObject();

        foreach (var (key, count) in counts)
        {
            w.WriteNumber(key, count);
        }

        w.WriteEndObject();
    }

    private static void Bump(Dictionary<string, int> counts, string key)
    {
        if (key == null)
        {
            return;
        }

        counts[key] = counts.TryGetValue(key, out var n) ? n + 1 : 1;
    }

    private static string BotJsonOf(BotMobile bot, long now, Roll roll, bool away)
    {
        var buffer = new ArrayBufferWriter<byte>(1024);

        using (var w = new Utf8JsonWriter(buffer, Options))
        {
            w.WriteStartObject();

            var resolve = bot.Resolve;
            var journey = bot.Journey;
            var deed = resolve?.Deed;
            var guild = bot.Guild as Guild;
            var alive = bot.Alive && !away && bot.Map != null && bot.Map != Map.Internal;
            var offline = away || bot.Map == null || bot.Map == Map.Internal;

            roll.Counts[offline ? 2 : alive ? 0 : 1]++;
            Bump(roll.ByClass, bot.Class?.Name);
            Bump(roll.ByStanding, offline ? "Offline" : resolve?.Standing.ToString());

            if (!offline && deed != null)
            {
                Bump(roll.ByWork, deed.Kind);
            }

            w.WriteString("name", bot.Name ?? "?");
            w.WriteString("class", bot.Class?.Name ?? "?");
            w.WriteString("role", bot.Class?.Role.ToString() ?? "?");
            w.WriteString("people", bot.People?.Name ?? "Human");
            w.WriteString("race", bot.Race?.Name ?? "Human");
            w.WriteBoolean("minded", bot.Minded);
            w.WriteBoolean("female", bot.Female);
            BotJson.StringOrNull(w, "guild", guild?.Name);
            BotJson.StringOrNull(w, "guildAbbr", guild?.Abbreviation);
            BotJson.StringOrNull(w, "residence", BotResidence.NameOf(bot));

            if (bot.Squad is { Disbanded: false } squad)
            {
                w.WriteNumber("squad", squad.Id);
            }
            else
            {
                w.WriteNull("squad");
            }

            w.WriteNumber("x", bot.X);
            w.WriteNumber("y", bot.Y);
            w.WriteNumber("z", bot.Z);
            w.WriteString("map", bot.Map?.Name ?? "-");
            w.WriteBoolean("alive", alive);
            w.WriteBoolean("offline", offline);
            w.WriteNumber("hp", bot.Hits);
            w.WriteNumber("hpMax", bot.HitsMax);
            w.WriteNumber("mana", bot.Mana);
            w.WriteNumber("stam", bot.Stam);
            w.WriteBoolean("mounted", bot.Mounted);
            w.WriteBoolean("hidden", bot.Hidden);
            w.WriteBoolean("criminal", bot.Criminal);
            w.WriteBoolean("murderer", bot.Kills >= 5);
            w.WriteString("standing", offline ? "Offline" : resolve?.Standing.ToString() ?? "?");

            if (offline)
            {
                BotJson.StringOrNull(w, "work", null);
                w.WriteString("stage", BotRest.Offline(bot) ?? "away");
                w.WriteNumber("sinceMs", 0);
                w.WriteNumber("expects", 0);
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
                w.WriteString("stage", alive ? "nothing" : "dead");
                w.WriteNumber("sinceMs", 0);
                w.WriteNumber("expects", 0);
            }

            string stuckReason = null;
            var target = Point3D.Zero;

            if (journey is { Active: true } && !offline)
            {
                target = journey.Target;

                BotJson.PointObject(w, "target", target);
                w.WriteString("targetWhy", journey.Reason ?? "");
                w.WriteBoolean("walking", journey.Walking);
                w.WriteBoolean("moving", journey.Moving);
                w.WriteNumber("plans", journey.Plans);
                w.WriteNumber("plansSinceCloser", journey.PlansSinceCloser);
                w.WriteBoolean("noWay", journey.NoWayThere);
                w.WriteNumber("routeLeft", journey.RouteLeft);
                w.WriteNumber("planLeft", journey.Remaining);
                w.WriteBoolean("legging", journey.Legging);

                WriteRoute(w, journey, bot);

                if (journey.NoWayThere)
                {
                    stuckReason = $"the graph says there is no way to ({target.X}, {target.Y})";
                }
                else if (journey.Hopeless)
                {
                    stuckReason = $"hopeless: {journey.PlansSinceCloser} plans without getting closer to ({target.X}, {target.Y})";
                }
                else if (journey.PlansSinceCloser >= StuckPlans)
                {
                    stuckReason = $"{journey.PlansSinceCloser} plans without getting closer to ({target.X}, {target.Y})";
                }
            }
            else
            {
                w.WriteNull("target");
                w.WriteBoolean("walking", false);
                w.WriteBoolean("moving", false);
                w.WriteNumber("plans", 0);
                w.WriteNumber("plansSinceCloser", 0);
                w.WriteBoolean("noWay", false);
                w.WriteNumber("routeLeft", 0);
                w.WriteNumber("planLeft", 0);
                w.WritePropertyName("route");
                w.WriteStartArray();
                w.WriteEndArray();
            }

            if (stuckReason == null && resolve is { Becalmed: true } && now - resolve.BecalmedTick < 180000)
            {
                stuckReason = $"becalmed {resolve.BecalmedOff} tiles short at ({resolve.BecalmedAt.X}, {resolve.BecalmedAt.Y})";
            }

            var since = resolve == null ? 0 : Math.Max(0, now - Math.Max(resolve.SinceTick, resolve.StirredTick));

            w.WriteBoolean("stuck", stuckReason != null);
            BotJson.StringOrNull(w, "stuckReason", stuckReason);

            if (stuckReason != null && alive)
            {
                roll.Stuck.Add((bot.Name, stuckReason, since, bot.X, bot.Y, target));
            }

            w.WriteNumber("mood", Math.Round(bot.Mood, 3));
            w.WriteNumber("purse", bot.Backpack?.GetAmount(typeof(Gold)) ?? 0);
            w.WriteNumber("bank", Banker.GetBalance(bot));
            // Units over the allowance in capped kinds, and how many things the bank box holds of its 125 (06.10.2026, build next).
            w.WriteNumber("over", BotUnload.OverUnits(bot));
            w.WriteNumber("boxItems", bot.FindBankNoCreate()?.TotalItems ?? 0);
            // Whether that box is at its count, so it takes only what joins a stack it holds (06.10.2026, box16). See BotSurplus.
            w.WriteBoolean("boxFull", BotSurplus.BoxFull(bot));
            w.WriteString("rank", bot.BotRank ?? "");
            w.WriteString("level", Level(bot.BotRank));
            w.WriteNumber("progress", Math.Round(bot.Progress, 3));
            w.WriteNumber("kills", bot.Kills);

            // Its nature, its story and its friends and enemies (07.10.2026, traits17). See BotCharacterWeb.
            BotCharacterWeb.Write(w, bot);
            BotDay.WriteBot(w, bot);

            WriteSkills(w, bot);

            var said = BotEvents.LastSaid(bot.Name);

            BotJson.StringOrNull(w, "lastSaid", said?.Text);
            BotJson.StringOrNull(w, "lastSaidAt", said?.At.ToString("HH:mm:ss"));
            BotJson.StringOrNull(w, "lastSaidChannel", said?.Channel);

            var path = BotEvents.LastPath(bot.Name);

            if (path == null)
            {
                w.WriteNull("lastPath");
            }
            else
            {
                w.WritePropertyName("lastPath");
                w.WriteRawValue(LastPathJson(path), true);
            }

            if (resolve != null)
            {
                w.WritePropertyName("memory");
                w.WriteStartArray();

                foreach (var line in resolve.Recent())
                {
                    w.WriteStringValue(line);
                }

                w.WriteEndArray();
            }

            w.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    private static string LastPathJson(BotEvent e) =>
        BotJson.Object(
            w =>
            {
                w.WriteString("at", e.At.ToString("HH:mm:ss"));
                w.WriteString("text", e.Text ?? "");

                if (e.Data != null)
                {
                    w.WritePropertyName("data");
                    w.WriteRawValue(e.Data, true);
                }
            }
        );

    private static void WriteRoute(Utf8JsonWriter w, BotJourney journey, BotMobile bot)
    {
        w.WritePropertyName("route");
        w.WriteStartArray();

        var errand = journey.Current;
        var route = errand?.Route;

        if (route != null && route.Count > 0)
        {
            var from = Math.Clamp(errand.Leg, 0, route.Count);
            var left = route.Count - from;
            var step = Math.Max(1, (left + MostRoutePoints - 1) / MostRoutePoints);

            w.WriteStartArray();
            w.WriteNumberValue(bot.X);
            w.WriteNumberValue(bot.Y);
            w.WriteEndArray();

            for (var i = from; i < route.Count; i += step)
            {
                w.WriteStartArray();
                w.WriteNumberValue(route[i].X);
                w.WriteNumberValue(route[i].Y);
                w.WriteEndArray();
            }

            if (step > 1)
            {
                var last = route[^1];

                w.WriteStartArray();
                w.WriteNumberValue(last.X);
                w.WriteNumberValue(last.Y);
                w.WriteEndArray();
            }
        }
        else
        {
            // No graph route: the precise plan, which is at most a leg long.
            var plan = journey.Plan;
            var from = Math.Max(0, plan.Count - journey.Remaining);

            if (plan.Count > from)
            {
                w.WriteStartArray();
                w.WriteNumberValue(bot.X);
                w.WriteNumberValue(bot.Y);
                w.WriteEndArray();

                for (var i = from; i < plan.Count; i++)
                {
                    w.WriteStartArray();
                    w.WriteNumberValue(plan[i].X);
                    w.WriteNumberValue(plan[i].Y);
                    w.WriteEndArray();
                }
            }
        }

        w.WriteEndArray();
    }

    private static readonly List<(string Name, double Value)> _skillScratch = new(64);

    private static void WriteSkills(Utf8JsonWriter w, BotMobile bot)
    {
        _skillScratch.Clear();

        var skills = bot.Skills;

        if (skills != null)
        {
            for (var i = 0; i < skills.Length; i++)
            {
                var skill = skills[i];

                if (skill != null && skill.Value >= 10.0)
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
            w.WriteStartObject();
            w.WriteString("name", _skillScratch[i].Name);
            w.WriteNumber("value", Math.Round(_skillScratch[i].Value, 1));
            w.WriteEndObject();
        }

        w.WriteEndArray();
    }

    private static string Level(string rank)
    {
        if (string.IsNullOrEmpty(rank))
        {
            return "Novice";
        }

        var cut = rank.IndexOf(' ');

        return cut > 0 ? rank[..cut] : rank;
    }

    private static void WriteWork(Utf8JsonWriter w)
    {
        var ended = BotWill.Finished + BotWill.Failed + BotWill.Dropped;

        w.WritePropertyName("work");
        w.WriteStartObject();
        w.WriteNumber("taken", BotWill.Taken);
        w.WriteNumber("finished", BotWill.Finished);
        w.WriteNumber("failed", BotWill.Failed);
        w.WriteNumber("dropped", BotWill.Dropped);
        w.WriteNumber("died", BotWill.Deaths);
        w.WriteNumber("share", ended <= 0 ? 0.0 : Math.Round((double)BotWill.Finished / ended, 4));
        // Both shares side by side (Patrick, 01.10.2026: "both on the dashboard"): the 95 % is read on the share without deaths;
        // the share over every ending, deaths in the middle of the work included, stands beside it as the other measure.
        var endedAll = ended + BotWill.Deaths;
        w.WriteNumber("shareWithDeaths", endedAll <= 0 ? 0.0 : Math.Round((double)BotWill.Finished / endedAll, 4));
        w.WriteNumber("target", 0.95);
        w.WritePropertyName("byKind");
        w.WriteStartArray();

        foreach (var (kind, taken, finished, failed, dropped, died) in BotWill.Tallies())
        {
            // Deaths are endings, as the Resolve line counts them (build 334): without them the page read flee at 100 % while
            // half of its endings were deaths.
            var ends = finished + failed + dropped + died;

            w.WriteStartObject();
            w.WriteString("kind", kind);
            w.WriteNumber("taken", taken);
            w.WriteNumber("finished", finished);
            w.WriteNumber("failed", failed);
            w.WriteNumber("dropped", dropped);
            w.WriteNumber("died", died);
            w.WriteNumber("share", ends <= 0 ? 0.0 : Math.Round((double)finished / ends, 4));
            w.WriteEndObject();
        }

        w.WriteEndArray();
        w.WriteEndObject();
    }

    private static void WritePaths(Utf8JsonWriter w, Map map)
    {
        w.WritePropertyName("paths");
        w.WriteStartObject();
        w.WriteNumber("searches", BotPath.Searches);
        w.WriteNumber("reached", BotPath.Reached);
        w.WriteNumber("partial", BotPath.PartialRuns);
        w.WriteNumber("refused", BotPath.SealedRuns);
        // The walkers' arrivals, beside counting as arriving (build 386, see BotWalk.WalkerSearches); the raw search share beside it.
        w.WriteNumber("share", BotWalk.WalkerSearches <= 0 ? 0.0 : Math.Round((double)BotWalk.WalkerReached / BotWalk.WalkerSearches, 4));
        w.WriteNumber("walkerSearches", BotWalk.WalkerSearches);
        w.WriteNumber("walkerReached", BotWalk.WalkerReached);
        w.WriteNumber("searchShare", BotPath.Searches <= 0 ? 0.0 : Math.Round((double)BotPath.Reached / BotPath.Searches, 4));
        w.WriteNumber("besides", BotWalk.Besides);
        w.WriteNumber("target", 0.99);
        w.WriteNumber("partialNear", BotPath.PartialNear);
        w.WriteNumber("partialStill", BotPath.PartialStill);
        w.WriteNumber("msEach", BotPath.Searches <= 0 ? 0.0 : Math.Round(BotPath.TotalMs / BotPath.Searches, 3));
        w.WriteNumber("worstMs", Math.Round(BotPath.WorstMs, 2));

        w.WritePropertyName("byPurpose");
        w.WriteStartObject();
        Purpose(w, "fixed", BotWalk.FixedSearches, BotWalk.FixedReached);
        Purpose(w, "chase", BotWalk.ChaseSearches, BotWalk.ChaseReached);
        Purpose(w, "flight", BotWalk.FlightSearches, BotWalk.FlightReached);
        Purpose(w, "station", BotWalk.StationSearches, BotWalk.StationReached);
        w.WriteEndObject();

        w.WritePropertyName("walk");
        w.WriteStartObject();
        w.WriteNumber("steps", BotWalk.Steps);
        w.WriteNumber("refusedByEngine", BotWalk.Refusals);
        w.WriteNumber("doors", BotWalk.Doors);
        w.WriteNumber("gaveUp", BotWalk.GaveUp);
        w.WriteNumber("droppedDestinations", BotWalk.Dropped);
        w.WriteNumber("legs", BotWalk.Legs);
        w.WriteNumber("legsFailed", BotWalk.LegsFailed);
        w.WriteEndObject();

        w.WritePropertyName("routes");
        w.WriteStartObject();
        w.WriteNumber("asked", NavigationService.Routes);
        w.WriteNumber("routed", NavigationService.Status(NavStatus.Ok));
        w.WriteNumber("direct", NavigationService.Status(NavStatus.Direct));
        w.WriteNumber("unreachable", NavigationService.Status(NavStatus.Unreachable));
        w.WriteNumber("notDrawn", NavigationService.Status(NavStatus.Pending));
        w.WriteNumber("overBudget", NavigationService.Status(NavStatus.BudgetExceeded));
        w.WriteNumber("unplaced", NavigationService.Status(NavStatus.Unplaced));
        w.WriteNumber("msEach", NavigationService.Routes <= 0 ? 0.0 : Math.Round(NavigationService.RouteMs / NavigationService.Routes, 3));
        w.WriteNumber("worstMs", Math.Round(NavigationService.WorstRouteMs, 2));
        w.WriteNumber("longTier", NavigationService.LongRoutes);
        w.WriteNumber("overGraph", BotJourney.Routed);
        w.WriteNumber("overChart", BotJourney.Charted);
        w.WriteNumber("noWay", BotJourney.NoWay);

        // The route memory (29.09.2026): served against planned, what it saved, and what it keeps and forgets. See NavRouteMemory.
        var memory = NavigationService.Memory;
        var answerable = NavigationService.Remembered + NavigationService.EligiblePlans;

        w.WriteNumber("fromMemory", NavigationService.Remembered);
        w.WriteNumber("planned", NavigationService.Plans);
        w.WriteNumber("memoryShare", answerable <= 0 ? 0.0 : Math.Round((double)NavigationService.Remembered / answerable, 4));
        w.WriteNumber("memoryMs", Math.Round(NavigationService.RememberedMs, 1));
        w.WriteNumber("plannedMs", Math.Round(NavigationService.PlansMs, 1));
        w.WriteNumber("savedMs", Math.Round(NavigationService.SavedMs, 1));
        w.WriteNumber("remembered", memory.Count);
        w.WriteNumber("rememberedNodes", memory.Nodes);
        w.WriteNumber("joinedMiddle", memory.Middles);
        w.WriteNumber("detoured", memory.Detoured);
        w.WriteNumber("throughBlood", memory.Bloodied);
        w.WriteNumber("offeredCheaper", memory.Improved);
        w.WriteNumber("offeredNoCheaper", memory.Kept);
        w.WriteNumber("forgottenGround", memory.Invalidated);
        w.WriteNumber("searchesMemoized", BotPath.Recalled);
        w.WriteEndObject();

        w.WritePropertyName("graph");

        NavGraph graph = null;

        try
        {
            graph = NavigationService.Enabled ? NavigationService.Graph(map) : null;
        }
        catch
        {
            // A map without a graph is a graph of nothing.
        }

        if (graph == null)
        {
            w.WriteNullValue();
        }
        else
        {
            w.WriteStartObject();
            w.WriteBoolean("complete", graph.Complete);
            w.WriteNumber("clusters", graph.ClustersX * graph.ClustersY);
            w.WriteNumber("built", graph.Built);
            w.WriteNumber("nodes", graph.Nodes);
            w.WriteNumber("gates", graph.Gates);
            w.WriteNumber("edges", graph.Edges);
            w.WriteNumber("components", graph.ComponentsValid ? graph.ComponentCount : -1);
            w.WriteEndObject();
        }

        w.WriteEndObject();
    }

    private static void Purpose(Utf8JsonWriter w, string name, long searches, long reached)
    {
        w.WritePropertyName(name);
        w.WriteStartObject();
        w.WriteNumber("searches", searches);
        w.WriteNumber("reached", reached);
        w.WriteNumber("share", searches <= 0 ? 0.0 : Math.Round((double)reached / searches, 4));
        w.WriteEndObject();
    }

    private static void WriteGuilds(Utf8JsonWriter w)
    {
        w.WritePropertyName("guilds");
        w.WriteStartArray();

        foreach (var baseGuild in World.Guilds.Values)
        {
            if (baseGuild is not Guild { Disbanded: false } guild)
            {
                continue;
            }

            var members = guild.Members;
            var alive = 0;
            var ours = 0;

            for (var i = 0; i < members.Count; i++)
            {
                if (members[i] is BotMobile { Deleted: false } bot)
                {
                    ours++;

                    if (bot.Alive && bot.Map != Map.Internal)
                    {
                        alive++;
                    }
                }
            }

            if (ours == 0)
            {
                continue;
            }

            var seat = BotSeat.Of(guild.Name);

            w.WriteStartObject();
            w.WriteString("name", guild.Name);
            w.WriteString("abbr", guild.Abbreviation ?? "");
            w.WriteNumber("members", ours);
            w.WriteNumber("alive", alive);
            BotJson.StringOrNull(w, "leader", guild.Leader?.Name);

            // <b>"hall" is the hall, from 29.09.2026, evening, and "seat" is the seat.</b> Until then "hall" carried the seat,
            // which was the hall's place for a guild with one and the bare point it was given for one without — and the page
            // drew a house at it. With guild houses the seat is a building in a town and the hall stands in the fields, and
            // the page draws both. See BotGuildHouses.
            var hall = BotEstate.Hall(guild);

            if (hall is { Deleted: false })
            {
                BotJson.PointObject(w, "hall", hall.Location);
            }
            else
            {
                w.WriteNull("hall");
            }

            if (seat == Point3D.Zero)
            {
                w.WriteNull("seat");
            }
            else
            {
                BotJson.PointObject(w, "seat", seat);
            }

            if (BotGuildHouses.Of(guild.Name) is { } house)
            {
                w.WritePropertyName("house");
                w.WriteStartObject();
                w.WriteString("town", house.Town);
                w.WriteNumber("x", house.Heart.X);
                w.WriteNumber("y", house.Heart.Y);
                w.WriteNumber("doorX", house.Key.X);
                w.WriteNumber("doorY", house.Key.Y);
                w.WriteBoolean("britain", house.Britain);
                w.WriteNumber("paid", house.Paid);
                w.WriteString("since", house.Since.ToString("u"));
                w.WriteEndObject();
            }
            else
            {
                w.WriteNull("house");
            }

            // What the guild could pay with (chest and members' spare) and what it is saving for. See BotDues.
            var goal = BotDues.Goal(guild, out var saving);

            w.WriteNumber("fund", BotEstate.Fund(guild));
            w.WriteNumber("goal", goal);
            BotJson.StringOrNull(w, "saving", saving);
            w.WriteNumber("chest", BotChest.Holds(guild.Name));

            // How much the guild is worth belonging to, and how long it has been unwanted (06.10.2026). See BotGuilds.Appeal
            // and BotGuilds.Wane: at "unwantedMin" of BotGuilds.CloseAfterMs a guild of fewer than five closes.
            w.WriteNumber("appeal", Math.Round(BotGuilds.Appeal(guild), 1));
            w.WriteNumber("unwantedMin", BotGuilds.LowMinutes(guild));

            // Its politics: the elected head, what the chest saves for by vote, its posts in the towns, tribute (07.10.2026).
            BotPolitics.Write(w, guild);

            // Its officers, its store and their counts (07.10.2026, officers20). See BotOfficers.
            BotOfficers.Write(w, guild);
            w.WritePropertyName("wars");
            w.WriteStartArray();

            foreach (var war in BotWar.Standing)
            {
                var other = war.Against(guild.Name);

                if (other != null)
                {
                    w.WriteStringValue(other);
                }
            }

            w.WriteEndArray();

            // The squares the guild holds, as the tile at each square's corner, whether it was paid for and whether it is town ground; the side is
            // "quad" at the root. Patrick's order of 29.09.2026: the guilds' zones on the map in the browser.
            w.WritePropertyName("squares");
            w.WriteStartArray();

            foreach (var (holder, mapId, qx, qy, bought) in BotClaim.Holdings())
            {
                if (holder != guild.Name || mapId != (BotPopulation.Home?.MapID ?? -1))
                {
                    continue;
                }

                w.WriteStartArray();
                w.WriteNumberValue(qx * BotQuad.Side);
                w.WriteNumberValue(qy * BotQuad.Side);
                w.WriteNumberValue(bought ? 1 : 0);

                // 1 when the square reaches ground where a town's guards act: after the sweep only a hall's yard (07.10.2026, BotTownGround).
                w.WriteNumberValue(BotTownGround.Guarded(Map.Maps[mapId], (mapId, qx, qy), out _, out _) ? 1 : 0);
                w.WriteEndArray();
            }

            w.WriteEndArray();
            w.WriteEndObject();
        }

        w.WriteEndArray();
    }

    private static void WriteWars(Utf8JsonWriter w)
    {
        w.WritePropertyName("wars");
        w.WriteStartArray();

        foreach (var war in BotWar.Standing)
        {
            w.WriteStartObject();
            w.WriteString("a", war.A);
            w.WriteString("b", war.B);
            BotJson.StringOrNull(w, "declarer", war.Declarer);
            BotJson.StringOrNull(w, "why", war.Why);
            w.WriteNumber("minutes", war.Minutes);
            w.WriteNumber("killsA", war.KillsA);
            w.WriteNumber("killsB", war.KillsB);
            w.WriteNumber("plunderA", war.LootA);
            w.WriteNumber("plunderB", war.LootB);
            w.WriteBoolean("big", war.Big);
            w.WriteString("rule", $"{BotWar.Kills} dead or {BotWar.Loot}gp");
            w.WriteEndObject();
        }

        w.WriteEndArray();
    }

    /// <summary>The cave mouths and other teleporters read as gates: where to step, where it leads, what it is called. See BotGates (29.09.2026).</summary>
    private static void WriteGates(Utf8JsonWriter w)
    {
        w.WritePropertyName("gates");
        w.WriteStartArray();

        if (BotGates.Surveyed)
        {
            var gates = BotGates.All;

            for (var i = 0; i < gates.Count; i++)
            {
                var gate = gates[i];

                w.WriteStartObject();
                w.WriteNumber("x", gate.From.X);
                w.WriteNumber("y", gate.From.Y);
                w.WriteNumber("tx", gate.To.X);
                w.WriteNumber("ty", gate.To.Y);
                w.WriteString("name", gate.Name);
                w.WriteBoolean("landed", gate.FromLand >= 0 && gate.ToLand >= 0);
                w.WriteNumber("used", gate.Used);
                w.WriteEndObject();
            }
        }

        w.WriteEndArray();
    }

    /// <summary>
    /// The houses of the population's own (<see cref="BotAbode"/>): the owner with its class and guild, the house's kind and
    /// sign, where it stands, and whether the owner is inside it now. For the dashboard's "houses" layer.
    /// </summary>
    private static void WriteAbodes(Utf8JsonWriter w)
    {
        w.WritePropertyName("abodes");
        w.WriteStartArray();

        foreach (var house in BotAbode.Houses)
        {
            if (house == null || house.Deleted || house.Map == null)
            {
                continue;
            }

            var owner = house.Owner;
            var sign = house.Sign?.Name;
            var name = owner?.Name ?? (sign != null && sign.EndsWith("'s house", StringComparison.Ordinal) ? sign[..^8] : "?");

            w.WriteStartObject();
            w.WriteString("owner", name);
            w.WriteString("kind", BotHallKind.Of(house).ToString());
            w.WriteString("sign", sign ?? BotAbode.SignFor(name));
            w.WriteNumber("x", house.X);
            w.WriteNumber("y", house.Y);
            w.WriteString("map", house.Map.Name);

            if (owner is BotMobile bot)
            {
                w.WriteString("class", bot.Class?.Name ?? "?");
                BotJson.StringOrNull(w, "guild", bot.Guild?.Name);
                BotJson.StringOrNull(w, "guildAbbr", bot.Guild?.Abbreviation);
                w.WriteBoolean("alive", bot.Alive);
                w.WriteBoolean("home", house.IsInside(bot));
            }

            w.WriteEndObject();
        }

        w.WriteEndArray();
    }

    /// <summary>
    /// Ground closed to the population: the standing bars and the quarantines in force, the killing fields among them,
    /// each with why, the dead and the minutes left. See BotBarred (29.09.2026).
    /// </summary>
    private static void WriteBarred(Utf8JsonWriter w)
    {
        w.WritePropertyName("barred");
        w.WriteStartArray();

        if (BotBarred.Running)
        {
            var bars = BotBarred.Bars;

            for (var i = 0; i < bars.Count; i++)
            {
                var bar = bars[i];
                w.WriteStartObject();
                w.WriteString("kind", "bar");
                w.WriteNumber("x1", bar.X1);
                w.WriteNumber("y1", bar.Y1);
                w.WriteNumber("x2", bar.X2);
                w.WriteNumber("y2", bar.Y2);
                w.WriteString("why", bar.Why);
                w.WriteNumber("turned", bar.Turned);
                w.WriteEndObject();
            }

            foreach (var (x1, y1, x2, y2, why, deaths, left, turned) in BotBarred.Standing())
            {
                w.WriteStartObject();
                w.WriteString("kind", why != null && why.StartsWith("a killing field", StringComparison.Ordinal) ? "field" : "quarantine");
                w.WriteNumber("x1", x1);
                w.WriteNumber("y1", y1);
                w.WriteNumber("x2", x2);
                w.WriteNumber("y2", y2);
                w.WriteString("why", why);
                w.WriteNumber("deaths", deaths);
                w.WriteNumber("left", left);
                w.WriteNumber("turned", turned);
                w.WriteEndObject();
            }
        }

        w.WriteEndArray();
    }

    /// <summary>The towns, their squares and whether they can be walked to from home. See BotTowns (29.09.2026).</summary>
    private static void WriteTowns(Utf8JsonWriter w)
    {
        w.WritePropertyName("towns");
        w.WriteStartArray();

        if (BotTowns.Surveyed)
        {
            var towns = BotTowns.All;

            for (var i = 0; i < towns.Count; i++)
            {
                var town = towns[i];

                w.WriteStartObject();
                w.WriteString("name", town.Name);
                w.WriteNumber("x", town.Square.X);
                w.WriteNumber("y", town.Square.Y);
                w.WriteBoolean("reachable", town.FromHome);
                w.WriteBoolean("home", town.Home);
                w.WriteNumber("guilds", town.Seated);

                // Guild houses in the town and the empty buildings still free there. See BotGuildHouses (29.09.2026).
                w.WriteNumber("houses", BotGuildHouses.Houses(town.Name));
                w.WriteNumber("free", BotGuildHouses.Free(town.Name));
                w.WriteNumber("arrivals", town.Arrivals);
                w.WriteNumber("residents", BotResidence.Residents(town));

                // The town's charters, one per craft held or sold this period (07.10.2026, charters20). See BotCharters.Write.
                BotCharters.Write(w, town.Name);
                // Its steward and the last decrees (07.10.2026, steward20). See BotStewardship.Write.
                BotStewardship.Write(w, town);
                w.WriteEndObject();
            }
        }

        w.WriteEndArray();
    }

    private static void WriteSquads(Utf8JsonWriter w)
    {
        w.WritePropertyName("squads");
        w.WriteStartArray();

        var squads = BotSquads.All;

        for (var i = 0; i < squads.Count; i++)
        {
            var squad = squads[i];

            if (squad is not { Disbanded: false })
            {
                continue;
            }

            var leader = squad.Leader?.Self;

            w.WriteStartObject();
            w.WriteNumber("id", squad.Id);
            BotJson.StringOrNull(w, "leader", leader?.Name);
            w.WritePropertyName("members");
            w.WriteStartArray();

            var members = squad.Members;

            for (var j = 0; j < members.Count; j++)
            {
                var name = members[j]?.Self?.Name;

                if (name != null)
                {
                    w.WriteStringValue(name);
                }
            }

            w.WriteEndArray();
            w.WriteString("stance", squad.Stance.ToString());
            w.WriteBoolean("warring", squad.Warring);
            w.WriteNumber("x", leader?.X ?? 0);
            w.WriteNumber("y", leader?.Y ?? 0);
            w.WriteEndObject();
        }

        w.WriteEndArray();
    }

    private static void WriteAlarms(Utf8JsonWriter w)
    {
        w.WritePropertyName("alarms");
        w.WriteStartArray();

        var alarms = BotEvents.Alarms();
        var from = Math.Max(0, alarms.Count - 20);

        for (var i = from; i < alarms.Count; i++)
        {
            var a = alarms[i];

            w.WriteStartObject();
            w.WriteString("at", a.At.ToString("yyyy-MM-ddTHH:mm:ss"));
            w.WriteString("state", a.State);
            w.WriteString("kind", a.Kind);
            w.WriteString("say", a.Say ?? "");
            w.WriteNumber("n", a.N);
            w.WriteNumber("of", a.Of);
            w.WriteString("window", a.Window ?? "");
            w.WriteEndObject();
        }

        w.WriteEndArray();
    }

    private static void WriteMinds(Utf8JsonWriter w)
    {
        w.WritePropertyName("minds");

        BotMindsReport report = null;

        try
        {
            report = BotWebHooks.Minds?.Invoke();
        }
        catch
        {
            // The minds assembly answers for itself; a throw there is not the page's fault.
        }

        if (report == null)
        {
            w.WriteNullValue();

            return;
        }

        w.WriteStartObject();
        w.WriteString("model", report.Model);
        w.WriteString("endpoint", report.Endpoint);
        w.WriteNumber("awake", report.Awake);
        w.WriteBoolean("reachable", report.Reachable);
        w.WriteNumber("choices", report.Answered);
        w.WriteNumber("asked", report.Asked);
        w.WriteNumber("refused", report.Refused);
        w.WriteNumber("lastMs", report.LastMs);
        w.WriteEndObject();
    }

    /// <summary>One bot in full, with the events about it. Any thread.</summary>
    public static string Bot(string name)
    {
        string json;

        lock (_lock)
        {
            if (name == null || !_bots.TryGetValue(name, out json))
            {
                return null;
            }
        }

        var recent = BotEvents.Latest(40, e => string.Equals(e.Bot, name, StringComparison.OrdinalIgnoreCase));
        var text = new StringBuilder(json.Length + recent.Count * 200 + 64);

        // The cached object without its closing brace, then the extra fields.
        text.Append(json, 0, json.Length - 1);
        text.Append(",\"recent\":[");

        for (var i = 0; i < recent.Count; i++)
        {
            if (i > 0)
            {
                text.Append(',');
            }

            text.Append(recent[i].Json);
        }

        text.Append("],\"ledger\":[]}");

        return text.ToString();
    }

    private static string BuildPaths()
    {
        var records = BotEvents.Paths();
        Dictionary<string, (int Count, DateTime Last)> byBot = new(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, int> byReason = new(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, int> byOutcome = new(StringComparer.Ordinal);
        Dictionary<(int X, int Y), int> hot = [];

        foreach (var r in records)
        {
            if (r.Bot != null)
            {
                byBot[r.Bot] = byBot.TryGetValue(r.Bot, out var b) ? (b.Count + 1, r.At) : (1, r.At);
            }

            var reason = Family(r.Reason);

            byReason[reason] = byReason.TryGetValue(reason, out var n) ? n + 1 : 1;
            byOutcome[r.Outcome ?? "?"] = byOutcome.TryGetValue(r.Outcome ?? "?", out var o) ? o + 1 : 1;

            var cell = (r.To.X / 32 * 32 + 16, r.To.Y / 32 * 32 + 16);

            hot[cell] = hot.TryGetValue(cell, out var h) ? h + 1 : 1;
        }

        List<KeyValuePair<string, (int Count, DateTime Last)>> bots = [.. byBot];
        List<KeyValuePair<string, int>> reasons = [.. byReason];
        List<KeyValuePair<(int X, int Y), int>> cells = [.. hot];

        bots.Sort((a, b) => b.Value.Count.CompareTo(a.Value.Count));
        reasons.Sort((a, b) => b.Value.CompareTo(a.Value));
        cells.Sort((a, b) => b.Value.CompareTo(a.Value));

        return BotJson.Object(
            w =>
            {
                w.WriteNumber("kept", records.Count);
                w.WritePropertyName("recent");
                w.WriteStartArray();

                for (var i = Math.Max(0, records.Count - 300); i < records.Count; i++)
                {
                    var r = records[i];

                    w.WriteStartObject();
                    w.WriteString("at", r.At.ToString("HH:mm:ss"));
                    w.WriteString("bot", r.Bot ?? "?");
                    BotJson.StringOrNull(w, "work", r.Work);
                    w.WriteString("reason", r.Reason ?? "");
                    w.WriteString("outcome", r.Outcome ?? "?");
                    BotJson.Point(w, "from", r.From);
                    BotJson.Point(w, "to", r.To);
                    w.WriteNumber("span", r.Span);
                    w.WriteNumber("ms", Math.Round(r.Ms, 2));
                    w.WriteNumber("tiles", r.Tiles);
                    w.WriteNumber("plans", r.Plans);
                    w.WriteBoolean("chase", r.Chase);
                    w.WriteBoolean("flight", r.Flight);
                    w.WriteEndObject();
                }

                w.WriteEndArray();

                w.WritePropertyName("byBot");
                w.WriteStartArray();

                for (var i = 0; i < bots.Count && i < 60; i++)
                {
                    w.WriteStartObject();
                    w.WriteString("bot", bots[i].Key);
                    w.WriteNumber("count", bots[i].Value.Count);
                    w.WriteString("lastAt", bots[i].Value.Last.ToString("HH:mm:ss"));
                    w.WriteEndObject();
                }

                w.WriteEndArray();

                w.WritePropertyName("byReason");
                w.WriteStartArray();

                for (var i = 0; i < reasons.Count && i < 40; i++)
                {
                    w.WriteStartObject();
                    w.WriteString("reason", reasons[i].Key);
                    w.WriteNumber("count", reasons[i].Value);
                    w.WriteEndObject();
                }

                w.WriteEndArray();

                w.WritePropertyName("byOutcome");
                w.WriteStartObject();

                foreach (var (outcome, count) in byOutcome)
                {
                    w.WriteNumber(outcome, count);
                }

                w.WriteEndObject();

                w.WritePropertyName("hot");
                w.WriteStartArray();

                for (var i = 0; i < cells.Count && i < 60; i++)
                {
                    w.WriteStartObject();
                    w.WriteNumber("x", cells[i].Key.X);
                    w.WriteNumber("y", cells[i].Key.Y);
                    w.WriteNumber("count", cells[i].Value);
                    w.WriteEndObject();
                }

                w.WriteEndArray();
            }
        );
    }

    /// <summary>
    /// "away from a dire wolf" and "away from an ettin" are one family: the reason with its object replaced by
    /// what kind of object it was — a place, somebody named, or a creature.
    /// </summary>
    private static string Family(string reason)
    {
        if (string.IsNullOrEmpty(reason))
        {
            return "?";
        }

        foreach (var head in Heads)
        {
            if (!reason.StartsWith(head, StringComparison.Ordinal))
            {
                continue;
            }

            var rest = reason[head.Length..].TrimStart();

            if (rest.Length == 0)
            {
                return head.TrimEnd();
            }

            if (rest[0] == '(')
            {
                return head + "a place";
            }

            if (rest.StartsWith("a ", StringComparison.Ordinal) || rest.StartsWith("an ", StringComparison.Ordinal) || rest.StartsWith("the ", StringComparison.Ordinal))
            {
                return head + "a creature";
            }

            return char.IsUpper(rest[0]) ? head + "somebody" : head + rest;
        }

        var cut = reason.IndexOf('(');

        return cut > 0 ? reason[..cut].Trim() : reason;
    }

    private static readonly string[] Heads =
    [
        "away from ", "after ", "back to ", "toward ", "following ", "hunting ", "off to ", "to "
    ];

    public static string Health() =>
        BotJson.Object(
            w =>
            {
                w.WriteBoolean("ok", true);
                w.WriteNumber("built", Built);
                w.WriteNumber("lastBuildMs", Math.Round(LastBuildMs, 2));
                w.WriteNumber("worstBuildMs", Math.Round(WorstBuildMs, 2));
                w.WriteNumber("events", BotEvents.Seq);
                w.WriteNumber("streams", BotWebServer.Streams);
                w.WriteNumber("requests", BotWebServer.Requests);
                w.WriteBoolean("mapReady", BotWebMap.Ready);
                w.WriteBoolean("mapFailed", BotWebMap.Failed);
                BotWebLive.Health(w);
            }
        );

    public static string Describe() =>
        $"{Built} snapshots built, {LastBuildMs:F1}ms the last, {WorstBuildMs:F1}ms the worst, {TotalBuildMs:F0}ms in all; {BotWebHistory.Count} minutes of history";
}
