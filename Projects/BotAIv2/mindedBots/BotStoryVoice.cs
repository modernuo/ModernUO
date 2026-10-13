using System;
using System.Text.Json;
using Server.BotAI.V2;

namespace Server.BotAI.Mind;

/// <summary>
/// The model as the island's chronicler: one chapter or one closed day retold from its dated facts, when the model's slot is free.
/// PLAN-life A3, 13.10.2026: asked to retell one chapter of a bot's life or one closed day of the Chronicle from the dated facts
/// the template was written from, and to say nothing else.
/// Filled into <see cref="BotStory.Ask"/> from <c>BotMindModule.Start</c> and emptied on a reload, as the steward's
/// (<c>BotStewardArgus</c>) is from the debugger's; the bots' assembly never references this one.
///
/// <para>
/// <b>Strings in, a string out.</b> The question is made on the loop from strings the story built (<see cref="BotStoryAsk"/>), the
/// call runs off the loop inside <see cref="BotOllama"/>, and the answer is read here on the loop, where <c>BotOllama</c> posts it;
/// <see cref="BotStory"/> then checks it against the facts (<c>BotStoryWriter.Check</c>) and keeps it or throws it away. Nothing
/// here touches a mobile, an item or a map.
/// </para>
///
/// <para>
/// <b>The duke's model and the duke's manners with the card.</b> Unless <c>bot-story.json</c> names one, the chronicler asks the
/// duke's model (<c>BotParley.JudgeModel</c>, else the population's own) with the duke's keep-alive and timeout, without
/// thinking: a paragraph from a list wants no reckoning, and a thinking model would hold the one slot for a minute.
/// </para>
/// </summary>
public static class BotStoryVoice
{
    public static long Asked { get; private set; }

    public static long Read { get; private set; }

    public static long Unread { get; private set; }

    public static void Open() => BotStory.Ask = Ask;

    public static void Close() => BotStory.Ask = null;

    private static bool Ask(BotStoryAsk q, Action<string, long> then)
    {
        if (q == null || then == null || string.IsNullOrWhiteSpace(q.Facts) || !BotOllama.Free || World.WorldState != WorldState.Running)
        {
            return false;
        }

        Asked++;

        var model = !string.IsNullOrWhiteSpace(q.Model) ? q.Model
            : !string.IsNullOrWhiteSpace(BotParley.JudgeModel) ? BotParley.JudgeModel
            : BotOllama.Model;

        BotOllama.Ask(
            Role(q),
            Brief(q),
            Schema,
            false,
            (json, waited) =>
            {
                var text = Answer(json);

                if (text == null)
                {
                    Unread++;
                }
                else
                {
                    Read++;
                }

                then(text, waited);
            },
            model,
            BotParley.JudgeKeepAlive,
            BotParley.JudgeTimeoutMs
        );

        return true;
    }

    private static string Role(BotStoryAsk q) =>
        q.Kind == "day"
            ? "You keep the chronicle of an island of adventurers, guilds and towns. You are given what happened on one day, one fact a line "
              + "with its time. Retell the day as one page of plain English prose in the past tense, in the measured voice of a chronicler. "
              + "Use only the facts given: every person, guild, town, place, creature and number you write must appear in them, spelled as "
              + "they are. Add no event, cause, motive, feeling or detail that is not in them; leave out what you cannot say without inventing. "
              + $"At most {Math.Max(40, q.MostWords)} words. Answer with: text — the page."
            : "You keep the chronicle of an island of adventurers, guilds and towns. You are given the facts of one chapter of one person's "
              + "life, one fact a line, most with a date; quoted words are the person's own. Retell the chapter as one paragraph of plain "
              + "English prose in the past tense and the third person, in the measured voice of a chronicler, naming the person. Use only "
              + "the facts given: every person, guild, town, place, creature and number you write must appear in them, spelled as they are. "
              + "Add no event, cause, motive, feeling or detail that is not in them; you may quote the person's words. "
              + $"At most {Math.Max(40, q.MostWords)} words. Answer with: text — the paragraph.";

    private static string Brief(BotStoryAsk q) =>
        $"{q.Title}\nAbout: {q.Who}\nFacts:\n{q.Facts}";

    private const string Schema = "{\"type\":\"object\",\"properties\":{\"text\":{\"type\":\"string\",\"minLength\":40}},\"required\":[\"text\"]}";

    /// <summary>The paragraph out of the model's JSON, or null when there is none to read.</summary>
    private static string Answer(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);

            return doc.RootElement.ValueKind == JsonValueKind.Object
                   && doc.RootElement.TryGetProperty("text", out var text)
                   && text.ValueKind == JsonValueKind.String
                ? text.GetString()?.Trim()
                : null;
        }
        // Any failure to read an answer is not an answer: the template stands. See BotOllama.Content for why the catch is wide.
        catch (Exception)
        {
            return null;
        }
    }

    public static string Describe() => $"the chronicler's model: asked {Asked}, read {Read}, unread {Unread}";
}
