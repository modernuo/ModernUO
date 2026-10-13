using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>One thing that happened to one bot, as the dashboard and the event log see it.</summary>
public sealed class BotEvent
{
    public long Seq { get; init; }

    public DateTime At { get; init; }

    /// <summary>say, work, path, death, war, alarm, market, craft.</summary>
    public string Kind { get; init; }

    /// <summary>For speech: world, trade, guild, local, mood, cry. Null otherwise.</summary>
    public string Channel { get; init; }

    public string Bot { get; init; }

    public string Class { get; init; }

    public string Guild { get; init; }

    public int X { get; init; }

    public int Y { get; init; }

    public string Text { get; init; }

    /// <summary>A JSON object, already serialised, or null.</summary>
    public string Data { get; init; }

    /// <summary>The whole event as one JSON object, serialised once.</summary>
    public string Json { get; init; }
}

/// <summary>One search that did not reach its goal, kept apart from the general stream so the paths page can add them up.</summary>
public readonly record struct BotPathRecord(
    DateTime At,
    string Bot,
    string Work,
    string Reason,
    string Outcome,
    Point3D From,
    Point3D To,
    int Span,
    double Ms,
    long Tiles,
    int Plans,
    bool Chase,
    bool Flight
);

/// <summary>One line of the alarm channel, kept for the dashboard's header.</summary>
public readonly record struct BotAlarmRecord(DateTime At, string State, string Kind, string Say, long N, long Of, string Window);

/// <summary>
/// The population's event stream: everything worth telling a person about, in order, with a sequence number.
///
/// <para>
/// <b>Three readers, one writer.</b> The game loop posts; the web server reads by sequence number, the SSE
/// streams are fed a copy, and <c>logs/bot-events.ndjson</c> keeps the record across restarts. Nothing here
/// touches the world, so the readers can run on any thread under the one lock.
/// </para>
///
/// <para>
/// <b>Why this and not the session log.</b> The log says everything, in prose, for grep. A dashboard needs
/// the same facts as fields — which bot, where, what kind — without parsing sentences that change when the
/// sentence is improved. The log stays the record of reasoning; this is the record of events.
/// </para>
/// </summary>
public static class BotEvents
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotEvents));

    /// <summary>How many events are kept in memory for the page to page through.</summary>
    public static int Keep { get; set; } = 3000;

    /// <summary>How many failed searches are kept for the paths page.</summary>
    public static int KeepPaths { get; set; } = 2000;

    /// <summary>How many alarm lines are kept for the header.</summary>
    public static int KeepAlarms { get; set; } = 50;

    public static bool Running { get; private set; }

    public static long Written { get; private set; }

    public static long Swallowed { get; private set; }

    private static long _seq;

    private static readonly object _lock = new();

    private static readonly Queue<BotEvent> _ring = new();

    private static readonly Queue<BotPathRecord> _paths = new();

    private static readonly Queue<BotAlarmRecord> _alarms = new();

    private static readonly Dictionary<string, BotEvent> _lastPath = new(StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<string, BotEvent> _lastSaid = new(StringComparer.OrdinalIgnoreCase);

    private static readonly List<Action<BotEvent>> _sinks = [];

    private static StreamWriter _file;

    public static string Path { get; private set; }

    /// <summary>
    /// The live window's ear (02.10.2026): told every event on the loop, after the streams, and null while nobody watches a
    /// bot. Kept apart from <see cref="Subscribe"/> so that a watched bot is not counted as a live stream. See BotWebLive.
    /// </summary>
    public static Action<BotEvent> Live { get; set; }

    /// <summary>
    /// The stories' ear (PLAN-life A3, 13.10.2026): told every event on the loop, after the live window, so that a bot's book and
    /// the island's Chronicle are written from the events as they happen rather than read back off the file. Null while the
    /// stories are off. Kept apart from <see cref="Subscribe"/> for the reason <see cref="Live"/> is: it is not a stream a person
    /// is reading. See BotStory.
    /// </summary>
    public static Action<BotEvent> Heard { get; set; }

    /// <summary>The newest sequence number handed out.</summary>
    public static long Seq
    {
        get
        {
            lock (_lock)
            {
                return _seq;
            }
        }
    }

    public static void Open()
    {
        if (Running)
        {
            return;
        }

        try
        {
            var folder = System.IO.Path.GetFullPath(System.IO.Path.Combine(Core.BaseDirectory, "..", "logs"));

            Directory.CreateDirectory(folder);

            Path = System.IO.Path.Combine(folder, "bot-events.ndjson");

            _file = new StreamWriter(new FileStream(Path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite), new UTF8Encoding(false))
            {
                AutoFlush = true
            };

            Running = true;

            logger.Information("The event stream is open at {Path}: one line of JSON per event", Path);
        }
        catch (Exception e)
        {
            logger.Warning("The event stream could not be opened: {Message}", e.Message);
        }
    }

    public static void Close()
    {
        lock (_lock)
        {
            try
            {
                _file?.Dispose();
            }
            catch
            {
                // Closing is best effort.
            }

            _file = null;
            Running = false;
        }
    }

    /// <summary>Posts an event about a bot. Game loop only.</summary>
    public static BotEvent Post(string kind, Mobile who, string text, string channel = null, string data = null)
    {
        var guild = (who?.Guild as Guilds.Guild)?.Name;
        var klass = (who as BotMobile)?.Class?.Name;
        var at = who?.Location ?? Point3D.Zero;

        return Post(kind, who?.Name, klass, guild, at.X, at.Y, text, channel, data);
    }

    /// <summary>Posts an event about somebody or something named, without a body to read. Game loop only.</summary>
    public static BotEvent Post(string kind, string bot, string klass, string guild, int x, int y, string text, string channel, string data)
    {
        if (kind == null)
        {
            return null;
        }

        BotEvent e;
        Action<BotEvent>[] sinks;

        lock (_lock)
        {
            var seq = ++_seq;
            var now = DateTime.Now;

            e = new BotEvent
            {
                Seq = seq,
                At = now,
                Kind = kind,
                Channel = channel,
                Bot = bot,
                Class = klass,
                Guild = guild,
                X = x,
                Y = y,
                Text = text,
                Data = data,
                Json = Serialise(seq, now, kind, channel, bot, klass, guild, x, y, text, data)
            };

            _ring.Enqueue(e);

            while (_ring.Count > Keep)
            {
                _ring.Dequeue();
            }

            if (bot != null)
            {
                if (kind == "say")
                {
                    _lastSaid[bot] = e;
                }
                else if (kind == "path")
                {
                    _lastPath[bot] = e;
                }
            }

            if (_file != null)
            {
                try
                {
                    _file.WriteLine(e.Json);
                    Written++;
                }
                catch (Exception ex)
                {
                    Swallowed++;

                    if (Swallowed == 1)
                    {
                        logger.Warning("The event stream could not be written: {Message}", ex.Message);
                    }
                }
            }

            sinks = _sinks.Count == 0 ? [] : _sinks.ToArray();
        }

        for (var i = 0; i < sinks.Length; i++)
        {
            try
            {
                sinks[i](e);
            }
            catch
            {
                // A stream that throws is a stream that has gone; it unsubscribes itself.
            }
        }

        if (Live is { } live)
        {
            try
            {
                live(e);
            }
            catch
            {
                // The live window answers for itself; an event is never lost to it.
            }
        }

        if (Heard is { } heard)
        {
            try
            {
                heard(e);
            }
            catch
            {
                // The stories count and log their own faults (BotStory.Fault); a story never stops an event being posted.
            }
        }

        return e;
    }

    /// <summary>A search that did not reach its goal. Kept for the paths page, and posted as an event.</summary>
    public static void PathFailed(
        Mobile bot, string work, string reason, string outcome, Point3D from, Point3D to, double ms, long tiles, int plans,
        bool chase, bool flight
    )
    {
        var span = Math.Max(Math.Abs(to.X - from.X), Math.Abs(to.Y - from.Y));
        var record = new BotPathRecord(DateTime.Now, bot?.Name, work, reason, outcome, from, to, span, ms, tiles, plans, chase, flight);

        lock (_lock)
        {
            _paths.Enqueue(record);

            while (_paths.Count > KeepPaths)
            {
                _paths.Dequeue();
            }
        }

        var data = BotJson.Object(
            w =>
            {
                w.WriteString("outcome", outcome);
                w.WriteString("reason", reason);
                w.WriteString("work", work);
                BotJson.Point(w, "from", from);
                BotJson.Point(w, "to", to);
                w.WriteNumber("span", span);
                w.WriteNumber("ms", Math.Round(ms, 2));
                w.WriteNumber("tiles", tiles);
                w.WriteNumber("plans", plans);
                w.WriteBoolean("chase", chase);
                w.WriteBoolean("flight", flight);
            }
        );

        Post("path", bot, $"{outcome.ToLowerInvariant()} to ({to.X},{to.Y}): {reason}", null, data);
    }

    /// <summary>A line of the alarm channel, mirrored here so the page sees it without reading the file.</summary>
    public static void Alarm(string state, string kind, string say, long n, long of, string window)
    {
        lock (_lock)
        {
            _alarms.Enqueue(new BotAlarmRecord(DateTime.Now, state, kind, say, n, of, window));

            while (_alarms.Count > KeepAlarms)
            {
                _alarms.Dequeue();
            }
        }

        var data = BotJson.Object(
            w =>
            {
                w.WriteString("state", state);
                w.WriteString("kind", kind);
                w.WriteNumber("n", n);
                w.WriteNumber("of", of);
                w.WriteString("window", window);
            }
        );

        Post(kind == "war" ? "war" : "alarm", null, null, null, 0, 0, say, null, data);
    }

    public static List<BotEvent> Since(long seq, int limit)
    {
        List<BotEvent> found = [];

        lock (_lock)
        {
            foreach (var e in _ring)
            {
                if (e.Seq > seq)
                {
                    found.Add(e);
                }
            }
        }

        if (limit > 0 && found.Count > limit)
        {
            found.RemoveRange(0, found.Count - limit);
        }

        return found;
    }

    /// <summary>The newest events that pass the test, oldest first.</summary>
    public static List<BotEvent> Latest(int limit, Func<BotEvent, bool> where)
    {
        List<BotEvent> found = [];

        lock (_lock)
        {
            foreach (var e in _ring)
            {
                if (where == null || where(e))
                {
                    found.Add(e);
                }
            }
        }

        if (limit > 0 && found.Count > limit)
        {
            found.RemoveRange(0, found.Count - limit);
        }

        return found;
    }

    public static List<BotPathRecord> Paths()
    {
        lock (_lock)
        {
            return [.. _paths];
        }
    }

    public static List<BotAlarmRecord> Alarms()
    {
        lock (_lock)
        {
            return [.. _alarms];
        }
    }

    public static BotEvent LastPath(string bot)
    {
        lock (_lock)
        {
            return bot != null && _lastPath.TryGetValue(bot, out var e) ? e : null;
        }
    }

    public static BotEvent LastSaid(string bot)
    {
        lock (_lock)
        {
            return bot != null && _lastSaid.TryGetValue(bot, out var e) ? e : null;
        }
    }

    /// <summary>A live reader, fed every event on the game loop's thread. It must not block.</summary>
    public static void Subscribe(Action<BotEvent> sink)
    {
        lock (_lock)
        {
            _sinks.Add(sink);
        }
    }

    public static void Unsubscribe(Action<BotEvent> sink)
    {
        lock (_lock)
        {
            _sinks.Remove(sink);
        }
    }

    public static int Streams
    {
        get
        {
            lock (_lock)
            {
                return _sinks.Count;
            }
        }
    }

    private static string Serialise(
        long seq, DateTime at, string kind, string channel, string bot, string klass, string guild, int x, int y, string text, string data
    ) =>
        BotJson.Object(
            w =>
            {
                w.WriteNumber("seq", seq);
                w.WriteString("at", at.ToString("HH:mm:ss"));
                w.WriteString("day", at.ToString("yyyy-MM-dd"));
                w.WriteString("kind", kind);

                if (channel != null)
                {
                    w.WriteString("channel", channel);
                }

                if (bot != null)
                {
                    w.WriteString("bot", bot);
                }

                if (klass != null)
                {
                    w.WriteString("class", klass);
                }

                if (guild != null)
                {
                    w.WriteString("guild", guild);
                }

                if (bot != null)
                {
                    w.WriteNumber("x", x);
                    w.WriteNumber("y", y);
                }

                w.WriteString("text", text ?? "");

                if (data != null)
                {
                    w.WritePropertyName("data");
                    w.WriteRawValue(data, skipInputValidation: true);
                }
            }
        );

    public static string Describe() =>
        $"{Seq} events posted, {Written} written to {(Path == null ? "nowhere" : "bot-events.ndjson")}, {Swallowed} swallowed, {Streams} live streams";
}

/// <summary>The one way JSON is written in this folder: a writer over a buffer, and the text out of it.</summary>
public static class BotJson
{
    private static readonly JsonWriterOptions Options = new() { Indented = false, SkipValidation = false };

    public static string Object(Action<Utf8JsonWriter> write)
    {
        var buffer = new ArrayBufferWriter<byte>(256);

        using (var w = new Utf8JsonWriter(buffer, Options))
        {
            w.WriteStartObject();
            write(w);
            w.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    public static string Array(Action<Utf8JsonWriter> write)
    {
        var buffer = new ArrayBufferWriter<byte>(256);

        using (var w = new Utf8JsonWriter(buffer, Options))
        {
            w.WriteStartArray();
            write(w);
            w.WriteEndArray();
        }

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    public static void Point(Utf8JsonWriter w, string name, Point3D p)
    {
        w.WritePropertyName(name);
        w.WriteStartArray();
        w.WriteNumberValue(p.X);
        w.WriteNumberValue(p.Y);
        w.WriteEndArray();
    }

    public static void PointObject(Utf8JsonWriter w, string name, Point3D p)
    {
        w.WritePropertyName(name);
        w.WriteStartObject();
        w.WriteNumber("x", p.X);
        w.WriteNumber("y", p.Y);
        w.WriteNumber("z", p.Z);
        w.WriteEndObject();
    }

    public static void StringOrNull(Utf8JsonWriter w, string name, string value)
    {
        if (value == null)
        {
            w.WriteNull(name);
        }
        else
        {
            w.WriteString(name, value);
        }
    }
}
