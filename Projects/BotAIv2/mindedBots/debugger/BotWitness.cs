using System;
using System.Collections.Generic;
using System.Text.Json;
using Server.BotAI.V2;
using Server.Logging;

namespace Server.BotAI.Mind;

/// <summary>
/// The debugger as witness to the guilds' meetings, and the voice of the duke and of the towns: one of the squad appears at the
/// meeting, shows itself, hears both sides, and says the word the model gives.
///
/// <para>
/// <b>Patrick's order of 29.09.2026, evening: "one of the debuggers is invited as the witness — he becomes visible and hears the
/// sides' arguments"; and for a town, "the debugger is summoned, and feedback on the town's situation is taken from him, and he
/// gives the guild a unique task."</b> The meetings themselves are the bot assembly's (<c>BotParley</c>), which may not reference
/// this one; it asks through three hooks filled here when the debugger starts: <see cref="Summon"/>, <see cref="Dismiss"/> and
/// <see cref="Judge"/>.
/// </para>
///
/// <para>
/// <b>Its harmlessness is the engine's, and none of it is given up to be seen.</b> The body stays blessed — nothing can harm
/// it and it can harm nothing (<c>Mobile.CanBeHarmful</c>) — and stays above Player, which creatures' target search passes
/// over as it passes over the blessed (<c>BaseAI.AcquireFocusMob</c>). Only <c>Hidden</c> is lifted, for the meeting and no
/// longer, so a player standing there sees the scene; it is put back when the meeting ends, and <c>BotDebugger.Hover</c>
/// hides it again on any move besides. While shown it is solid to the walkers — a hidden administrator is walked through, a
/// visible one is walked round — so it is put two tiles off the place, not on it. It never walks: it appears there
/// (<c>Hover</c>). And while it witnesses, the vigil does not move it on (<see cref="Busy"/>, asked in <c>BotVigil.Follow</c>).
/// </para>
///
/// <para>
/// <b>The word is asked in the one slot, off the loop, and never waited on.</b> The ask is queued and put to the model the
/// moment the slot is free (<c>BotOllama.Free</c>), with the population's fast model and a keep-alive of seconds
/// (<c>BotParley.JudgeModel</c>, <c>JudgeKeepAlive</c>), so the watchers' own thinking model is not pushed off the card for long.
/// The answer is constrained by a schema whose outcome is an enumeration of exactly what the meeting allows (DECISIONS N2);
/// <c>BotParley</c> gives the word by rule if it has not come back in time, and a late answer is counted and dropped.
/// </para>
/// </summary>
public static class BotWitness
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotWitness));

    /// <summary>Whether the squad witnesses meetings and the model gives their word. Off: meetings go on unwitnessed, by rule.</summary>
    public static bool Running { get; set; } = true;

    /// <summary>How often the queue of words to ask is looked at, in milliseconds.</summary>
    public static int TickMs { get; set; } = 1000;

    /// <summary>How far from the meeting's place the witness is put, so it stands beside the parties and not in the doorway.</summary>
    public static int Aside { get; set; } = 2;

    /// <summary>Longest a spoken word may be, in characters, before it is cut at a sentence.</summary>
    public static int MostSpeech { get; set; } = 320;

    public static long Summoned { get; private set; }

    public static long NoneFree { get; private set; }

    public static long Asked { get; private set; }

    public static long Answered { get; private set; }

    public static long Unread { get; private set; }

    public static long Stale { get; private set; }

    public static long AskedMs { get; private set; }

    private static readonly Dictionary<BotWatcher, BotHearing> _held = [];

    private static readonly Queue<(BotHearing Hearing, Action<BotVerdict> Then)> _asks = new();

    private static bool _inFlight;

    private static Timer _timer;

    /// <summary>The hooks filled and the clock started. From <c>BotDebugModule.Start</c>.</summary>
    public static void Open()
    {
        BotParley.Summon = Summon;
        BotParley.Dismiss = Dismiss;
        BotParley.Judge = Judge;

        _timer?.Stop();
        _timer = new WitnessTimer(TimeSpan.FromMilliseconds(Math.Max(250, TickMs)));
        _timer.Start();
    }

    /// <summary>The hooks emptied, every witness hidden again and let go. From <c>BotDebugModule.Reset</c>.</summary>
    public static void Close()
    {
        BotParley.Summon = null;
        BotParley.Dismiss = null;
        BotParley.Judge = null;

        _timer?.Stop();
        _timer = null;

        foreach (var (w, _) in _held)
        {
            if (w.Body is { Deleted: false } body)
            {
                body.Hidden = true;
            }
        }

        _held.Clear();
        _asks.Clear();
        _inFlight = false;
    }

    /// <summary>Whether this watcher stands at a meeting now, and so is not to be moved on by the vigil.</summary>
    public static bool Busy(BotWatcher w) => w != null && _held.ContainsKey(w);

    /// <summary>The meeting this watcher stands witness at, in the words the summons line uses, or null. For the live window (02.10.2026).</summary>
    public static string Held(BotWatcher w) =>
        w != null && _held.TryGetValue(w, out var m)
            ? $"witness to meeting #{m.Id}: {m.Guest} {(m.Between ? $"to {m.Host}" : "to the town")} at {m.Town?.Name ?? "their seat"} over {BotDuke.Word(m.Topic)}, shown at {w.Body?.X},{w.Body?.Y}"
            : null;

    /// <summary>The watcher standing witness at meeting number <paramref name="id"/>, or null.</summary>
    public static BotWatcher WitnessOf(int id)
    {
        foreach (var (w, m) in _held)
        {
            if (m.Id == id)
            {
                return w;
            }
        }

        return null;
    }

    /// <summary>
    /// A watcher for the meeting: the lead if it is free and not charting the dungeons, else a helper, the marshal last. Put
    /// beside the place and shown. Null when every one of them is at a meeting already.
    /// </summary>
    private static Mobile Summon(BotHearing m)
    {
        if (!Running || m?.Map == null || m.Map == Map.Internal)
        {
            return null;
        }

        BotWatcher pick = null;

        for (var pass = 0; pass < 2 && pick == null; pass++)
        {
            for (var i = 0; i < BotVigil.Squad.Count && pick == null; i++)
            {
                var w = BotVigil.Squad[i];

                // The marshal only on the second pass: it organises the island's events and is the last to be spared. The zone keeper
                // never, while it keeps them (07.10.2026). See BotVigil.Keeper.
                if (w.Organiser != (pass == 1) || _held.ContainsKey(w) || w.Body is not { Deleted: false } || BotVigil.IsKeeper(w))
                {
                    continue;
                }

                // The lead charting the dungeons stands in the room being probed; it is not taken off that.
                if (i == 0 && !BotHalls.Charted && BotHalls.Probing != Point3D.Zero)
                {
                    continue;
                }

                pick = w;
            }
        }

        if (pick == null)
        {
            NoneFree++;

            return null;
        }

        var body = pick.Body;
        var spot = BotTowns.Place(m.Map, m.Place.X + Aside, m.Place.Y, 3, out var aside) ? aside : m.Place;

        body.Hover(m.Map, spot);

        // Shown, for the meeting only. See the class: blessed and above Player, it stays out of every fight while seen.
        body.Hidden = false;

        _held[pick] = m;
        Summoned++;

        BotDebugLog.Write(
            $"{pick.Name} is summoned as witness to meeting #{m.Id}: {m.Guest} {(m.Between ? $"to {m.Host}" : "to the town")} at {m.Town?.Name ?? "their seat"} over {BotDuke.Word(m.Topic)} — {m.Why}; it shows itself at {spot.X},{spot.Y}"
        );

        logger.Information("{Name} is witness to meeting #{Id} at {Town}, shown at {X},{Y}", pick.Name, m.Id, m.Town?.Name ?? "their seat", spot.X, spot.Y);

        return body;
    }

    /// <summary>The meeting is over: its witness is hidden again and goes back to watching at the vigil's next move.</summary>
    private static void Dismiss(BotHearing m)
    {
        BotWatcher found = null;

        foreach (var (w, held) in _held)
        {
            if (ReferenceEquals(held, m))
            {
                found = w;

                break;
            }
        }

        if (found == null)
        {
            return;
        }

        _held.Remove(found);

        if (found.Body is { Deleted: false } body)
        {
            body.Hidden = true;
        }

        BotDebugLog.Write($"{found.Name} leaves meeting #{m.Id} hidden again: {m.Ending}");
    }

    /// <summary>Queues the word to be asked when the slot is free. Returns whether it will be asked.</summary>
    private static bool Judge(BotHearing m, Action<BotVerdict> then)
    {
        if (!Running || m == null || then == null || m.Allowed is not { Length: > 0 })
        {
            return false;
        }

        _asks.Enqueue((m, then));

        return true;
    }

    private static void Tick()
    {
        if (_inFlight || _asks.Count == 0 || !BotOllama.Free || World.Saving)
        {
            return;
        }

        var (m, then) = _asks.Dequeue();

        // Given by rule meanwhile, or ended: nothing to ask.
        if (m.Stage is not (BotHearingStage.Hearing or BotHearingStage.Awaiting) || m.Verdict != null)
        {
            Stale++;

            return;
        }

        _inFlight = true;
        Asked++;

        var model = string.IsNullOrWhiteSpace(BotParley.JudgeModel) ? BotOllama.Model : BotParley.JudgeModel;

        BotOllama.Ask(
            m.Between ? Duke(m) : Voice(m),
            m.Brief ?? "",
            Schema(m),
            false,
            (json, waited) =>
            {
                _inFlight = false;
                AskedMs += waited;

                var v = Read(m, json);

                // The word, as the witness's latest thought on the live window (02.10.2026): it asked nothing itself, but it is
                // the one standing there, and the model's answer is what a person looking at it wants to read.
                WitnessOf(m.Id)?.Think(
                    v == null ? "unread" : "meeting",
                    $"{(m.Between ? "the duke's word" : "the town's word")} at meeting #{m.Id}: {m.Guest} {(m.Between ? $"to {m.Host}" : "to the town")} over {BotDuke.Word(m.Topic)}",
                    v == null ? "the model gave no word that could be read" : $"{v.Outcome} — \"{v.Speech}\"",
                    v == null ? null : $"{v.Reason}{(v.Report != null ? $"; the town: \"{v.Report}\"" : "")}",
                    null
                );

                if (v == null)
                {
                    Unread++;
                    BotDebugLog.Write($"meeting #{m.Id}: the model gave no word that could be read in {waited}ms{(json == null ? " (no answer at all)" : $": {Cut(json, 300)}")}");
                }
                else
                {
                    Answered++;
                    BotDebugLog.Write($"meeting #{m.Id}: the model's word in {waited}ms is {v.Outcome} — \"{v.Speech}\" ({v.Reason}){(v.Report != null ? $"; the town: \"{v.Report}\"" : "")}");
                }

                then(v);
            },
            model,
            BotParley.JudgeKeepAlive,
            BotParley.JudgeTimeoutMs
        );
    }

    /// <summary>The duke, in Patrick's words of 29.09.2026, and what his answer is for.</summary>
    private static string Duke(BotHearing m) =>
        "You are the duke of this land. The guilds are your vassals, and their quarrels and bargains come before you; your word settles them. "
        + "What you hear is what vassals always bring: one says the other attacks its people too often, the other says the first crosses its borders; "
        + "one takes the reward off the other's fallen fighters, but does it on its own land. Nothing grand — land, blood, trade and pride. "
        + "Judge by the facts you are given, not by the speeches. A war costs both guilds lives and gold and gives the land nothing, "
        + "but a grievance with blood in it that goes unanswered festers; an agreement is worth giving only where both sides can keep it. "
        + $"Your witness, {m.WitnessName ?? "your herald"}, will say your word aloud to both envoys.\n"
        + "Answer in English with: outcome — exactly one of the outcomes offered; speech — what your witness says to both envoys, one or two plain sentences, "
        + "naming the guilds, no titles, no lists, no numbers they were not told; reason — one sentence for the record naming the facts that decided it.";

    /// <summary>The town's voice.</summary>
    private static string Voice(BotHearing m) =>
        $"You are {m.WitnessName ?? "the town's reeve"}, speaking for the town of {m.Town?.Name} to an envoy of the guild {m.Guest}. "
        + "Tell the envoy honestly how the town stands — its shelves, its market, the danger round it — from the numbers you are given, "
        + "and set the guild one task from those offered: the one the town needs most that the guild can do. "
        + "Answer in English with: task — exactly one of the task ids offered; report — how the town stands, one or two plain sentences from the numbers; "
        + "speech — the task put to the guild, one sentence naming it and the place or the goods; reason — one sentence for the record.";

    /// <summary>
    /// The answer's shape: the outcome (or task) an enumeration of exactly what this meeting allows, and floors on the words so
    /// a one-word answer is not an answer. Built per ask, because what is allowed is per meeting.
    /// </summary>
    private static string Schema(BotHearing m)
    {
        var buffer = new System.IO.MemoryStream();

        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            writer.WriteString("type", "object");
            writer.WriteStartObject("properties");

            writer.WriteStartObject(m.Between ? "outcome" : "task");
            writer.WriteString("type", "string");
            writer.WriteStartArray("enum");

            for (var i = 0; i < m.Allowed.Length; i++)
            {
                writer.WriteStringValue(m.Allowed[i]);
            }

            writer.WriteEndArray();
            writer.WriteEndObject();

            if (!m.Between)
            {
                Text(writer, "report", 20);
            }

            Text(writer, "speech", 20);
            Text(writer, "reason", 20);

            writer.WriteEndObject();

            writer.WriteStartArray("required");
            writer.WriteStringValue(m.Between ? "outcome" : "task");

            if (!m.Between)
            {
                writer.WriteStringValue("report");
            }

            writer.WriteStringValue("speech");
            writer.WriteStringValue("reason");
            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        return System.Text.Encoding.UTF8.GetString(buffer.ToArray());
    }

    private static void Text(Utf8JsonWriter writer, string name, int least)
    {
        writer.WriteStartObject(name);
        writer.WriteString("type", "string");
        writer.WriteNumber("minLength", least);
        writer.WriteEndObject();
    }

    /// <summary>The answer, or null when it cannot be read or names something the meeting does not allow.</summary>
    private static BotVerdict Read(BotHearing m, string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var outcome = Field(root, m.Between ? "outcome" : "task");
            var speech = Field(root, "speech");

            if (outcome == null || Array.IndexOf(m.Allowed, outcome) < 0 || string.IsNullOrWhiteSpace(speech))
            {
                return null;
            }

            return new BotVerdict
            {
                Outcome = outcome,
                Speech = Cut(speech, MostSpeech),
                Reason = Field(root, "reason") ?? "the model gave no reason",
                Report = m.Between ? null : Cut(Field(root, "report"), MostSpeech)
            };
        }
        // Any failure to read an answer is not an answer — see BotMindChoice.Read for the day a narrow catch took the shard down.
        catch (Exception)
        {
            return null;
        }
    }

    private static string Field(JsonElement root, string name) =>
        root.ValueKind == JsonValueKind.Object && root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()?.Trim()
            : null;

    /// <summary>Cut at the last sentence that fits, else at the last word.</summary>
    private static string Cut(string text, int most)
    {
        if (text == null || text.Length <= most)
        {
            return text;
        }

        var cut = text.LastIndexOf(". ", most, StringComparison.Ordinal);

        if (cut < most / 3)
        {
            cut = text.LastIndexOf(' ', most);
        }

        return cut > 0 ? text[..(cut + 1)].Trim() : text[..most];
    }

    public static string Describe() =>
        $"witnessed {Summoned} meetings ({NoneFree} times none of the squad was free, {_held.Count} standing now); asked the model {Asked} words ({Answered} read, {Unread} not, {Stale} dropped for being given by rule first, {AskedMs / Math.Max(1, Asked)}ms each)";

    public static void Forget()
    {
        Summoned = 0;
        NoneFree = 0;
        Asked = 0;
        Answered = 0;
        Unread = 0;
        Stale = 0;
        AskedMs = 0;
    }

    private sealed class WitnessTimer : Timer
    {
        public WitnessTimer(TimeSpan interval) : base(interval, interval)
        {
        }

        protected override void OnTick() => Tick();
    }
}
