using System;
using System.Collections.Generic;
using System.IO;
using Server.Json;
using Server.Logging;
using Server.Text;

namespace Server.BotAI.Mind;

/// <summary>One thing the debugger has come to believe, and how many times it has come to believe it.</summary>
public sealed class BotDebugBelief
{
    public string Kind { get; set; }

    public string Who { get; set; }

    public string Finding { get; set; }

    public string Cause { get; set; }

    public string Fix { get; set; }

    public double Confidence { get; set; }

    /// <summary>
    /// How many times this has been found, counting across sessions.
    ///
    /// <para>
    /// <b>The most valuable number in this file.</b> A defect named once by a language model is a guess; the
    /// same defect named nine times over four evenings, from measurements taken on different populations, is
    /// something a person should go and look at. Nothing else here distinguishes the two, and without it a
    /// memory of findings is just a longer list of guesses.
    /// </para>
    /// </summary>
    public int Seen { get; set; }

    public string First { get; set; }

    public string Last { get; set; }

    /// <summary>Whether the debugger has since looked and found the numbers no longer support it.</summary>
    public bool Gone { get; set; }
}

/// <summary>Something a person said to the debugger. Kept until it is answered, and after.</summary>
public sealed class BotDebugAsk
{
    public string Who { get; set; }

    public string What { get; set; }

    public string When { get; set; }

    public bool Answered { get; set; }
}

/// <summary>What survives a restart.</summary>
public sealed class BotDebugRecall
{
    public int Sessions { get; set; }

    public List<BotDebugBelief> Beliefs { get; set; } = [];

    /// <summary>Standing conclusions from the slow question. Fewer, broader, and about the shard.</summary>
    public List<string> Lessons { get; set; } = [];

    public List<BotDebugAsk> Notes { get; set; } = [];
}

/// <summary>
/// The debugger's long memory: <c>Configuration/bot-debugger-memory.json</c>.
///
/// <para>
/// <b>Without it every restart is the first evening.</b> The population is rebuilt from configuration on
/// every world load and keeps nothing but names and skills, and until now the debugger kept even less — its
/// findings, its conclusions and every question a person had asked it went with the process. That made it
/// structurally incapable of the one thing that would make it worth having: saying "this is the ninth time I
/// have found this, over four sessions". A watcher that cannot count how often it has been right about the
/// same thing is a watcher whose confidence means nothing.
/// </para>
///
/// <para>
/// <b>Written the moment something is added rather than at shutdown, and that is not caution.</b> A shard is
/// killed far more often than it is stopped — this one has been killed a dozen times today — and a memory
/// that is flushed on a clean exit is a memory that is empty exactly when it is wanted.
/// </para>
///
/// <para>
/// <b>Beliefs are merged by what they say, never by when they were said.</b> A model asked the same question
/// about the same defect writes it in different words every time; a check on the opening characters lets
/// every one of them through and the file fills with one finding restated forty ways. Two thirds of the
/// words in common is one belief twice — the same rule, and the same figure, the minds' own lessons use.
/// </para>
/// </summary>
public static class BotDebugMemory
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotDebugMemory));

    private const string MemoryPath = "Configuration/bot-debugger-memory.json";

    /// <summary>How alike two findings must be before the second is the first again, as a share of words.</summary>
    public static double Same { get; set; } = 0.67;

    /// <summary>Most beliefs kept. Beyond this the least-seen goes, not the oldest.</summary>
    public static int MostBeliefs { get; set; } = 40;

    /// <summary>Most standing conclusions kept.</summary>
    public static int MostLessons { get; set; } = 12;

    /// <summary>Most beliefs recited into a prompt, strongest first.</summary>
    public static int Recall { get; set; } = 8;

    private static BotDebugRecall _recall = new();

    public static int Sessions => _recall.Sessions;

    public static int Beliefs => _recall.Beliefs.Count;

    public static IReadOnlyList<BotDebugAsk> Notes => _recall.Notes;

    public static void Load()
    {
        var path = Path.Combine(Core.BaseDirectory, MemoryPath);

        try
        {
            _recall = JsonConfig.Deserialize<BotDebugRecall>(path) ?? new BotDebugRecall();
        }
        catch (Exception e)
        {
            // A memory that will not parse is not a reason to refuse to run. It is a reason to say so once
            // and carry on with nothing, because the alternative is a debugger that will not start on the
            // morning after the one evening its own file was written badly.
            _recall = new BotDebugRecall();

            logger.Warning("The debugger's memory could not be read and it starts this session with none: {Message}", e.Message);
        }

        _recall.Beliefs ??= [];
        _recall.Lessons ??= [];
        _recall.Notes ??= [];

        // Labels held from before they were turned away at the door go now, each named in the log with its count, so
        // that what was dropped can still be read.
        for (var i = _recall.Beliefs.Count - 1; i >= 0; i--)
        {
            var belief = _recall.Beliefs[i];

            if (!Label(belief.Finding))
            {
                continue;
            }

            logger.Information(
                "The debugger drops a belief that was a label rather than a sentence, found {Seen} times from {First} to {Last}: [{Kind}] {Finding}",
                belief.Seen,
                belief.First,
                belief.Last,
                belief.Kind,
                belief.Finding
            );

            _recall.Beliefs.RemoveAt(i);
        }

        _recall.Sessions++;

        Save();

        logger.Information(
            "The debugger remembers {Beliefs} findings and {Lessons} conclusions from {Sessions} earlier sessions, and {Asks} things people asked it that it has not answered",
            _recall.Beliefs.Count,
            _recall.Lessons.Count,
            Math.Max(0, _recall.Sessions - 1),
            Unanswered()
        );
    }

    public static void Save()
    {
        try
        {
            JsonConfig.Serialize(Path.Combine(Core.BaseDirectory, MemoryPath), _recall);
        }
        catch (Exception e)
        {
            logger.Warning("The debugger's memory could not be written down: {Message}", e.Message);
        }
    }

    /// <summary>
    /// Files a finding, merging it into one already held if it is saying the same thing again.
    /// </summary>
    public static void Believe(BotDebugNote note)
    {
        if (note == null || string.IsNullOrWhiteSpace(note.Finding) || note.Kind == "nothing" || Label(note.Finding))
        {
            return;
        }

        var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        var held = Alike(note.Finding);

        if (held != null)
        {
            held.Seen++;
            held.Last = now;
            held.Gone = false;

            // The stronger statement of the two is kept. A finding restated with more confidence and a
            // better-named cause is the same finding better understood, and keeping the first draft for ever
            // because it came first would make the memory worse the longer it ran.
            if (note.Confidence >= held.Confidence)
            {
                held.Confidence = note.Confidence;
                held.Cause = note.Cause ?? held.Cause;
                held.Fix = note.Fix ?? held.Fix;
            }

            Save();

            return;
        }

        _recall.Beliefs.Add(
            new BotDebugBelief
            {
                Kind = note.Kind,
                Who = note.Bot,
                Finding = note.Finding,
                Cause = note.Cause,
                Fix = note.Fix,
                Confidence = note.Confidence,
                Seen = 1,
                First = now,
                Last = now
            }
        );

        Trim();
        Save();
    }

    /// <summary>The debugger looked again and the numbers no longer support what it said. Kept, and marked.</summary>
    public static void Doubt(string finding)
    {
        var held = Alike(finding);

        if (held == null)
        {
            return;
        }

        held.Gone = true;
        held.Last = DateTime.Now.ToString("yyyy-MM-dd HH:mm");

        Save();
    }

    /// <summary>A conclusion from the slow question. Broader than a finding and about the shard, not a bot.</summary>
    public static void Learn(string lesson)
    {
        if (string.IsNullOrWhiteSpace(lesson))
        {
            return;
        }

        for (var i = 0; i < _recall.Lessons.Count; i++)
        {
            if (Overlap(_recall.Lessons[i], lesson) >= Same)
            {
                return;
            }
        }

        _recall.Lessons.Add(lesson.Trim());

        while (_recall.Lessons.Count > MostLessons)
        {
            _recall.Lessons.RemoveAt(0);
        }

        Save();
    }

    /// <summary>Somebody asked the debugger something. Kept across restarts: a question outlives a process.</summary>
    public static void Ask(string who, string what)
    {
        if (string.IsNullOrWhiteSpace(what))
        {
            return;
        }

        _recall.Notes.Add(
            new BotDebugAsk
            {
                Who = who,
                What = what.Trim(),
                When = DateTime.Now.ToString("yyyy-MM-dd HH:mm")
            }
        );

        Save();
    }

    /// <summary>Marks every standing question as having been put in front of the model.</summary>
    public static void Answered()
    {
        var moved = false;

        for (var i = 0; i < _recall.Notes.Count; i++)
        {
            if (!_recall.Notes[i].Answered)
            {
                _recall.Notes[i].Answered = true;
                moved = true;
            }
        }

        if (moved)
        {
            Save();
        }
    }

    public static int Unanswered()
    {
        var waiting = 0;

        for (var i = 0; i < _recall.Notes.Count; i++)
        {
            if (!_recall.Notes[i].Answered)
            {
                waiting++;
            }
        }

        return waiting;
    }

    /// <summary>
    /// What it has believed across every session, strongest first — strongest meaning most often found, not
    /// most recently found.
    /// </summary>
    public static string Recite()
    {
        if (_recall.Beliefs.Count == 0 && _recall.Lessons.Count == 0)
        {
            return null;
        }

        var sb = ValueStringBuilder.Create(1024);

        try
        {
            if (_recall.Beliefs.Count > 0)
            {
                List<BotDebugBelief> sorted = [.. _recall.Beliefs];

                sorted.Sort((a, b) => b.Seen.CompareTo(a.Seen));

                sb.Append("Across ");
                sb.Append(_recall.Sessions);
                sb.AppendLine(" sessions of watching this shard, these are the things you have found more than once. The count is how many separate times you reached the same conclusion from fresh measurements, which is the only reason to weigh one above another.");

                var recited = 0;

                for (var i = 0; i < sorted.Count && recited < Recall; i++)
                {
                    var belief = sorted[i];

                    if (Label(belief.Finding))
                    {
                        continue;
                    }

                    recited++;

                    sb.Append("- found ");
                    sb.Append(belief.Seen);
                    sb.Append(belief.Seen == 1 ? " time" : " times");
                    sb.Append(" (");
                    sb.Append(belief.First);
                    sb.Append(" to ");
                    sb.Append(belief.Last);
                    sb.Append(belief.Gone ? ", and last time the numbers no longer supported it" : "");
                    sb.Append(") [");
                    sb.Append(belief.Kind);
                    sb.Append("] ");
                    sb.Append(belief.Finding);

                    if (!string.IsNullOrWhiteSpace(belief.Fix))
                    {
                        sb.Append(" — you suggested: ");
                        sb.Append(belief.Fix);
                    }

                    sb.AppendLine("");
                }
            }

            if (_recall.Lessons.Count > 0)
            {
                sb.AppendLine("\nWhat you have concluded about this shard as a whole:");

                for (var i = 0; i < _recall.Lessons.Count; i++)
                {
                    sb.Append("- ");
                    sb.AppendLine(_recall.Lessons[i]);
                }
            }

            return sb.ToString();
        }
        finally
        {
            sb.Dispose();
        }
    }

    /// <summary>The longest run of characters without a space that a finding may hold before it is a label.</summary>
    public static int LongestWord { get; set; } = 32;

    /// <summary>
    /// Whether a finding is a label rather than a sentence: no space in it at all, or a run of characters without one
    /// longer than any word has business being.
    ///
    /// <para>
    /// <b>The strongest belief this memory held on 14.09.2026 was the word "SameTwoTiles", found 102 times in 387
    /// sessions.</b> The prompt quoted that word as the example of what a finding is not, beside a model sentence with a
    /// bot's name and coordinates in it; the schema's floor on length made the model pad the word into a hundred
    /// joined-up characters with coordinates spliced in; and every repetition was merged here as another independent
    /// sighting, so it was recited at the top of every prompt. Twenty-six findings in ninety minutes that evening were
    /// that label about five different bots, while twenty-eight bots died in one field and no watcher said so.
    /// </para>
    /// </summary>
    public static bool Label(string finding)
    {
        if (string.IsNullOrWhiteSpace(finding) || finding.IndexOf(' ') < 0)
        {
            return true;
        }

        var run = 0;

        for (var i = 0; i < finding.Length; i++)
        {
            if (char.IsWhiteSpace(finding[i]))
            {
                run = 0;

                continue;
            }

            if (++run > LongestWord)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>The belief already held that says the same thing, or null.</summary>
    private static BotDebugBelief Alike(string finding)
    {
        if (string.IsNullOrWhiteSpace(finding))
        {
            return null;
        }

        for (var i = 0; i < _recall.Beliefs.Count; i++)
        {
            if (Overlap(_recall.Beliefs[i].Finding, finding) >= Same)
            {
                return _recall.Beliefs[i];
            }
        }

        return null;
    }

    /// <summary>
    /// How much two sentences have in common, as a share of the shorter one's words.
    ///
    /// By overlap, never by prefix: the same defect written twice differs at the first word as often as at
    /// the last, and a check on the opening characters lets every restatement through.
    /// </summary>
    private static double Overlap(string a, string b)
    {
        if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b))
        {
            return 0.0;
        }

        var one = a.ToLowerInvariant().Split([' ', ',', '.', ';', ':', '(', ')', '"', '\''], StringSplitOptions.RemoveEmptyEntries);
        var two = b.ToLowerInvariant().Split([' ', ',', '.', ';', ':', '(', ')', '"', '\''], StringSplitOptions.RemoveEmptyEntries);

        if (one.Length == 0 || two.Length == 0)
        {
            return 0.0;
        }

        HashSet<string> words = [.. one];
        var shared = 0;

        for (var i = 0; i < two.Length; i++)
        {
            if (words.Contains(two[i]))
            {
                shared++;
            }
        }

        return shared / (double)Math.Min(one.Length, two.Length);
    }

    /// <summary>
    /// Keeps the file bounded by dropping the least-often-found, never the oldest.
    ///
    /// The oldest belief is frequently the truest one — it has had the most evenings to be re-found — and a
    /// memory that forgets by age throws away exactly the findings that earned their place.
    /// </summary>
    private static void Trim()
    {
        while (_recall.Beliefs.Count > MostBeliefs)
        {
            var worst = 0;

            for (var i = 1; i < _recall.Beliefs.Count; i++)
            {
                if (_recall.Beliefs[i].Seen < _recall.Beliefs[worst].Seen)
                {
                    worst = i;
                }
            }

            _recall.Beliefs.RemoveAt(worst);
        }
    }

    /// <summary>One line for the shard's own log.</summary>
    public static string Describe() =>
        $"{_recall.Beliefs.Count} findings remembered over {_recall.Sessions} sessions, "
        + $"{_recall.Lessons.Count} standing conclusions, {Unanswered()} questions from people still unanswered";
}
