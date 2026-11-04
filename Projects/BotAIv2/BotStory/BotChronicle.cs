using System;
using System.Collections.Generic;
using System.Globalization;
using Server.Text;

namespace Server.BotAI.V2;

/// <summary>
/// Which events are the island's history, and the Chronicle's page a day of them.
/// The island's Chronicle (PLAN-life A3): a page a day of its wars, guilds founded, closed and split, champions, relics,
/// elections and depositions, the duke's words between guilds, invasions and fairs — taken from the event stream as it is
/// posted (<see cref="BotEvents"/>, including the alarm channel's war and guild notes) and from the story's own walk (the
/// champion, read off the name's hue). Legends (A8) and festivals (A1) will post into it through <see cref="Record"/>.
///
/// <para>
/// <b>Kept small and in the world save.</b> <see cref="MostPerDay"/> entries a day — a full page lets the lightest go —
/// and <see cref="MostDays"/> days; the page is rebuilt on the loop at most every <see cref="EveryMs"/> when something new
/// came, and handed to the dashboard as one string through a volatile reference. On 12.10.2026 the stream carried
/// 1 war ending, 262 alarm lines (a handful of them guilds), 162 politics lines (about 40 of them a change of head or
/// steward), 931 parley lines (54 of them the duke's word), 151 town-crier lines and 149 championship lines in 24 hours
/// out of 816 000 events: the rules below keep the 30–80 of those that are history and leave the rest to the log.
/// </para>
/// </summary>
public static class BotChronicle
{
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    /// <summary>Days kept. Thirty: a month of the island's history, a few hundred kilobytes.</summary>
    public static int MostDays { get; set; } = 30;

    /// <summary>Entries kept a day; past it the lightest goes. Eighty: a busy day of 12.10 holds about forty that pass the rules.</summary>
    public static int MostPerDay { get; set; } = 80;

    /// <summary>How often the page may be rebuilt when something new came. A minute.</summary>
    public static int EveryMs { get; set; } = 60000;

    /// <summary>A closed day is offered to the model only with at least this many entries; a thin day reads better as a list.</summary>
    public static int VoiceLeast { get; set; } = 4;

    /// <summary>The heaviest entries of a day the model is told, in the order they happened: a prompt has to fit the model's context.</summary>
    public static int VoiceMostFacts { get; set; } = 30;

    /// <summary>
    /// The same sentence again within this many minutes is one thing said twice, and is not written; later it is a new thing.
    /// Thirty. The first cut kept a sentence once a day, and the stream of 11.10.2026 shows why that was wrong: "The Ash declared
    /// war on The Crown over seizing: a claim on its square…" came at 10:03 and again at 11:27, two wars (the first ended in 27
    /// minutes), and the second would have been lost as a repeat; the doubles the rule is for (an infirmary "opened" twice in
    /// nine minutes, the duke's same word heard by both sides) come within minutes.
    /// </summary>
    public static int RepeatMinutes { get; set; } = 30;

    /// <summary>
    /// Entries lighter than this are routine and kept once a day whatever the minutes between: on 12.10.2026 the duke renewed the
    /// same pairs' trade terms 106 times in 60 sentences ("The Hammer and The Ash over an alliance: trade terms for 12 hours" at
    /// 03:42, 06:04, 08:10, 12:50, 15:14 and 17:24), and a quiet day's page was that one line six times. Thirty: the trade terms
    /// (12), the charters (15, 20) and the towns' collections (20) are below it; wars, relics, elections and invasions are not.
    /// </summary>
    public static double RepeatDayBelow { get; set; } = 30.0;

    public static long Recorded { get; private set; }

    public static long Repeats { get; private set; }

    public static long Dropped { get; private set; }

    public static long Built { get; private set; }

    private static readonly Dictionary<string, long> _byKind = new(StringComparer.Ordinal);

    private static readonly Dictionary<string, BotChronicleDay> _days = new(StringComparer.Ordinal);

    private static readonly List<string> _keys = new(32);

    /// <summary>Guilds founded lately, by name: who founded or led them, and when (a tick). For the story's "founded" note.</summary>
    private static readonly Dictionary<string, (string Who, long At)> _founded = new(StringComparer.Ordinal);

    private static readonly Dictionary<string, long> _closed = new(StringComparer.Ordinal);

    private static bool _dirty = true;

    private static long _builtTick;

    private static bool _builtOnce;

    private static volatile string _json = "{\"days\":[]}";

    /// <summary>The page as the dashboard serves it. Any thread: one immutable string, swapped whole.</summary>
    public static string Json => _json;

    public static IReadOnlyDictionary<string, BotChronicleDay> Days => _days;

    // ------------------------------------------------------------------ what goes in

    /// <summary>An event off the stream, kept when it is history. Game loop only (it is called from <see cref="BotEvents.Post"/>).</summary>
    public static void Heard(BotEvent e)
    {
        var text = e.Text;

        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        switch (e.Kind)
        {
            case "war":
                {
                    // Keyed by the sentence with its square: on 09.10.2026 The Blade declared war on The Mill over a square at 705,855
                    // at 08:23 and over one at 675,855 at 08:43, two wars that read the same once the coordinates are out (review B, 3.2).
                    Record("war", text, 80.0, text);

                    break;
                }
            case "alarm":
                {
                    var kind = DataKind(e.Data);

                    if (kind == "guild")
                    {
                        Record("guild", Closing(text), 70.0);
                        Guilds(text);
                    }
                    else if (kind == "exile")
                    {
                        Record("town", text, 30.0);
                    }

                    break;
                }
            case "relic":
                {
                    Record("relic", e.Bot != null ? $"{e.Bot} {text}" : text, 65.0, text);

                    break;
                }
            case "politics":
                {
                    Politics(e, text);

                    break;
                }
            case "parley":
                {
                    // The duke's word between two guilds. On 12.10.2026 106 of its 110 words were trade terms renewed every twelve hours:
                    // kept, but lightest of all, so that a full page lets them go before a war, a peace or an alliance.
                    if (e.Channel == "heard" && text.Contains(" and ", StringComparison.Ordinal) && !text.Contains("town's favour", StringComparison.Ordinal))
                    {
                        Record("diplomacy", $"The duke's word on {Meeting(text)}", text.Contains(": trade terms", StringComparison.Ordinal) ? 12.0 : 40.0);
                    }
                    else if (e.Channel == null && text.Contains(" laid claim to ", StringComparison.Ordinal))
                    {
                        Record("war", text, 50.0, text);
                    }

                    break;
                }
            case "officers":
                {
                    if (e.Bot != null && text.StartsWith("won the guilds' bout", StringComparison.Ordinal))
                    {
                        Record("contest", e.Guild != null ? $"{e.Bot} of {e.Guild} {text}" : $"{e.Bot} {text}", 35.0);
                    }

                    break;
                }
            case "charter":
                {
                    // 63 charters won or taken back on 12.10.2026, every six hours across the towns and crafts: kept light.
                    // The data is a JSON object ({"outcome":"won"}); a bare word here broke the stream's ndjson lines (F2, 04.11.2026).
                    var outcome = DataField(e.Data, "outcome");

                    if (outcome == "won")
                    {
                        Record("town", text, 20.0);
                    }
                    else if (outcome == "revoked")
                    {
                        Record("town", text, 15.0);
                    }

                    break;
                }
            case "event":
                {
                    Crier(e.Channel, text);

                    break;
                }
            case "house":
                {
                    Record("guild", text, 40.0, text);

                    break;
                }
            case "crime":
                {
                    if (e.Bot != null && text.StartsWith("stole ", StringComparison.Ordinal))
                    {
                        Record("crime", $"{e.Bot} {text}", 50.0);
                    }

                    break;
                }
        }
    }

    private static void Politics(BotEvent e, string text)
    {
        if (text.Contains(" re-elected head of ", StringComparison.Ordinal))
        {
            return;
        }

        if (text.Contains(" elected head of ", StringComparison.Ordinal))
        {
            Record("politics", text.Replace(" elected head of ", " was elected head of ", StringComparison.Ordinal), 45.0);
        }
        else if (e.Channel == "steward")
        {
            if (text.Contains(" deposed its steward ", StringComparison.Ordinal))
            {
                Record("politics", text, 45.0);
            }
            else if (text.Contains(" is the steward of ", StringComparison.Ordinal))
            {
                Record("politics", Steward(text), 35.0);
            }
        }
        else if (text.Contains(" deposed ", StringComparison.Ordinal))
        {
            Record("politics", text, 55.0);
        }
        else if (text.Contains(" tribute ", StringComparison.Ordinal))
        {
            Record("politics", text, 40.0);
        }
        else if (text.Contains(" collects ", StringComparison.Ordinal) || text.Contains(" captains ", StringComparison.Ordinal))
        {
            Record("town", text, 20.0);
        }
    }

    /// <summary>
    /// A meeting's line without the meeting's number and the witness: "#104 The Blade and The Crown over trade: trade terms for 12
    /// hours (by the model, before Argus)" → "The Blade and The Crown over trade: trade terms for 12 hours".
    /// </summary>
    private static string Meeting(string text)
    {
        if (text.Length > 1 && text[0] == '#')
        {
            var space = text.IndexOf(' ');

            if (space > 0)
            {
                text = text[(space + 1)..];
            }
        }

        var by = text.LastIndexOf(" (by ", StringComparison.Ordinal);

        return by > 0 && text.EndsWith(')') ? text[..by] : text;
    }

    /// <summary>"X of G is the steward of T now." → "X of G became the steward of T."</summary>
    public static string Steward(string text) =>
        text.Replace(" is the steward of ", " became the steward of ", StringComparison.Ordinal).Replace(" now.", ".", StringComparison.Ordinal);

    /// <summary>The town crier's lines: the ends of invasions, the Great Fairs and their lotteries, found chests and caravans' ends.</summary>
    private static void Crier(string channel, string text)
    {
        switch (channel)
        {
            case "invasion":
                {
                    if (!text.StartsWith("They come!", StringComparison.Ordinal) && !text.StartsWith("Wave ", StringComparison.Ordinal))
                    {
                        Record("town", text, text.StartsWith("To arms", StringComparison.Ordinal) ? 30.0 : 40.0);
                    }

                    break;
                }
            case "fair":
                {
                    if ((text.Contains("Great Fair", StringComparison.Ordinal) && text.Contains(" calls ", StringComparison.Ordinal)) || text.Contains(" lottery: ", StringComparison.Ordinal))
                    {
                        Record("town", text, 30.0);
                    }

                    break;
                }
            case "rumour":
                {
                    if (text.Contains(" found the chest", StringComparison.Ordinal))
                    {
                        Record("road", text, 20.0);
                    }

                    break;
                }
            case "caravan":
                {
                    if (text.StartsWith("The caravan", StringComparison.Ordinal))
                    {
                        Record("road", text, 15.0);
                    }

                    break;
                }
        }
    }

    /// <summary>
    /// A guild's closing without the alarm's reckoning: "The Moon closed: attractiveness 0.2 with 1 members for 123 minutes, nobody
    /// joining; 50gp shared" → "The Moon closed, nobody joining; 50gp shared among its members". The rest is for the log.
    /// </summary>
    private static string Closing(string text)
    {
        var at = text.IndexOf(" closed:", StringComparison.Ordinal);

        if (at <= 0)
        {
            return text;
        }

        var shared = text.LastIndexOf("; ", StringComparison.Ordinal);
        var gold = shared > at && text.EndsWith("gp shared", StringComparison.Ordinal) ? $"; {text[(shared + 2)..]} among its members" : "";

        return $"{text[..at]} closed, nobody joining{gold}";
    }

    /// <summary>Who founded a guild, or led the band that split off into it, read off the alarm channel's own sentence.</summary>
    private static void Guilds(string text)
    {
        var now = Core.TickCount;
        var founded = text.IndexOf(" was founded by ", StringComparison.Ordinal);

        if (founded > 0)
        {
            var guild = text[..founded];
            var bracket = guild.IndexOf(" [", StringComparison.Ordinal);

            if (bracket > 0)
            {
                guild = guild[..bracket];
            }

            var rest = text[(founded + " was founded by ".Length)..];
            string who = null;
            var led = rest.IndexOf(", led by ", StringComparison.Ordinal);

            if (led >= 0)
            {
                who = rest[(led + ", led by ".Length)..].Trim();
            }
            else
            {
                var the = rest.IndexOf(" the ", StringComparison.Ordinal);

                if (the > 0)
                {
                    who = rest[..the];
                }
            }

            if (!string.IsNullOrEmpty(who))
            {
                _founded[guild] = (who, now);
            }

            return;
        }

        var closed = text.IndexOf(" closed:", StringComparison.Ordinal);

        if (closed > 0)
        {
            _closed[text[..closed]] = now;
        }
    }

    /// <summary>Who founded this guild, if the Chronicle saw it founded within the window.</summary>
    public static string FounderOf(string guild, long now, long withinMs) =>
        guild != null && _founded.TryGetValue(guild, out var f) && now - f.At < withinMs ? f.Who : null;

    /// <summary>Whether the Chronicle saw this guild close within the window.</summary>
    public static bool ClosedLately(string guild, long now, long withinMs) =>
        guild != null && _closed.TryGetValue(guild, out var at) && now - at < withinMs;

    /// <summary>
    /// An entry on today's page. Also the seam for what is to come (legends, festivals): any module may record a line of history.
    /// A sentence already on today's page is not written twice; a full page lets its lightest entry go for a heavier one.
    /// <paramref name="key"/>: what tells this entry from another with the same words (the event's sentence with its coordinates).
    /// </summary>
    public static void Record(string kind, string text, double weight, string key = null)
    {
        if (!BotStory.Running || BotStory.Sealed || string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        var now = DateTime.Now;
        var day = Day(now);
        var line = BotStoryWriter.Sentence(BotStory.Plain(text));
        var self = key ?? line;

        for (var i = day.Entries.Count - 1; i >= 0; i--)
        {
            var entry = day.Entries[i];

            if ((weight < RepeatDayBelow || (now - entry.At).TotalMinutes < Math.Max(0, RepeatMinutes))
                && string.Equals(entry.Key ?? entry.Text, self, StringComparison.Ordinal))
            {
                Repeats++;

                return;
            }
        }

        if (day.Entries.Count >= Math.Max(1, MostPerDay))
        {
            var lightest = 0;

            for (var i = 1; i < day.Entries.Count; i++)
            {
                if (day.Entries[i].Weight < day.Entries[lightest].Weight)
                {
                    lightest = i;
                }
            }

            day.Dropped++;
            Dropped++;

            if (day.Entries[lightest].Weight >= weight)
            {
                return;
            }

            day.Entries.RemoveAt(lightest);
            day.Hashed = null;
        }

        day.Entries.Add(new BotChronicleEntry { At = now, Kind = kind, Text = line, Key = key, Weight = weight });
        day.Hashed = null;
        Recorded++;
        _byKind[kind] = _byKind.GetValueOrDefault(kind) + 1;
        _dirty = true;
    }

    private static BotChronicleDay Day(DateTime local)
    {
        var key = local.ToString("yyyy-MM-dd", Invariant);

        if (_days.TryGetValue(key, out var day))
        {
            return day;
        }

        day = new BotChronicleDay { Key = key, Date = local.Date };
        Put(day);

        return day;
    }

    /// <summary>A day into the book, the oldest let go past <see cref="MostDays"/>. Keys sort as dates.</summary>
    private static void Put(BotChronicleDay day)
    {
        _days[day.Key] = day;

        if (!_keys.Contains(day.Key))
        {
            _keys.Add(day.Key);
            _keys.Sort(StringComparer.Ordinal);
        }

        while (_keys.Count > Math.Max(1, MostDays))
        {
            _days.Remove(_keys[0]);
            _keys.RemoveAt(0);
        }

        _dirty = true;
    }

    private static string DataKind(string data) => DataField(data, "kind");

    /// <summary>
    /// A "name":"word" field out of an event's data object, or null when it is not there. The stream's data is written by
    /// <see cref="BotJson"/> in one piece, so its quoting is known and searching for the field's opening is enough.
    /// </summary>
    private static string DataField(string data, string name)
    {
        if (string.IsNullOrEmpty(data))
        {
            return null;
        }

        var at = data.IndexOf($"\"{name}\":\"", StringComparison.Ordinal);

        if (at < 0)
        {
            return null;
        }

        at += name.Length + 4;

        var end = data.IndexOf('"', at);

        return end > at ? data[at..end] : null;
    }

    // ------------------------------------------------------------------ the page

    private static readonly string[] Order = ["war", "guild", "champion", "relic", "contest", "diplomacy", "politics", "town", "crime", "road"];

    /// <summary>On the story's clock: the page rebuilt when something came and the minute has passed. Game loop only.</summary>
    public static void Tick(long now)
    {
        if (!_dirty || _builtOnce && now - _builtTick < Math.Max(1000, EveryMs))
        {
            return;
        }

        _dirty = false;
        _builtOnce = true;
        _builtTick = now;
        Built++;
        _json = Build();
    }

    /// <summary>The whole Chronicle, newest day first.</summary>
    private static string Build() =>
        BotJson.Object(
            w =>
            {
                w.WriteString("at", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", Invariant));
                w.WriteNumber("recorded", Recorded);
                w.WriteNumber("dropped", Dropped);
                w.WritePropertyName("days");
                w.WriteStartArray();

                for (var k = _keys.Count - 1; k >= 0; k--)
                {
                    var day = _days[_keys[k]];

                    w.WriteStartObject();
                    w.WriteString("day", day.Key);
                    w.WriteString("title", Title(day));
                    w.WriteNumber("count", day.Entries.Count);
                    w.WriteString("text", Prose(day));

                    if (day.Voiced is { Text: { } voiced } && day.Voiced.Hash == Hash(day))
                    {
                        w.WriteString("voiced", voiced);
                    }
                    else
                    {
                        w.WriteNull("voiced");
                    }

                    w.WritePropertyName("entries");
                    w.WriteStartArray();

                    for (var i = 0; i < day.Entries.Count; i++)
                    {
                        var entry = day.Entries[i];

                        w.WriteStartObject();
                        w.WriteString("at", entry.At.ToString("HH:mm", Invariant));
                        w.WriteString("kind", entry.Kind);
                        w.WriteString("text", entry.Text);
                        w.WriteNumber("weight", Math.Round(entry.Weight));
                        w.WriteEndObject();
                    }

                    w.WriteEndArray();
                    w.WriteEndObject();
                }

                w.WriteEndArray();
            }
        );

    public static string Title(BotChronicleDay day) => day.Date.ToString("dddd, d MMMM yyyy", Invariant);

    /// <summary>The template's page: what kinds of thing the day held, then each kind's entries in the order they happened.</summary>
    public static string Prose(BotChronicleDay day)
    {
        if (day.Entries.Count == 0)
        {
            return $"{Title(day)}. Nothing the chronicle keeps has happened yet.";
        }

        using var sb = ValueStringBuilder.Create(1024);

        sb.Append($"{Title(day)}.");

        for (var o = 0; o < Order.Length; o++)
        {
            var kind = Order[o];
            var first = true;

            for (var i = 0; i < day.Entries.Count; i++)
            {
                var entry = day.Entries[i];

                if (entry.Kind != kind)
                {
                    continue;
                }

                if (first)
                {
                    sb.Append($"\n\n{Heading(kind)}. ");
                }
                else
                {
                    sb.Append(' ');
                }

                sb.Append($"At {entry.At.ToString("HH:mm", Invariant)}, {entry.Text}");
                first = false;
            }
        }

        return sb.ToString();
    }

    /// <summary>The heading of a kind's paragraph on the page, so that a page grouped by kind does not read as one list out of time order.</summary>
    private static string Heading(string kind) =>
        kind switch
        {
            "war"       => "War",
            "guild"     => "The guilds",
            "champion"  => "The championship",
            "relic"     => "Relics and banners",
            "contest"   => "Bouts between the guilds",
            "diplomacy" => "The duke's words",
            "politics"  => "Offices",
            "town"      => "The towns",
            "crime"     => "Crime",
            _           => "The roads"
        };

    /// <summary>The facts a closed day is retold from: its heaviest entries, in the order they happened, with their times.</summary>
    public static List<string> Facts(BotChronicleDay day)
    {
        var picked = new List<BotChronicleEntry>(day.Entries);

        if (picked.Count > Math.Max(1, VoiceMostFacts))
        {
            picked.Sort(static (a, b) => b.Weight.CompareTo(a.Weight));
            picked.RemoveRange(Math.Max(1, VoiceMostFacts), picked.Count - Math.Max(1, VoiceMostFacts));
            picked.Sort(static (a, b) => a.At.CompareTo(b.At));
        }

        var facts = new List<string>(picked.Count);

        for (var i = 0; i < picked.Count; i++)
        {
            facts.Add($"{picked[i].At.ToString("HH:mm", Invariant)} — {picked[i].Text}");
        }

        return facts;
    }

    /// <summary>The fingerprint of a day's facts, worked out once and kept until the day takes another entry.</summary>
    public static ulong Hash(BotChronicleDay day) => day.Hashed ??= BotStoryWriter.Hash(Facts(day));

    /// <summary>
    /// The closed day the model may retell next: the newest one before today with enough entries and no retelling for its present
    /// entries (a refused one is marked, and not asked again). Null when there is none.
    /// </summary>
    public static BotChronicleDay Unvoiced()
    {
        var today = DateTime.Now.ToString("yyyy-MM-dd", Invariant);

        for (var k = _keys.Count - 1; k >= 0; k--)
        {
            if (string.CompareOrdinal(_keys[k], today) >= 0)
            {
                continue;
            }

            var day = _days[_keys[k]];

            if (day.Entries.Count < Math.Max(1, VoiceLeast))
            {
                continue;
            }

            if (day.Voiced == null || day.Voiced.Hash != Hash(day))
            {
                return day;
            }
        }

        return null;
    }

    /// <summary>The model's page for a day, or a refusal marked so it is not asked again for the same entries.</summary>
    public static void Voice(string key, ulong hash, string text)
    {
        if (!_days.TryGetValue(key, out var day))
        {
            return;
        }

        day.Voiced = new BotStoryProse { Hash = hash, Text = text, At = DateTime.UtcNow };
        _dirty = true;
    }

    public static int Voiced()
    {
        var n = 0;

        foreach (var (_, day) in _days)
        {
            if (day.Voiced is { Text: not null })
            {
                n++;
            }
        }

        return n;
    }

    public static int Today() => _days.TryGetValue(DateTime.Now.ToString("yyyy-MM-dd", Invariant), out var day) ? day.Entries.Count : 0;

    public static string Kinds()
    {
        var kinds = "";

        foreach (var (kind, n) in _byKind)
        {
            kinds += (kinds.Length == 0 ? "" : ", ") + $"{kind} {n}";
        }

        return kinds.Length == 0 ? "none" : kinds;
    }

    // ------------------------------------------------------------------ keeping

    public static void Save(IGenericWriter writer)
    {
        writer.WriteEncodedInt(_keys.Count);

        for (var k = 0; k < _keys.Count; k++)
        {
            var day = _days[_keys[k]];

            writer.Write(day.Key);
            writer.Write(day.Date);
            writer.WriteEncodedInt(day.Entries.Count);

            for (var i = 0; i < day.Entries.Count; i++)
            {
                var entry = day.Entries[i];

                writer.Write(entry.At);
                writer.Write(entry.Kind);
                writer.Write(entry.Text);
                writer.Write(entry.Weight);
            }

            writer.Write(day.Voiced != null);

            if (day.Voiced != null)
            {
                writer.Write(day.Voiced.Hash);
                writer.Write(day.Voiced.Text);
                writer.Write(day.Voiced.At);
            }
        }
    }

    public static int Load(IGenericReader reader)
    {
        var count = reader.ReadEncodedInt();

        for (var k = 0; k < count; k++)
        {
            // The writer keeps a local time as UTC and reads it back as UTC: the page's days and hours are local, as the stream's.
            var day = new BotChronicleDay { Key = reader.ReadString(), Date = reader.ReadDateTime().ToLocalTime().Date };
            var entries = reader.ReadEncodedInt();

            for (var i = 0; i < entries; i++)
            {
                day.Entries.Add(
                    new BotChronicleEntry
                    {
                        At = reader.ReadDateTime().ToLocalTime(),
                        Kind = reader.ReadString(),
                        Text = reader.ReadString(),
                        Weight = reader.ReadDouble()
                    }
                );
            }

            if (reader.ReadBool())
            {
                day.Voiced = new BotStoryProse { Hash = reader.ReadULong(), Text = reader.ReadString(), At = reader.ReadDateTime() };
            }

            if (!string.IsNullOrEmpty(day.Key))
            {
                Put(day);
            }
        }

        return count;
    }

    /// <summary>The counters, for a world reload. The days are what the save holds and stay.</summary>
    public static void Forget()
    {
        Recorded = 0;
        Repeats = 0;
        Dropped = 0;
        Built = 0;
        _byKind.Clear();
        _founded.Clear();
        _closed.Clear();
        _dirty = true;
        _builtOnce = false;
    }

    /// <summary>Patrick's reset: a new world has no history. Returns the days forgotten.</summary>
    public static int Wipe()
    {
        var days = _days.Count;

        _days.Clear();
        _keys.Clear();
        Forget();
        _json = "{\"days\":[]}";

        return days;
    }
}
