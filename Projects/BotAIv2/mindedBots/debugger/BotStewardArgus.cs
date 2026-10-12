using System;
using System.Text.Json;
using Server.BotAI.V2;

namespace Server.BotAI.Mind;

/// <summary>
/// The model as a town's steward (07.10.2026, steward20): asked, when the slot is free, to choose one of the decrees the rule set
/// found lawful in the town this round — or none — and to say it in a sentence. Filled into <see cref="BotStewardship.Ask"/> from
/// <c>BotDebugModule.Start</c> and emptied on a reload, as the duke's witness is; the bots' assembly never references this one.
///
/// <para>
/// <b>Narrow the enumeration instead of persuading</b> (DECISIONS N2, the duke's rule): the schema's enumeration is exactly the
/// ids the rule set offered, so a model cannot decree what is unlawful; what it chooses is checked again before it is given, and
/// an answer that cannot be read, or none at all, leaves the round to the rule.
/// </para>
/// </summary>
public static class BotStewardArgus
{
    /// <summary>Whether the model is asked at all. Off: the rules decide every round.</summary>
    public static bool Running { get; set; } = true;

    /// <summary>The model asked; empty means the duke's (<c>BotParley.JudgeModel</c>), else the minds' own.</summary>
    public static string Model { get; set; }

    /// <summary>The longest speech kept, in characters.</summary>
    public static int MostSpeech { get; set; } = 200;

    public static long Asked { get; private set; }

    public static long Read { get; private set; }

    public static long Unread { get; private set; }

    public static long WaitedMs { get; private set; }

    public static void Open() => BotStewardship.Ask = Ask;

    public static void Close() => BotStewardship.Ask = null;

    private static bool Ask(BotStewardAsk q, Action<string, string, string> then)
    {
        if (!Running || q?.Ids is not { Length: > 1 } || then == null || !BotOllama.Free || World.Saving)
        {
            return false;
        }

        Asked++;

        var model = !string.IsNullOrWhiteSpace(Model) ? Model : string.IsNullOrWhiteSpace(BotParley.JudgeModel) ? BotOllama.Model : BotParley.JudgeModel;

        BotOllama.Ask(
            Role(q),
            q.Brief ?? "",
            Schema(q),
            false,
            (json, waited) =>
            {
                WaitedMs += waited;

                var (id, speech, reason) = Answer(q, json);

                if (id == null)
                {
                    Unread++;
                    BotDebugLog.Write($"steward of {q.Town}: the model gave no decree that could be read in {waited}ms");
                }
                else
                {
                    Read++;
                    BotDebugLog.Write($"steward of {q.Town}: the model decrees {id} in {waited}ms — \"{speech}\" ({reason})");
                }

                then(id, speech, reason);
            },
            model,
            BotParley.JudgeKeepAlive,
            BotParley.JudgeTimeoutMs
        );

        return true;
    }

    private static string Role(BotStewardAsk q) =>
        $"You are {q.Head ?? "the head"} of the guild {q.Guild}, and the steward of the town of {q.Town}. You hold the town's coffer and its levers: "
        + "the shops' tax, the captain of the guard, bounties on wanted heads, the militia, bars on outcast guilds, holidays and caravans. "
        + "You answer for them: an empty coffer or a town whose regard of you keeps falling will put you out. "
        + "Taxes fill the coffer but the town likes them less and traders buy elsewhere; every decree costs the coffer what it says. "
        + "Choose what the town needs most now from the decrees offered, or none when nothing is worth its price. "
        + "Answer in English with: decree — exactly one of the ids offered; speech — what is cried in the town square, one plain sentence beginning with what is decreed, "
        + "naming the town, no numbers it was not told; reason — one sentence for the record naming the facts that decided it.";

    private static string Schema(BotStewardAsk q)
    {
        var buffer = new System.IO.MemoryStream();

        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            writer.WriteString("type", "object");
            writer.WriteStartObject("properties");
            writer.WriteStartObject("decree");
            writer.WriteString("type", "string");
            writer.WriteStartArray("enum");

            for (var i = 0; i < q.Ids.Length; i++)
            {
                writer.WriteStringValue(q.Ids[i]);
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
            Text(writer, "speech", 15);
            Text(writer, "reason", 15);
            writer.WriteEndObject();
            writer.WriteStartArray("required");
            writer.WriteStringValue("decree");
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

    /// <summary>The decree, its speech and reason; a null decree when it cannot be read or is not one offered.</summary>
    private static (string Id, string Speech, string Reason) Answer(BotStewardAsk q, string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return (null, null, null);
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var id = Field(root, "decree");

            if (id == null || Array.IndexOf(q.Ids, id) < 0)
            {
                return (null, null, null);
            }

            var speech = Field(root, "speech");

            if (speech != null && speech.Length > MostSpeech)
            {
                var cut = speech.LastIndexOf(' ', MostSpeech);

                speech = cut > 0 ? speech[..cut] : speech[..MostSpeech];
            }

            return (id, speech, Field(root, "reason"));
        }
        // Any failure to read an answer is not an answer: the rule decides.
        catch (Exception)
        {
            return (null, null, null);
        }
    }

    private static string Field(JsonElement root, string name) =>
        root.ValueKind == JsonValueKind.Object && root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()?.Trim()
            : null;

    public static string Describe() =>
        $"the steward's model: {(Running ? "on" : "off")}, asked {Asked}, read {Read}, unread {Unread}, {WaitedMs / Math.Max(1, Asked)}ms each";
}
