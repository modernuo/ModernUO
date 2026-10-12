using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Server.BotAI.V2;

namespace Server.BotAI.Mind;

/// <summary>
/// What a mind came back with: one trade, one number it is prepared to be judged on, and its reason.
///
/// <para>
/// <b>The prediction is the point of the whole arrangement.</b> Without a number the model can say anything
/// and nothing can ever be shown to have been wrong; with one, every choice ends in a comparison between
/// what was promised and what the ledger actually measured, and that comparison is the only honest material
/// a lesson can be made of. It is also what the auction weighs the offer by, so an optimistic mind loses
/// its bots to the shard's own arithmetic within a few jobs rather than being argued with.
/// </para>
/// </summary>
public sealed class BotMindChoice
{
    /// <summary>The name of a proposer, exactly as <c>IBotProposer.Name</c> gives it.</summary>
    public string Intent { get; init; }

    /// <summary>Gold-equivalent a minute the mind expects this to come to.</summary>
    public double Expect { get; init; }

    /// <summary>How long it thinks it will take.</summary>
    public double Minutes { get; init; }

    /// <summary>Its reason, in its own words. Never parsed — read by people and fed back at review.</summary>
    public string Why { get; init; }

    /// <summary>
    /// One line to the others, or nothing.
    ///
    /// <para>
    /// Optional in the schema and allowed to be empty, which is the part that matters: a required field is a
    /// field that gets filled every time, and a mind that announces every decision to two colleagues is
    /// noise rather than communication. What is wanted is the line worth saying — something found, something
    /// given up on, somewhere worth coming to.
    /// </para>
    /// </summary>
    public string Say { get; init; }

    /// <summary>
    /// A trade the mind wants its whole guild gathering at, or nothing.
    ///
    /// <para>
    /// <b>Patrick's order of 11.09.2026: the crafters are the guilds' makers, so give them the guild.</b>
    /// These three fields are the whole of that power — what the band collects, what it makes, and where it
    /// stands — and every one of them is optional, because a mind that charges its guild with something new
    /// every two minutes is a mind nobody in it can follow. See <c>BotCharter</c>: an order is a price on
    /// work, never an instruction, and it lapses on its own.
    /// </para>
    /// </summary>
    public string Gather { get; init; }

    /// <summary>And a trade it wants them making at, or nothing.</summary>
    public string Make { get; init; }

    /// <summary>Somewhere it wants the band to be. Zero for nowhere in particular.</summary>
    public int MarchX { get; init; }

    public int MarchY { get; init; }

    /// <summary>
    /// A member of its own band it wants out, or nothing.
    ///
    /// <para>
    /// <b>Patrick's order of 11.09.2026: a leader may choose who is in its band.</b> Both of these are
    /// names rather than trades, and both are constrained to enums built from the two lists the state block
    /// actually showed — see <c>BotRoster</c>. A name is the one thing a model will invent most readily and
    /// an invented name is a decision that silently does nothing, so neither field exists in the schema at
    /// all unless there was somebody real to put in it.
    /// </para>
    /// </summary>
    public string Expel { get; init; }

    /// <summary>Somebody outside every band that it wants to take on, or nothing.</summary>
    public string Recruit { get; init; }

    /// <summary>
    /// A row of the board this mind says it will fill, by the board's own label, or nothing. Patrick's order of
    /// 15.09.2026: the crafters share out who takes which order. See <see cref="BotMindClaims"/>.
    /// </summary>
    public string Take { get; init; }

    /// <summary>A material this maker asks its guild for, out of <see cref="BotCharter.Materials"/>, or nothing.</summary>
    public string Want { get; init; }

    /// <summary>How much of it. Nought when nothing is asked.</summary>
    public int WantAmount { get; init; }

    /// <summary>
    /// The schema the sampler is constrained by, built from the trades that actually exist right now.
    ///
    /// <para>
    /// <b>An enum rather than a free string, and that is a defect class removed rather than handled.</b> A
    /// model asked in words for the name of a trade will eventually invent one — "Mining", "go mining",
    /// "Miner (ore)" — and every one of those is a decision that silently does nothing. Constrained to the
    /// list, the answer is always a trade the shard has.
    /// </para>
    /// </summary>
    /// <param name="ours">Members this leader may put out. Empty leaves the field out of the schema.</param>
    /// <param name="theirs">Bots it may take on. Empty leaves that field out of the schema.</param>
    /// <param name="band">What the guild's orders may name, when it is wider than what the intent may: see <c>BotMind.Rest</c>.</param>
    public static string Schema(IReadOnlyList<string> trades, IReadOnlyList<string> ours = null, IReadOnlyList<string> theirs = null, IReadOnlyList<string> orders = null, IReadOnlyList<string> band = null)
    {
        var buffer = new System.IO.MemoryStream();

        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            writer.WriteString("type", "object");

            writer.WriteStartObject("properties");

            writer.WriteStartObject("intent");
            writer.WriteString("type", "string");
            writer.WriteStartArray("enum");

            for (var i = 0; i < trades.Count; i++)
            {
                writer.WriteStringValue(trades[i]);
            }

            writer.WriteEndArray();
            writer.WriteEndObject();

            writer.WriteStartObject("expect");
            writer.WriteString("type", "number");
            writer.WriteEndObject();

            writer.WriteStartObject("minutes");
            writer.WriteString("type", "number");
            writer.WriteEndObject();

            writer.WriteStartObject("why");
            writer.WriteString("type", "string");
            writer.WriteEndObject();

            writer.WriteStartObject("say");
            writer.WriteString("type", "string");
            writer.WriteEndObject();

            // The guild's orders. Constrained to a list as the intent is, and for the same reason: a
            // model asked in words for the name of a trade invents one sooner or later, and an invented
            // trade is an order that silently does nothing. "none" is first in both, because a mind with no
            // way to say "leave it as it is" charges its guild with something every single time. The list
            // is the band's since build 102, which keeps the trades resting from this mind's own choice.
            var named = band ?? trades;

            for (var g = 0; g < 2; g++)
            {
                writer.WriteStartObject(g == 0 ? "gather" : "make");
                writer.WriteString("type", "string");
                writer.WriteStartArray("enum");
                writer.WriteStringValue("none");

                for (var i = 0; i < named.Count; i++)
                {
                    writer.WriteStringValue(named[i]);
                }

                writer.WriteEndArray();
                writer.WriteEndObject();
            }

            writer.WriteStartObject("marchx");
            writer.WriteString("type", "number");
            writer.WriteEndObject();

            writer.WriteStartObject("marchy");
            writer.WriteString("type", "number");
            writer.WriteEndObject();

            // The roster, on the same terms as the orders above and left out entirely when there is nobody
            // to name. A field whose only value is "none" is a question asked for the sake of asking, and
            // it costs the same prompt budget as one with an answer in it.
            Names(writer, "expel", ours);
            Names(writer, "recruit", theirs);

            // The board's rows, by their own labels, so a claim can only ever name a row that exists; and the
            // materials a maker may ask its guild for, from the one list the charter reads.
            Names(writer, "take", orders);
            Names(writer, "want", BotCharter.Materials);

            writer.WriteStartObject("wantamount");
            writer.WriteString("type", "number");
            writer.WriteEndObject();

            writer.WriteEndObject();

            writer.WriteStartArray("required");
            writer.WriteStringValue("intent");
            writer.WriteStringValue("expect");
            writer.WriteStringValue("minutes");
            writer.WriteStringValue("why");
            writer.WriteEndArray();

            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.ToArray());
    }

    /// <summary>One optional field naming somebody, constrained to the list they were chosen from.</summary>
    private static void Names(Utf8JsonWriter writer, string field, IReadOnlyList<string> who)
    {
        if (who == null || who.Count == 0)
        {
            return;
        }

        writer.WriteStartObject(field);
        writer.WriteString("type", "string");
        writer.WriteStartArray("enum");
        writer.WriteStringValue("none");

        for (var i = 0; i < who.Count; i++)
        {
            writer.WriteStringValue(who[i]);
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    /// <summary>One of the two guild orders, with "none" read as nothing rather than as a trade.</summary>
    private static string Order(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        var said = value.GetString();

        return string.IsNullOrWhiteSpace(said) || said.InsensitiveEquals("none") ? null : said;
    }

    /// <summary>A whole number out of the answer, or nought.</summary>
    private static int Whole(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number
        && value.TryGetInt32(out var whole)
            ? whole
            : 0;

    /// <summary>The schema for a reckoning: one lesson, and whether it is worth keeping at all.</summary>
    public const string LessonSchema =
        """
        {"type":"object","properties":{"lesson":{"type":"string"},"keep":{"type":"boolean"}},"required":["lesson","keep"]}
        """;

    /// <summary>Reads an answer, or null if it is not one. Never throws at the caller.</summary>
    public static BotMindChoice Read(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            if (!root.TryGetProperty("intent", out var intent))
            {
                return null;
            }

            var name = intent.GetString();

            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            return new BotMindChoice
            {
                Intent = name,
                Expect = Number(root, "expect"),
                Minutes = Number(root, "minutes"),
                Why = root.TryGetProperty("why", out var why) ? why.GetString() : null,
                Say = root.TryGetProperty("say", out var say) ? say.GetString() : null,
                Gather = Order(root, "gather"),
                Make = Order(root, "make"),
                MarchX = Whole(root, "marchx"),
                MarchY = Whole(root, "marchy"),
                Expel = Order(root, "expel"),
                Recruit = Order(root, "recruit"),
                Take = Order(root, "take"),
                Want = Order(root, "want"),
                WantAmount = Whole(root, "wantamount")
            };
        }
        // <b>Anything at all, and the word JsonException was not wide enough to keep a promise made three
        // lines above this method's name.</b> A malformed answer makes System.Text.Json build a
        // JsonReaderException — and building its message asks the framework for a localised string, which on
        // a Russian-locale machine means loading System.Text.Json.resources for ru-RU, which is not deployed.
        // The FileNotFoundException from that is thrown while the JsonException is being constructed, so it
        // is not a JsonException and nothing here caught it. It went all the way to the event loop and took
        // the shard down at 07:15:32 on 03.09.2026, after seven hours up, on one bad answer from the model.
        //
        // What comes back from a language model is untrusted input. Any failure to read it means the same
        // thing — this is not an answer — and the caller is entitled to that and nothing else.
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>Reads a reckoning: the lesson and whether the mind thinks it worth keeping.</summary>
    public static (string Lesson, bool Keep) ReadLesson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return (null, false);
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            var lesson = root.TryGetProperty("lesson", out var text) ? text.GetString() : null;
            var keep = root.TryGetProperty("keep", out var flag) && flag.ValueKind == JsonValueKind.True;

            return (string.IsNullOrWhiteSpace(lesson) ? null : lesson.Trim(), keep);
        }
        // The same reasoning as Read above: any failure to read an answer is not an answer.
        catch (Exception)
        {
            return (null, false);
        }
    }

    private static double Number(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number
            ? value.GetDouble()
            : 0.0;

    public override string ToString() =>
        $"{Intent} at {Expect:F0}/min over {Minutes:F0} min — {Why}";
}
