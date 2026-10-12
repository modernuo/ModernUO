using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Server.BotAI.Mind;

/// <summary>
/// What the debugger came back with after one look at the population: one claim, the numbers it was made
/// from, a guess at the cause and one change worth making.
///
/// <para>
/// <b>Every field here exists to make the claim checkable by somebody who was not there.</b> A finding
/// without evidence is an opinion, a cause without a finding is a theory about nothing, and a fix without
/// either is a patch waiting to be applied to code that was working. The one that earns its place hardest
/// is <see cref="Last"/>: the debugger is shown what it said the time before and has to say whether the
/// numbers still support it. A watcher that never revisits its own claims produces a log of confident
/// paragraphs with no way to tell the true ones from the rest — which is exactly what the minds' own
/// reckonings were before they were made to predict a number and be measured against it.
/// </para>
/// </summary>
public sealed class BotDebugNote
{
    /// <summary>What sort of thing it thinks it has found. Constrained; see <see cref="Kinds"/>.</summary>
    public string Kind { get; init; }

    /// <summary>Which bot it is about, by name, or <c>-</c> for something about the population as a whole.</summary>
    public string Bot { get; init; }

    /// <summary>The claim, in one sentence.</summary>
    public string Finding { get; init; }

    /// <summary>The numbers out of the report that support it. Quoted, not summarised.</summary>
    public string Evidence { get; init; }

    /// <summary>What it thinks is behind the numbers. A guess, and labelled one in the log.</summary>
    public string Cause { get; init; }

    /// <summary>One change worth making. Concrete, or it is not a suggestion.</summary>
    public string Fix { get; init; }

    /// <summary>How sure it is, nought to one.</summary>
    public double Confidence { get; init; }

    /// <summary>Whether what it said last time still stands: holds, gone, unclear, first.</summary>
    public string Last { get; init; }

    /// <summary>Which bot to go and stand beside next, or <c>-</c> to stay where it is.</summary>
    public string Watch { get; init; }

    /// <summary>
    /// One administrator's command to run, out of <see cref="BotHand.Verbs"/>, or <c>none</c>.
    ///
    /// <para>
    /// <b>Constrained to the list and defaulting to doing nothing, for the reason every other field here
    /// is.</b> Free words would produce a verb that does not exist on the first minute and a verb that
    /// exists and was not meant on the second. <c>none</c> is first in the list because a watcher offered a
    /// hand and no way to keep it in its pocket uses it every time it is asked.
    /// </para>
    /// </summary>
    public string Probe { get; init; }

    /// <summary>What the command is aimed at: a bot's name, or a pair of coordinates. <c>-</c> for nothing.</summary>
    public string At { get; init; }

    /// <summary>
    /// The sorts of finding there are. An enum rather than free words, for the reason the minds learned the
    /// hard way: a constraint the sampler enforces cannot be reinterpreted and a sentence in a prompt always
    /// can. <c>nothing</c> is on the list on purpose and is the most important entry — a watcher with no way
    /// to say "the population is fine this minute" will invent a defect every time it is asked.
    /// </summary>
    public static readonly string[] Kinds =
    [
        "stuck",
        "loop",
        "starved",
        "mismatch",
        "waste",
        "unreachable",
        "nothing"
    ];

    private static readonly string[] Verdicts = ["first", "holds", "gone", "unclear"];

    /// <summary>
    /// The schema, with the bot names of this minute written into it.
    ///
    /// <para>
    /// <b>Names constrained to the roster, and it is the same defect class the minds removed.</b> Asked in
    /// words for the name of a bot, a model eventually produces "the miner", "Bot 3" or a name off an
    /// earlier report — and every one of those is a finding that cannot be looked up, a bot that cannot be
    /// gone to, and a log entry nobody can check. Constrained to the list, the answer is always somebody who
    /// exists.
    /// </para>
    /// </summary>
    public static string Schema(IReadOnlyList<string> names)
    {
        var buffer = new System.IO.MemoryStream();

        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            writer.WriteString("type", "object");

            writer.WriteStartObject("properties");

            Enumeration(writer, "kind", Kinds);
            Roster(writer, "bot", names);
            // The floors are not the same, because the fields are not the same. A finding has to carry the
            // claim and the pair of numbers it rests on; a cause is allowed to be one clause.
            Text(writer, "finding", 90);
            Text(writer, "evidence", 70);
            Text(writer, "cause", 45);
            Text(writer, "fix", 45);

            writer.WriteStartObject("confidence");
            writer.WriteString("type", "number");
            writer.WriteEndObject();

            Enumeration(writer, "last", Verdicts);
            Roster(writer, "watch", names);
            Enumeration(writer, "probe", BotHand.Verbs);
            Text(writer, "at", 1);

            writer.WriteEndObject();

            writer.WriteStartArray("required");
            writer.WriteStringValue("kind");
            writer.WriteStringValue("bot");
            writer.WriteStringValue("finding");
            writer.WriteStringValue("evidence");
            writer.WriteStringValue("cause");
            writer.WriteStringValue("fix");
            writer.WriteStringValue("confidence");
            writer.WriteStringValue("last");
            writer.WriteStringValue("watch");
            writer.WriteStringValue("probe");
            writer.WriteStringValue("at");
            writer.WriteEndArray();

            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.ToArray());
    }

    /// <summary>
    /// A string field with a floor under it, in characters.
    ///
    /// <para>
    /// <b>A floor, because asking in words for a sentence does not get one.</b> On the morning of 02.09.2026
    /// the debugger answered a report with the single word "SameTwoTiles" as its whole finding — accurate,
    /// unarguable, and of no use to anybody who was not already looking at the same numbers. The prompt had
    /// asked for a sentence naming the pair of figures that disagree; the schema had asked for a string, and
    /// the schema is what the sampler enforces. It is the same lesson as the trade enum and the bot names:
    /// a constraint cannot be reinterpreted and a request always can.
    /// </para>
    ///
    /// <para>
    /// Measured before it was relied on — Ollama passes the schema down to a grammar and the floor is
    /// honoured there, which is not true of every keyword JSON Schema has.
    /// </para>
    /// </summary>
    private static void Text(Utf8JsonWriter writer, string name, int least = 0)
    {
        writer.WriteStartObject(name);
        writer.WriteString("type", "string");

        if (least > 0)
        {
            writer.WriteNumber("minLength", least);
        }

        writer.WriteEndObject();
    }

    private static void Enumeration(Utf8JsonWriter writer, string name, IReadOnlyList<string> values)
    {
        writer.WriteStartObject(name);
        writer.WriteString("type", "string");
        writer.WriteStartArray("enum");

        for (var i = 0; i < values.Count; i++)
        {
            writer.WriteStringValue(values[i]);
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    /// <summary>A name from the roster, or the dash that means nobody. The dash goes first so it is cheap to pick.</summary>
    private static void Roster(Utf8JsonWriter writer, string name, IReadOnlyList<string> names)
    {
        writer.WriteStartObject(name);
        writer.WriteString("type", "string");
        writer.WriteStartArray("enum");
        writer.WriteStringValue("-");

        for (var i = 0; i < names.Count; i++)
        {
            writer.WriteStringValue(names[i]);
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    /// <summary>Reads an answer, or null if it is not one. Never throws at the caller.</summary>
    public static BotDebugNote Read(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            var finding = Word(root, "finding");

            if (string.IsNullOrWhiteSpace(finding))
            {
                return null;
            }

            return new BotDebugNote
            {
                Kind = Word(root, "kind") ?? "nothing",
                Bot = Word(root, "bot") ?? "-",
                Finding = finding,
                Evidence = Word(root, "evidence"),
                Cause = Word(root, "cause"),
                Fix = Word(root, "fix"),
                Confidence = Sure(root),
                Last = Word(root, "last") ?? "first",
                Watch = Word(root, "watch") ?? "-",
                Probe = Word(root, "probe") ?? "none",
                At = Word(root, "at") ?? "-"
            };
        }
        // Any failure to read an answer is not an answer. See BotMindChoice.Read: a narrow catch here let a
        // missing ru-RU resource assembly, thrown while a JsonException was being built, reach the event loop
        // and take the shard down on 03.09.2026.
        catch (Exception)
        {
            return null;
        }
    }

    private static string Word(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    /// <summary>
    /// How sure it says it is, as a share of one, whichever of the two scales it answered on.
    ///
    /// <para>
    /// <b>A schema that says "number" gets both, and asking for one in words does not settle it.</b> Told
    /// the field was a confidence from nought to one, qwen3:14b answered <c>85</c> and deepseek-r1:14b
    /// answered <c>65</c> — both meaning percent, both perfectly reasonable readings of an unbounded number,
    /// and both formatted by this log as "8500% sure". The scale is decided here, where it can only be
    /// decided one way, rather than argued for in a prompt where it can be read either.
    /// </para>
    /// </summary>
    internal static double Sure(JsonElement root)
    {
        if (!root.TryGetProperty("confidence", out var value) || value.ValueKind != JsonValueKind.Number)
        {
            return 0.0;
        }

        var sure = value.GetDouble();

        return Math.Clamp(sure > 1.0 ? sure / 100.0 : sure, 0.0, 1.0);
    }

    public override string ToString() => $"{Kind} — {Finding}";
}

/// <summary>
/// The slower answer: what the debugger makes of an hour of its own findings put together.
///
/// <para>
/// <b>A different question from the one above, and it is worth the cost of a thinking call precisely
/// because it is not the same question asked more often.</b> A finding is about this minute — that bot, that
/// pair of numbers. This asks what the findings have in common, which is where a defect that shows up
/// differently in six places is actually visible. The shard's worst faults have all had that shape: an
/// empty purse read as a veto looked like idle gatherers, timid crafters and a market with no smith, and
/// each of the three on its own looked like a different bug.
/// </para>
///
/// <para>
/// <see cref="Wrong"/> is the field that keeps this honest. A conjecture that names nothing which could
/// falsify it is not a conjecture, and a log full of those is a log that gets acted on and never checked.
/// </para>
/// </summary>
public sealed class BotDebugThought
{
    /// <summary>The one thing most keeping this population from getting anywhere.</summary>
    public string Blocking { get; init; }

    /// <summary>The numbers across the session that say so.</summary>
    public string Evidence { get; init; }

    /// <summary>One change, concrete enough to be made.</summary>
    public string Change { get; init; }

    /// <summary>The next most likely thing, so that one answer is not made to carry everything.</summary>
    public string Second { get; init; }

    /// <summary>What would show this to be wrong, or what to measure next to tell.</summary>
    public string Wrong { get; init; }

    public double Confidence { get; init; }

    /// <summary>
    /// The schema, with a floor under every answer. See <see cref="BotDebugNote.Text"/> for what a missing
    /// floor produced and why the floor lives here rather than in the wording of the question.
    /// </summary>
    public const string Schema =
        """
        {"type":"object","properties":{"blocking":{"type":"string","minLength":120},"evidence":{"type":"string","minLength":80},"change":{"type":"string","minLength":60},"second":{"type":"string","minLength":50},"wrong":{"type":"string","minLength":60},"confidence":{"type":"number"}},"required":["blocking","evidence","change","second","wrong","confidence"]}
        """;

    public static BotDebugThought Read(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            var blocking = Word(root, "blocking");

            if (string.IsNullOrWhiteSpace(blocking))
            {
                return null;
            }

            return new BotDebugThought
            {
                Blocking = blocking,
                Evidence = Word(root, "evidence"),
                Change = Word(root, "change"),
                Second = Word(root, "second"),
                Wrong = Word(root, "wrong"),
                Confidence = BotDebugNote.Sure(root)
            };
        }
        // Any failure to read an answer is not an answer. See BotMindChoice.Read: a narrow catch here let a
        // missing ru-RU resource assembly, thrown while a JsonException was being built, reach the event loop
        // and take the shard down on 03.09.2026.
        catch (Exception)
        {
            return null;
        }
    }

    private static string Word(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    /// <summary>Reads the short answer given to a question asked at the door.</summary>
    public static string ReadAnswer(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            var answer = Word(root, "answer");

            if (string.IsNullOrWhiteSpace(answer))
            {
                return null;
            }

            var evidence = Word(root, "evidence");

            return string.IsNullOrWhiteSpace(evidence) ? answer : $"{answer}\n  evidence: {evidence}";
        }
        // Any failure to read an answer is not an answer. See BotMindChoice.Read: a narrow catch here let a
        // missing ru-RU resource assembly, thrown while a JsonException was being built, reach the event loop
        // and take the shard down on 03.09.2026.
        catch (Exception)
        {
            return null;
        }
    }

    public override string ToString() => Blocking;
}
